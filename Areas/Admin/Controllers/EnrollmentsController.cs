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
        private readonly ILogger<EnrollmentsController> _logger;

        private static readonly HashSet<string> ValidStatuses = new(StringComparer.OrdinalIgnoreCase)
        {
            "active", "completed", "cancelled"
        };

        public EnrollmentsController(ApplicationDbContext context, ILogger<EnrollmentsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View("~/Areas/Admin/Views/enrollments.html");
        }

        /// <param name="lookups">
        /// true: trả kèm danh sách học viên + lớp đang mở (dùng cho dropdown modal).
        /// Frontend gửi false khi chỉ lọc/tìm kiếm để tránh tải lại dữ liệu không đổi.
        /// </param>
        [HttpGet("/Admin/api/Enrollments/List")]
        public async Task<IActionResult> List(string? status, string? search, CancellationToken cancellationToken, bool lookups = true)
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
                    .ToListAsync(cancellationToken);

                if (!lookups)
                {
                    return Ok(new { success = true, items });
                }

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
                    .ToListAsync(cancellationToken);

                // Projection + đếm sĩ số bằng subquery tương quan ngay trên PostgreSQL (không kéo cả entity về RAM)
                var classes = await _context.CourseClasses
                    .AsNoTracking()
                    .Where(cc => cc.Status == "upcoming" || cc.Status == "ongoing")
                    .OrderBy(cc => cc.ClassCode)
                    .Select(cc => new
                    {
                        id = cc.Id,
                        classCode = cc.ClassCode,
                        className = cc.ClassName,
                        price = cc.Price,
                        status = cc.Status,
                        activeStudents = _context.Enrollments.Count(e => e.ClassId == cc.Id && e.Status == "active"),
                        maxStudents = cc.MaxStudents
                    })
                    .ToListAsync(cancellationToken);

                return Ok(new { success = true, items, students, classes });
            }
            catch (OperationCanceledException)
            {
                return this.ApiCancelled();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể tải danh sách ghi danh.");
                return this.ApiServerError("Không thể tải danh sách ghi danh.");
            }
        }

        [HttpPost("/Admin/api/Enrollments/Create")]
        public async Task<IActionResult> Create([FromBody] EnrollmentCreateDto? dto, CancellationToken cancellationToken)
        {
            if (dto == null || dto.UserId <= 0 || dto.ClassId <= 0)
            {
                return this.ApiBadRequest("Vui lòng chọn học viên và lớp học hợp lệ.");
            }

            var status = string.IsNullOrWhiteSpace(dto.Status) ? "active" : dto.Status.Trim().ToLowerInvariant();
            if (!ValidStatuses.Contains(status))
            {
                return this.ApiBadRequest("Trạng thái ghi danh không hợp lệ (chỉ chấp nhận active, completed, cancelled).");
            }

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var student = await _context.Users
                    .AsNoTracking()
                    .Where(u => u.Id == dto.UserId && u.Role == "student" && u.IsActive)
                    .Select(u => new { u.FullName })
                    .FirstOrDefaultAsync(cancellationToken);
                if (student == null)
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiBadRequest("Học viên không tồn tại hoặc tài khoản đang bị khóa.");
                }

                // Khóa giao dịch mức PostgreSQL (pg_advisory_xact_lock) theo ClassId để tuần tự hóa các request ghi danh đồng thời vào cùng một lớp.
                long lockKey = 100_000_000L + dto.ClassId;
                await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({lockKey})", cancellationToken);

                var courseClass = await _context.CourseClasses
                    .AsNoTracking()
                    .Where(c => c.Id == dto.ClassId)
                    .Select(c => new { c.ClassCode, c.Status, c.MaxStudents })
                    .FirstOrDefaultAsync(cancellationToken);
                if (courseClass == null || (courseClass.Status != "upcoming" && courseClass.Status != "ongoing"))
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiBadRequest("Lớp học không tồn tại hoặc không còn nhận ghi danh (chỉ nhận lớp upcoming hoặc ongoing).");
                }

                var alreadyActive = await _context.Enrollments
                    .AnyAsync(e => e.UserId == dto.UserId && e.ClassId == dto.ClassId && e.Status == "active", cancellationToken);
                if (alreadyActive)
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiBadRequest($"Học viên {student.FullName} đã có bản ghi ghi danh đang học (active) trong lớp {courseClass.ClassCode}.");
                }

                if (status == "active")
                {
                    var activeCount = await _context.Enrollments
                        .CountAsync(e => e.ClassId == dto.ClassId && e.Status == "active", cancellationToken);
                    if (activeCount >= courseClass.MaxStudents)
                    {
                        await transaction.RollbackAsync(CancellationToken.None);
                        return this.ApiBadRequest($"Lớp {courseClass.ClassCode} đã đạt sĩ số tối đa ({activeCount}/{courseClass.MaxStudents}).");
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
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return Ok(new { success = true, message = "Ghi danh học viên thành công.", id = enrollment.Id, item = enrollment });
            }
            catch (OperationCanceledException)
            {
                // Dispose của transaction sẽ tự ROLLBACK và nhả advisory lock
                return this.ApiCancelled();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể ghi danh học viên {UserId} vào lớp {ClassId}.", dto.UserId, dto.ClassId);
                await transaction.RollbackAsync(CancellationToken.None);
                return this.ApiServerError("Không thể ghi danh học viên.");
            }
        }

        [HttpPost("/Admin/api/Enrollments/UpdateStatus/{id}")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] EnrollmentStatusDto? dto, CancellationToken cancellationToken)
        {
            if (dto == null)
            {
                return this.ApiBadRequest("Dữ liệu cập nhật không hợp lệ.");
            }

            var newStatus = (dto.Status ?? string.Empty).Trim().ToLowerInvariant();
            if (!ValidStatuses.Contains(newStatus))
            {
                return this.ApiBadRequest("Trạng thái ghi danh phải là active, completed hoặc cancelled.");
            }

            int? targetClassId;
            try
            {
                targetClassId = await _context.Enrollments
                    .AsNoTracking()
                    .Where(e => e.Id == id)
                    .Select(e => (int?)e.ClassId)
                    .FirstOrDefaultAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return this.ApiCancelled();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể tra cứu ghi danh {EnrollmentId}.", id);
                return this.ApiServerError("Không thể cập nhật trạng thái ghi danh.");
            }

            if (targetClassId == null)
            {
                return this.ApiNotFound("Không tìm thấy bản ghi ghi danh.");
            }

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                // Khóa giao dịch mức PostgreSQL (pg_advisory_xact_lock) theo ClassId trước khi load tracked entity để tránh đọc snapshot cũ trước khi giữ khóa.
                var lockKey = 100_000_000L + targetClassId.Value;
                await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({lockKey})", cancellationToken);

                var enrollment = await _context.Enrollments.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
                if (enrollment == null)
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiNotFound("Không tìm thấy bản ghi ghi danh.");
                }

                if (newStatus == "active" && enrollment.Status != "active")
                {
                    var courseClass = await _context.CourseClasses
                        .AsNoTracking()
                        .Where(c => c.Id == enrollment.ClassId)
                        .Select(c => new { c.ClassCode, c.Status, c.MaxStudents })
                        .FirstOrDefaultAsync(cancellationToken);
                    if (courseClass == null)
                    {
                        await transaction.RollbackAsync(CancellationToken.None);
                        return this.ApiNotFound("Không tìm thấy lớp học của bản ghi ghi danh.");
                    }

                    if (courseClass.Status != "upcoming" && courseClass.Status != "ongoing")
                    {
                        await transaction.RollbackAsync(CancellationToken.None);
                        var statusLabel = courseClass.Status == "completed" ? "đã kết thúc" : "đã bị hủy";
                        return this.ApiBadRequest($"Không thể kích hoạt lại ghi danh vì lớp {courseClass.ClassCode} {statusLabel}.");
                    }

                    var duplicateActive = await _context.Enrollments
                        .AnyAsync(e => e.Id != enrollment.Id && e.UserId == enrollment.UserId && e.ClassId == enrollment.ClassId && e.Status == "active", cancellationToken);
                    if (duplicateActive)
                    {
                        await transaction.RollbackAsync(CancellationToken.None);
                        return this.ApiBadRequest("Học viên này đã có một ghi danh active khác trong cùng lớp.");
                    }

                    var activeCount = await _context.Enrollments
                        .CountAsync(e => e.ClassId == enrollment.ClassId && e.Status == "active", cancellationToken);
                    if (activeCount >= courseClass.MaxStudents)
                    {
                        await transaction.RollbackAsync(CancellationToken.None);
                        return this.ApiBadRequest($"Lớp {courseClass.ClassCode} đã đạt sĩ số tối đa ({activeCount}/{courseClass.MaxStudents}).");
                    }
                }

                enrollment.Status = newStatus;
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return Ok(new { success = true, message = "Cập nhật trạng thái ghi danh thành công.", status = enrollment.Status });
            }
            catch (OperationCanceledException)
            {
                return this.ApiCancelled();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể cập nhật trạng thái ghi danh {EnrollmentId}.", id);
                await transaction.RollbackAsync(CancellationToken.None);
                return this.ApiServerError("Không thể cập nhật trạng thái ghi danh.");
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
