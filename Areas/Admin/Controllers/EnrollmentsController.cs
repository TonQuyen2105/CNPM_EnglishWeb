using System.Globalization;
using System.Text;
using CNPM_EnglishLearning.Data;
using CNPM_EnglishLearning.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CNPM_EnglishLearning.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class EnrollmentsController : Controller
    {
        private const long ClassLockOffset = 100_000_000L;
        private const long TimeSlotLockKey = 700_000_000L;
        private const long StudentLockOffset = 800_000_000L;

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
        /// true: trả kèm danh sách học viên + toàn bộ danh sách lớp (phục vụ dropdown lọc lớp & modal ghi danh).
        /// Frontend gửi false khi chỉ lọc/chuyển trang để tránh tải lại dữ liệu không đổi.
        /// </param>
        [HttpGet("/Admin/api/Enrollments/List")]
        public async Task<IActionResult> List(
            string? status,
            int? classId,
            string? search,
            int page = 1,
            int pageSize = 15,
            CancellationToken cancellationToken = default,
            bool lookups = true)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 15;
                if (pageSize > 50) pageSize = 50;

                var query = BuildFilteredEnrollmentsQuery(status, classId, search);

                var totalItems = await query.CountAsync(cancellationToken);
                var totalPages = totalItems > 0 ? (int)Math.Ceiling(totalItems / (double)pageSize) : 1;
                if (page > totalPages) page = totalPages;

                var rows = await query
                    .OrderByDescending(x => x.EnrolledAt)
                    .ThenByDescending(x => x.Id)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken);

                var items = rows.Select(x => new
                {
                    id = x.Id,
                    userId = x.UserId,
                    studentName = x.StudentName,
                    studentEmail = x.StudentEmail,
                    classId = x.ClassId,
                    classCode = x.ClassCode,
                    className = x.ClassName,
                    courseTitle = x.CourseTitle,
                    price = x.Price,
                    enrolledAt = x.EnrolledAt,
                    status = x.Status
                }).ToList();

                var pagination = new
                {
                    page,
                    pageSize,
                    totalItems,
                    totalPages
                };

                if (!lookups)
                {
                    return Ok(new { success = true, items, pagination });
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

                // Trả đủ danh sách lớp để lọc theo lớp (modal ghi danh chỉ hiển thị lớp upcoming/ongoing)
                var rawClasses = await _context.CourseClasses
                    .AsNoTracking()
                    .OrderBy(cc => cc.ClassCode)
                    .Select(cc => new
                    {
                        cc.Id,
                        cc.ClassCode,
                        cc.ClassName,
                        cc.Price,
                        cc.Status,
                        cc.MaxStudents
                    })
                    .ToListAsync(cancellationToken);

                var lookupClassIds = rawClasses.Select(cc => cc.Id).ToList();
                var activeCounts = lookupClassIds.Count == 0
                    ? new Dictionary<int, int>()
                    : await _context.Enrollments
                        .AsNoTracking()
                        .Where(e => lookupClassIds.Contains(e.ClassId) && e.Status == "active")
                        .GroupBy(e => e.ClassId)
                        .Select(g => new { ClassId = g.Key, Count = g.Count() })
                        .ToDictionaryAsync(x => x.ClassId, x => x.Count, cancellationToken);

                var classes = rawClasses.Select(cc => new
                {
                    id = cc.Id,
                    classCode = cc.ClassCode,
                    className = cc.ClassName,
                    price = cc.Price,
                    status = cc.Status,
                    activeStudents = activeCounts.GetValueOrDefault(cc.Id),
                    maxStudents = cc.MaxStudents
                }).ToList();

                return Ok(new { success = true, items, pagination, students, classes });
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

        [HttpGet("/Admin/api/Enrollments/ExportCsv")]
        public async Task<IActionResult> ExportCsv(
            string? status,
            int? classId,
            string? search,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var rows = await BuildFilteredEnrollmentsQuery(status, classId, search)
                    .OrderByDescending(x => x.EnrolledAt)
                    .ThenByDescending(x => x.Id)
                    .ToListAsync(cancellationToken);

                var sb = new StringBuilder();
                sb.AppendLine("STT,Học viên,Email,Mã lớp,Tên lớp,Khóa học,Ngày ghi danh,Học phí (VND),Trạng thái");

                for (var i = 0; i < rows.Count; i++)
                {
                    var row = rows[i];
                    var fee = string.Equals(row.Status, "cancelled", StringComparison.OrdinalIgnoreCase) ? 0m : row.Price;
                    sb.AppendLine(string.Join(",",
                        (i + 1).ToString(CultureInfo.InvariantCulture),
                        AdminCoreHelpers.EscapeCsv(row.StudentName),
                        AdminCoreHelpers.EscapeCsv(row.StudentEmail),
                        AdminCoreHelpers.EscapeCsv(row.ClassCode),
                        AdminCoreHelpers.EscapeCsv(row.ClassName),
                        AdminCoreHelpers.EscapeCsv(row.CourseTitle),
                        AdminCoreHelpers.EscapeCsv(row.EnrolledAt.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)),
                        fee.ToString("0", CultureInfo.InvariantCulture),
                        AdminCoreHelpers.EscapeCsv(AdminCoreHelpers.FormatEnrollmentStatusVi(row.Status))));
                }

                var preamble = Encoding.UTF8.GetPreamble();
                var body = Encoding.UTF8.GetBytes(sb.ToString());
                var bytes = new byte[preamble.Length + body.Length];
                Buffer.BlockCopy(preamble, 0, bytes, 0, preamble.Length);
                Buffer.BlockCopy(body, 0, bytes, preamble.Length, body.Length);

                var dateTag = DateTime.UtcNow.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
                return File(bytes, "text/csv; charset=utf-8", $"danh-sach-ghi-danh-{dateTag}.csv");
            }
            catch (OperationCanceledException)
            {
                return this.ApiCancelled();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể xuất CSV danh sách ghi danh.");
                return this.ApiServerError("Không thể xuất tệp CSV danh sách ghi danh.");
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

                // Khóa giao dịch tăng dần: Lớp (100M) -> Ca học shared (700M khi kích hoạt) -> Học viên (800M)
                long classLockKey = ClassLockOffset + dto.ClassId;
                await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({classLockKey})", cancellationToken);
                if (status == "active")
                {
                    await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock_shared({TimeSlotLockKey})", cancellationToken);
                }
                long studentLockKey = StudentLockOffset + dto.UserId;
                await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({studentLockKey})", cancellationToken);

                var courseClass = await _context.CourseClasses
                    .AsNoTracking()
                    .Where(c => c.Id == dto.ClassId)
                    .Select(c => new { c.ClassCode, c.Status, c.MaxStudents, c.StartDate, c.EndDate })
                    .FirstOrDefaultAsync(cancellationToken);
                if (courseClass == null || (courseClass.Status != "upcoming" && courseClass.Status != "ongoing"))
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiBadRequest("Lớp học không tồn tại hoặc không còn nhận ghi danh (chỉ nhận lớp upcoming hoặc ongoing).");
                }

                if (status == "completed" && courseClass.Status == "upcoming")
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiBadRequest($"Không thể tạo ghi danh ở trạng thái hoàn thành (completed) cho lớp {courseClass.ClassCode} đang sắp khai giảng (upcoming).");
                }

                var existingEnrollment = await _context.Enrollments
                    .FirstOrDefaultAsync(e => e.UserId == dto.UserId && e.ClassId == dto.ClassId, cancellationToken);

                if (existingEnrollment != null)
                {
                    var existingStatus = (existingEnrollment.Status ?? string.Empty).Trim().ToLowerInvariant();
                    if (existingStatus == "active")
                    {
                        await transaction.RollbackAsync(CancellationToken.None);
                        return this.ApiBadRequest("Học viên này đã được ghi danh vào lớp.");
                    }

                    if (existingStatus == "completed")
                    {
                        await transaction.RollbackAsync(CancellationToken.None);
                        return this.ApiBadRequest("Học viên đã hoàn thành lớp học này.");
                    }

                    if (existingStatus == "cancelled" && status == "active")
                    {
                        var activeCount = await _context.Enrollments
                            .CountAsync(e => e.ClassId == dto.ClassId && e.Status == "active", cancellationToken);
                        if (activeCount >= courseClass.MaxStudents)
                        {
                            await transaction.RollbackAsync(CancellationToken.None);
                            return this.ApiBadRequest($"Lớp {courseClass.ClassCode} đã đạt sĩ số tối đa ({activeCount}/{courseClass.MaxStudents}).");
                        }

                        var scheduleConflict = await AdminCoreHelpers.CheckStudentScheduleConflictAsync(
                            _context,
                            dto.UserId,
                            dto.ClassId,
                            courseClass.ClassCode,
                            courseClass.StartDate,
                            courseClass.EndDate,
                            cancellationToken);
                        if (scheduleConflict != null)
                        {
                            await transaction.RollbackAsync(CancellationToken.None);
                            return this.ApiBadRequest(scheduleConflict);
                        }

                        existingEnrollment.Status = "active";
                        existingEnrollment.EnrolledAt = DateTime.UtcNow;

                        try
                        {
                            await _context.SaveChangesAsync(cancellationToken);
                        }
                        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
                        {
                            await transaction.RollbackAsync(CancellationToken.None);
                            return this.ApiBadRequest("Học viên này đã được ghi danh vào lớp.");
                        }

                        await transaction.CommitAsync(cancellationToken);
                        return Ok(new
                        {
                            success = true,
                            message = $"Đã kích hoạt lại ghi danh cho học viên {student.FullName} vào lớp {courseClass.ClassCode}.",
                            id = existingEnrollment.Id,
                            item = existingEnrollment
                        });
                    }

                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiBadRequest($"Học viên {student.FullName} đã có hồ sơ ghi danh ở trạng thái '{existingStatus}' trong lớp {courseClass.ClassCode}; chỉ có thể tái kích hoạt sang trạng thái active.");
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

                    var scheduleConflict = await AdminCoreHelpers.CheckStudentScheduleConflictAsync(
                        _context,
                        dto.UserId,
                        dto.ClassId,
                        courseClass.ClassCode,
                        courseClass.StartDate,
                        courseClass.EndDate,
                        cancellationToken);
                    if (scheduleConflict != null)
                    {
                        await transaction.RollbackAsync(CancellationToken.None);
                        return this.ApiBadRequest(scheduleConflict);
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
                try
                {
                    await _context.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiBadRequest("Học viên này đã được ghi danh vào lớp.");
                }
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
            if (id <= 0)
            {
                return this.ApiBadRequest("Mã bản ghi ghi danh không hợp lệ.");
            }

            if (dto == null)
            {
                return this.ApiBadRequest("Dữ liệu cập nhật không hợp lệ.");
            }

            var newStatus = (dto.Status ?? string.Empty).Trim().ToLowerInvariant();
            if (!ValidStatuses.Contains(newStatus))
            {
                return this.ApiBadRequest("Trạng thái ghi danh phải là active, completed hoặc cancelled.");
            }

            int targetClassId;
            int targetUserId;
            try
            {
                var targetInfo = await _context.Enrollments
                    .AsNoTracking()
                    .Where(e => e.Id == id)
                    .Select(e => new { e.ClassId, e.UserId })
                    .FirstOrDefaultAsync(cancellationToken);
                if (targetInfo == null)
                {
                    return this.ApiNotFound("Không tìm thấy bản ghi ghi danh.");
                }

                targetClassId = targetInfo.ClassId;
                targetUserId = targetInfo.UserId;
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

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                // Khóa giao dịch tăng dần: Lớp (100M) -> Ca học shared (700M) -> Học viên (800M)
                var classLockKey = ClassLockOffset + targetClassId;
                await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({classLockKey})", cancellationToken);
                if (newStatus == "active")
                {
                    await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock_shared({TimeSlotLockKey})", cancellationToken);
                    var studentLockKey = StudentLockOffset + targetUserId;
                    await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({studentLockKey})", cancellationToken);
                }

                var enrollment = await _context.Enrollments.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
                if (enrollment == null)
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiNotFound("Không tìm thấy bản ghi ghi danh.");
                }

                var courseClass = await _context.CourseClasses
                    .AsNoTracking()
                    .Where(c => c.Id == enrollment.ClassId)
                    .Select(c => new { c.ClassCode, c.Status, c.MaxStudents, c.StartDate, c.EndDate })
                    .FirstOrDefaultAsync(cancellationToken);
                if (courseClass == null)
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiNotFound("Không tìm thấy lớp học của bản ghi ghi danh.");
                }

                if (newStatus == "completed" && (courseClass.Status == "upcoming" || courseClass.Status == "cancelled"))
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    var reason = courseClass.Status == "upcoming" ? "chưa khai giảng (upcoming)" : "đã bị hủy (cancelled)";
                    return this.ApiBadRequest($"Không thể chuyển ghi danh sang hoàn thành (completed) vì lớp {courseClass.ClassCode} {reason}.");
                }

                if (newStatus == "active" && enrollment.Status != "active")
                {
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

                    var scheduleConflict = await AdminCoreHelpers.CheckStudentScheduleConflictAsync(
                        _context,
                        enrollment.UserId,
                        enrollment.ClassId,
                        courseClass.ClassCode,
                        courseClass.StartDate,
                        courseClass.EndDate,
                        cancellationToken);
                    if (scheduleConflict != null)
                    {
                        await transaction.RollbackAsync(CancellationToken.None);
                        return this.ApiBadRequest(scheduleConflict);
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

        private IQueryable<EnrollmentListRow> BuildFilteredEnrollmentsQuery(string? status, int? classId, string? search)
        {
            var query =
                from e in _context.Enrollments.AsNoTracking()
                join u in _context.Users.AsNoTracking() on e.UserId equals u.Id
                join cc in _context.CourseClasses.AsNoTracking() on e.ClassId equals cc.Id
                join c in _context.Courses.AsNoTracking() on cc.CourseId equals c.Id
                select new EnrollmentListRow
                {
                    Id = e.Id,
                    UserId = u.Id,
                    StudentName = u.FullName,
                    StudentEmail = u.Email,
                    ClassId = cc.Id,
                    ClassCode = cc.ClassCode,
                    ClassName = cc.ClassName,
                    CourseTitle = c.Title,
                    Price = cc.Price,
                    EnrolledAt = e.EnrolledAt,
                    Status = e.Status
                };

            if (classId.HasValue && classId.Value > 0)
            {
                var targetClassId = classId.Value;
                query = query.Where(x => x.ClassId == targetClassId);
            }

            if (!string.IsNullOrWhiteSpace(status) && !status.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                var normalizedStatus = status.Trim().ToLowerInvariant();
                query = query.Where(x => x.Status == normalizedStatus);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var pattern = AdminCoreHelpers.BuildContainsLikePattern(search.Trim());
                query = query.Where(x =>
                    EF.Functions.ILike(x.StudentName, pattern, "\\") ||
                    EF.Functions.ILike(x.StudentEmail, pattern, "\\") ||
                    EF.Functions.ILike(x.ClassCode, pattern, "\\") ||
                    EF.Functions.ILike(x.ClassName, pattern, "\\") ||
                    EF.Functions.ILike(x.CourseTitle, pattern, "\\"));
            }

            return query;
        }

        private sealed class EnrollmentListRow
        {
            public int Id { get; set; }
            public int UserId { get; set; }
            public string StudentName { get; set; } = string.Empty;
            public string StudentEmail { get; set; } = string.Empty;
            public int ClassId { get; set; }
            public string ClassCode { get; set; } = string.Empty;
            public string ClassName { get; set; } = string.Empty;
            public string CourseTitle { get; set; } = string.Empty;
            public decimal Price { get; set; }
            public DateTime EnrolledAt { get; set; }
            public string Status { get; set; } = string.Empty;
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
