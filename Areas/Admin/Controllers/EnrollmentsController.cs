using CNPM_EnglishLearning.Data;
using CNPM_EnglishLearning.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CNPM_EnglishLearning.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class EnrollmentsController : Controller
    {
        private readonly ApplicationDbContext _context;

        private static readonly HashSet<string> ValidStatuses = new(StringComparer.OrdinalIgnoreCase)
        {
            "active", "completed", "cancelled"
        };

        public EnrollmentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View("~/Areas/Admin/Views/enrollments.html");
        }

        [HttpGet("/Admin/api/Enrollments/List")]
        public async Task<IActionResult> List(string? status, string? search)
        {
            try
            {
                var query =
                    from e in _context.Enrollments.AsNoTracking()
                    join u in _context.Users.AsNoTracking() on e.UserId equals u.Id
                    join cc in _context.CourseClasses.AsNoTracking() on e.ClassId equals cc.Id
                    join c in _context.Courses.AsNoTracking() on cc.CourseId equals c.Id
                    select new
                    {
                        id = e.Id,
                        userId = u.Id,
                        studentName = u.FullName,
                        studentEmail = u.Email,
                        classId = cc.Id,
                        classCode = cc.ClassCode,
                        className = cc.ClassName,
                        courseTitle = c.Title,
                        price = cc.Price,
                        enrolledAt = e.EnrolledAt,
                        status = e.Status
                    };

                if (!string.IsNullOrWhiteSpace(status) && !status.Equals("all", StringComparison.OrdinalIgnoreCase))
                {
                    var normalizedStatus = status.Trim().ToLowerInvariant();
                    query = query.Where(x => x.status == normalizedStatus);
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var term = search.Trim().ToLower();
                    query = query.Where(x =>
                        x.studentName.ToLower().Contains(term) ||
                        x.studentEmail.ToLower().Contains(term) ||
                        x.classCode.ToLower().Contains(term) ||
                        x.className.ToLower().Contains(term) ||
                        x.courseTitle.ToLower().Contains(term));
                }

                var items = await query
                    .OrderByDescending(x => x.enrolledAt)
                    .ThenByDescending(x => x.id)
                    .ToListAsync();

                var students = await _context.Users
                    .AsNoTracking()
                    .Where(u => u.Role == "student" && u.IsActive)
                    .OrderBy(u => u.FullName)
                    .Select(u => new
                    {
                        id = u.Id,
                        fullName = u.FullName,
                        email = u.Email
                    })
                    .ToListAsync();

                var openClasses = await _context.CourseClasses
                    .AsNoTracking()
                    .Where(cc => cc.Status == "upcoming" || cc.Status == "ongoing")
                    .OrderBy(cc => cc.ClassCode)
                    .ToListAsync();

                var openClassIds = openClasses.Select(cc => cc.Id).ToList();
                var activeCounts = new Dictionary<int, int>();
                if (openClassIds.Count > 0)
                {
                    activeCounts = await _context.Enrollments
                        .AsNoTracking()
                        .Where(e => openClassIds.Contains(e.ClassId) && e.Status == "active")
                        .GroupBy(e => e.ClassId)
                        .Select(g => new { ClassId = g.Key, Count = g.Count() })
                        .ToDictionaryAsync(x => x.ClassId, x => x.Count);
                }

                var classes = openClasses.Select(cc => new
                {
                    id = cc.Id,
                    classCode = cc.ClassCode,
                    className = cc.ClassName,
                    price = cc.Price,
                    status = cc.Status,
                    activeStudents = activeCounts.TryGetValue(cc.Id, out var cnt) ? cnt : 0,
                    maxStudents = cc.MaxStudents
                }).ToList();

                return Ok(new { items, students, classes });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Không thể tải danh sách ghi danh.", detail = ex.Message });
            }
        }

        [HttpPost("/Admin/api/Enrollments/Create")]
        public async Task<IActionResult> Create([FromBody] EnrollmentCreateDto? dto)
        {
            if (dto == null || dto.UserId <= 0 || dto.ClassId <= 0)
            {
                return BadRequest(new { message = "Vui lòng chọn học viên và lớp học hợp lệ." });
            }

            var status = string.IsNullOrWhiteSpace(dto.Status) ? "active" : dto.Status.Trim().ToLowerInvariant();
            if (!ValidStatuses.Contains(status))
            {
                return BadRequest(new { message = "Trạng thái ghi danh không hợp lệ (chỉ chấp nhận active, completed, cancelled)." });
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var student = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == dto.UserId && u.Role == "student" && u.IsActive);
                if (student == null)
                {
                    await transaction.RollbackAsync();
                    return BadRequest(new { message = "Học viên không tồn tại hoặc tài khoản đang bị khóa." });
                }

                // Khóa giao dịch mức PostgreSQL (pg_advisory_xact_lock) theo ClassId để tuần tự hóa các request ghi danh đồng thời vào cùng một lớp.
                long lockKey = 100_000_000L + dto.ClassId;
                await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({lockKey})");

                var courseClass = await _context.CourseClasses
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == dto.ClassId);
                if (courseClass == null || (courseClass.Status != "upcoming" && courseClass.Status != "ongoing"))
                {
                    await transaction.RollbackAsync();
                    return BadRequest(new { message = "Lớp học không tồn tại hoặc không còn nhận ghi danh (chỉ nhận lớp upcoming hoặc ongoing)." });
                }

                var alreadyActive = await _context.Enrollments
                    .AnyAsync(e => e.UserId == dto.UserId && e.ClassId == dto.ClassId && e.Status == "active");
                if (alreadyActive)
                {
                    await transaction.RollbackAsync();
                    return BadRequest(new { message = $"Học viên {student.FullName} đã có bản ghi ghi danh đang học (active) trong lớp {courseClass.ClassCode}." });
                }

                if (status == "active")
                {
                    var activeCount = await _context.Enrollments
                        .CountAsync(e => e.ClassId == dto.ClassId && e.Status == "active");
                    if (activeCount >= courseClass.MaxStudents)
                    {
                        await transaction.RollbackAsync();
                        return BadRequest(new { message = $"Lớp {courseClass.ClassCode} đã đạt sĩ số tối đa ({activeCount}/{courseClass.MaxStudents})." });
                    }
                }

                var enrollment = new Enrollment
                {
                    UserId = dto.UserId,
                    ClassId = dto.ClassId,
                    EnrolledAt = DateTime.UtcNow,
                    Status = status
                };

                _context.Enrollments.Add(enrollment);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { message = "Ghi danh học viên thành công.", id = enrollment.Id, item = enrollment });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = "Không thể ghi danh học viên.", detail = ex.Message });
            }
        }

        [HttpPost("/Admin/api/Enrollments/UpdateStatus/{id}")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] EnrollmentStatusDto? dto)
        {
            if (dto == null)
            {
                return BadRequest(new { message = "Dữ liệu cập nhật không hợp lệ." });
            }

            var newStatus = (dto.Status ?? string.Empty).Trim().ToLowerInvariant();
            if (!ValidStatuses.Contains(newStatus))
            {
                return BadRequest(new { message = "Trạng thái ghi danh phải là active, completed hoặc cancelled." });
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var enrollment = await _context.Enrollments.FirstOrDefaultAsync(e => e.Id == id);
                if (enrollment == null)
                {
                    await transaction.RollbackAsync();
                    return NotFound(new { message = "Không tìm thấy bản ghi ghi danh." });
                }

                // Khóa giao dịch mức PostgreSQL (pg_advisory_xact_lock) theo ClassId để tuần tự hóa các request cập nhật đồng thời vào cùng một lớp.
                long lockKey = 100_000_000L + enrollment.ClassId;
                await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({lockKey})");

                var courseClass = await _context.CourseClasses
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == enrollment.ClassId);

                if (newStatus == "active" && enrollment.Status != "active")
                {
                    if (courseClass == null)
                    {
                        await transaction.RollbackAsync();
                        return NotFound(new { message = "Không tìm thấy lớp học của bản ghi ghi danh." });
                    }

                    if (courseClass.Status != "upcoming" && courseClass.Status != "ongoing")
                    {
                        await transaction.RollbackAsync();
                        var statusLabel = courseClass.Status == "completed" ? "đã kết thúc" : "đã bị hủy";
                        return BadRequest(new { message = $"Không thể kích hoạt lại ghi danh vì lớp {courseClass.ClassCode} {statusLabel}." });
                    }

                    var duplicateActive = await _context.Enrollments
                        .AnyAsync(e => e.Id != enrollment.Id && e.UserId == enrollment.UserId && e.ClassId == enrollment.ClassId && e.Status == "active");
                    if (duplicateActive)
                    {
                        await transaction.RollbackAsync();
                        return BadRequest(new { message = "Học viên này đã có một ghi danh active khác trong cùng lớp." });
                    }

                    var activeCount = await _context.Enrollments
                        .CountAsync(e => e.ClassId == enrollment.ClassId && e.Status == "active");
                    if (activeCount >= courseClass.MaxStudents)
                    {
                        await transaction.RollbackAsync();
                        return BadRequest(new { message = $"Lớp {courseClass.ClassCode} đã đạt sĩ số tối đa ({activeCount}/{courseClass.MaxStudents})." });
                    }
                }

                enrollment.Status = newStatus;
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { message = "Cập nhật trạng thái ghi danh thành công.", status = enrollment.Status });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = "Không thể cập nhật trạng thái ghi danh.", detail = ex.Message });
            }
        }

        public class EnrollmentCreateDto
        {
            public int UserId { get; set; }
            public int ClassId { get; set; }
            public string? Status { get; set; }
        }

        public class EnrollmentStatusDto
        {
            public string Status { get; set; } = string.Empty;
        }
    }
}
