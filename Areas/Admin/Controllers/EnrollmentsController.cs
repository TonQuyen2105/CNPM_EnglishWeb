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

        private static readonly HashSet<string> ValidPaymentMethods = new(StringComparer.OrdinalIgnoreCase)
        {
            "bank_transfer", "cash"
        };

        private static readonly HashSet<string> ValidPaymentStatuses = new(StringComparer.OrdinalIgnoreCase)
        {
            "pending", "completed", "failed", "refunded"
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
            string? paymentStatus,
            int? classId,
            string? search,
            int page = 1,
            int pageSize = 15,
            CancellationToken cancellationToken = default,
            bool lookups = true)
        {
            try
            {
                await AdminCoreHelpers.SyncExpiredClassesAsync(_context, cancellationToken);

                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 15;
                if (pageSize > 50) pageSize = 50;

                var query = BuildFilteredEnrollmentsQuery(status, paymentStatus, classId, search);

                var totalItems = await query.CountAsync(cancellationToken);
                var totalPages = totalItems > 0 ? (int)Math.Ceiling(totalItems / (double)pageSize) : 1;
                if (page > totalPages) page = totalPages;

                var rows = await query
                    .OrderByDescending(x => x.EnrolledAt)
                    .ThenByDescending(x => x.Id)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken);

                var paymentLookup = await LoadLatestPaymentsAsync(rows, cancellationToken);

                var items = rows.Select(x =>
                {
                    var pay = ResolvePaymentForRow(paymentLookup, x);
                    return new
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
                        classStatus = x.ClassStatus,
                        enrolledAt = x.EnrolledAt,
                        status = x.Status,
                        paymentId = pay?.Id,
                        paymentStatus = pay?.Status,
                        paymentMethod = pay?.PaymentMethod,
                        paidAmount = pay?.Amount,
                        transactionCode = pay?.TransactionCode,
                        paidAt = pay?.PaidAt ?? pay?.CreatedAt,
                        paymentNote = pay?.Note
                    };
                }).ToList();

                var pagination = new
                {
                    page,
                    pageSize,
                    totalItems,
                    totalPages
                };

                var completedStats = await (
                    from p in _context.Payments.AsNoTracking()
                    join cc in _context.CourseClasses.AsNoTracking() on p.ClassId equals cc.Id
                    where p.Status == "completed"
                          && cc.Status != "cancelled"
                          && _context.Enrollments.Any(e =>
                              (e.Id == p.EnrollmentId || (p.EnrollmentId == null && e.UserId == p.UserId && e.ClassId == p.ClassId))
                              && e.Status != "cancelled")
                    group p by 1 into g
                    select new
                    {
                        Count = g.Count(),
                        StudentCount = g.Select(p => p.UserId).Distinct().Count(),
                        TotalAmount = g.Sum(p => (decimal?)p.Amount) ?? 0m
                    }
                ).FirstOrDefaultAsync(cancellationToken);

                var pendingPaymentCount = await (
                    from e in _context.Enrollments.AsNoTracking()
                    join cc in _context.CourseClasses.AsNoTracking() on e.ClassId equals cc.Id
                    where e.Status != "cancelled"
                          && cc.Status != "cancelled"
                          && _context.Payments
                              .Where(p => p.EnrollmentId == e.Id || (p.EnrollmentId == null && p.UserId == e.UserId && p.ClassId == e.ClassId))
                              .OrderByDescending(p => p.Id)
                              .Select(p => p.Status)
                              .FirstOrDefault() == "pending"
                    select e.Id
                ).CountAsync(cancellationToken);

                var paidEnrollmentCount = await (
                    from e in _context.Enrollments.AsNoTracking()
                    join cc in _context.CourseClasses.AsNoTracking() on e.ClassId equals cc.Id
                    where e.Status != "cancelled"
                          && cc.Status != "cancelled"
                          && _context.Payments
                              .Where(p => p.EnrollmentId == e.Id || (p.EnrollmentId == null && p.UserId == e.UserId && p.ClassId == e.ClassId))
                              .OrderByDescending(p => p.Id)
                              .Select(p => p.Status)
                              .FirstOrDefault() == "completed"
                    select e.Id
                ).CountAsync(cancellationToken);

                var unpaidEnrollmentCount = await (
                    from e in _context.Enrollments.AsNoTracking()
                    join cc in _context.CourseClasses.AsNoTracking() on e.ClassId equals cc.Id
                    where (e.Status == "active" || e.Status == "completed")
                          && cc.Status != "cancelled"
                          && (_context.Payments
                              .Where(p => p.EnrollmentId == e.Id || (p.EnrollmentId == null && p.UserId == e.UserId && p.ClassId == e.ClassId))
                              .OrderByDescending(p => p.Id)
                              .Select(p => p.Status)
                              .FirstOrDefault() ?? string.Empty) != "completed"
                    select e.Id
                ).CountAsync(cancellationToken);

                var paymentStats = new
                {
                    totalPaidStudents = completedStats?.StudentCount ?? 0,
                    totalPaidTransactions = completedStats?.Count ?? 0,
                    totalPaidEnrollments = paidEnrollmentCount,
                    totalCollectedAmount = completedStats?.TotalAmount ?? 0m,
                    pendingPaymentCount,
                    unpaidEnrollmentCount
                };

                if (!lookups)
                {
                    return Ok(new { success = true, items, pagination, paymentStats });
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

                return Ok(new { success = true, items, pagination, paymentStats, students, classes });
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
            string? paymentStatus,
            int? classId,
            string? search,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await AdminCoreHelpers.SyncExpiredClassesAsync(_context, cancellationToken);

                var rows = await BuildFilteredEnrollmentsQuery(status, paymentStatus, classId, search)
                    .OrderByDescending(x => x.EnrolledAt)
                    .ThenByDescending(x => x.Id)
                    .ToListAsync(cancellationToken);

                var paymentLookup = await LoadLatestPaymentsAsync(rows, cancellationToken);

                var sb = new StringBuilder();
                sb.AppendLine("STT,Học viên,Email,Mã lớp,Tên lớp,Khóa học,Ngày ghi danh,Học phí (VND),Trạng thái,Thanh toán,Phương thức,Mã giao dịch");

                for (var i = 0; i < rows.Count; i++)
                {
                    var row = rows[i];
                    var pay = ResolvePaymentForRow(paymentLookup, row);
                    var isCancelled = string.Equals(row.Status, "cancelled", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(row.ClassStatus, "cancelled", StringComparison.OrdinalIgnoreCase);
                    var fee = isCancelled
                        ? 0m
                        : (pay != null
                            ? (string.Equals(pay.Status, "completed", StringComparison.OrdinalIgnoreCase) ? pay.Amount : 0m)
                            : row.Price);
                    sb.AppendLine(string.Join(",",
                        (i + 1).ToString(CultureInfo.InvariantCulture),
                        AdminCoreHelpers.EscapeCsv(row.StudentName),
                        AdminCoreHelpers.EscapeCsv(row.StudentEmail),
                        AdminCoreHelpers.EscapeCsv(row.ClassCode),
                        AdminCoreHelpers.EscapeCsv(row.ClassName),
                        AdminCoreHelpers.EscapeCsv(row.CourseTitle),
                        AdminCoreHelpers.EscapeCsv(row.EnrolledAt.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)),
                        fee.ToString("0", CultureInfo.InvariantCulture),
                        AdminCoreHelpers.EscapeCsv(AdminCoreHelpers.FormatEnrollmentStatusVi(row.Status)),
                        AdminCoreHelpers.EscapeCsv(FormatPaymentStatusVi(pay?.Status)),
                        AdminCoreHelpers.EscapeCsv(FormatPaymentMethodVi(pay?.PaymentMethod)),
                        AdminCoreHelpers.EscapeCsv(pay?.TransactionCode ?? string.Empty)));
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

            string? paymentMethod = string.IsNullOrWhiteSpace(dto.PaymentMethod) ? null : dto.PaymentMethod.Trim().ToLowerInvariant();
            string paymentStatus = string.IsNullOrWhiteSpace(dto.PaymentStatus) ? "completed" : dto.PaymentStatus.Trim().ToLowerInvariant();
            if (paymentMethod != null)
            {
                if (!ValidPaymentMethods.Contains(paymentMethod))
                {
                    return this.ApiBadRequest("Phương thức thanh toán không hợp lệ (chỉ chấp nhận bank_transfer hoặc cash).");
                }
                if (!ValidPaymentStatuses.Contains(paymentStatus))
                {
                    return this.ApiBadRequest("Trạng thái thanh toán không hợp lệ (pending, completed, failed, refunded).");
                }
                if (dto.PaidAmount.HasValue && dto.PaidAmount.Value < 0)
                {
                    return this.ApiBadRequest("Số tiền thanh toán không được nhỏ hơn 0.");
                }
                if (paymentStatus == "completed" && status == "cancelled")
                {
                    return this.ApiBadRequest("Không thể ghi nhận thanh toán hoàn tất (completed) cho ghi danh ở trạng thái đã hủy (cancelled).");
                }
            }

            try
            {
                await AdminCoreHelpers.SyncExpiredClassesAsync(_context, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return this.ApiCancelled();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không thể đồng bộ lớp hết hạn trước khi ghi danh.");
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
                    .FirstOrDefaultAsync(c => c.Id == dto.ClassId, cancellationToken);
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

                        var nowUtc = DateTime.UtcNow;
                        existingEnrollment.Status = "active";
                        existingEnrollment.EnrolledAt = nowUtc;
                        if (string.Equals(courseClass.Status, "upcoming", StringComparison.OrdinalIgnoreCase))
                        {
                            courseClass.Status = "ongoing";
                            courseClass.UpdatedAt = nowUtc;
                        }

                        try
                        {
                            await _context.SaveChangesAsync(cancellationToken);
                            await AdminCoreHelpers.SyncExpiredClassesAsync(_context, cancellationToken);
                        }
                        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
                        {
                            await transaction.RollbackAsync(CancellationToken.None);
                            return this.ApiBadRequest("Học viên này đã được ghi danh vào lớp.");
                        }

                        if (paymentMethod != null)
                        {
                            var payAmount = dto.PaidAmount ?? courseClass.Price;
                            if (payAmount < 0)
                            {
                                await transaction.RollbackAsync(CancellationToken.None);
                                return this.ApiBadRequest("Số tiền thanh toán không được nhỏ hơn 0.");
                            }

                            var payNow = DateTime.UtcNow;
                            _context.Payments.Add(new Payment
                            {
                                UserId = dto.UserId,
                                ClassId = dto.ClassId,
                                EnrollmentId = existingEnrollment.Id,
                                TransactionCode = GenerateTransactionCode(existingEnrollment.Id, payNow),
                                Amount = payAmount,
                                PaymentMethod = paymentMethod,
                                Status = paymentStatus,
                                PaidAt = paymentStatus == "completed" ? payNow : null,
                                Note = string.IsNullOrWhiteSpace(dto.PaymentNote) ? null : dto.PaymentNote.Trim(),
                                CreatedAt = payNow,
                                UpdatedAt = payNow
                            });
                            try
                            {
                                await _context.SaveChangesAsync(cancellationToken);
                            }
                            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
                            {
                                await transaction.RollbackAsync(CancellationToken.None);
                                return this.ApiBadRequest("Mã giao dịch thanh toán bị trùng lặp. Vui lòng thử lại.");
                            }
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

                var enrollNow = DateTime.UtcNow;
                var enrollment = new Enrollment
                {
                    UserId = dto.UserId,
                    ClassId = dto.ClassId,
                    EnrolledAt = enrollNow,
                    Status = status
                };

                _context.Enrollments.Add(enrollment);
                if (status == "active" && string.Equals(courseClass.Status, "upcoming", StringComparison.OrdinalIgnoreCase))
                {
                    courseClass.Status = "ongoing";
                    courseClass.UpdatedAt = enrollNow;
                }

                try
                {
                    await _context.SaveChangesAsync(cancellationToken);
                    await AdminCoreHelpers.SyncExpiredClassesAsync(_context, cancellationToken);
                }
                catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiBadRequest("Học viên này đã được ghi danh vào lớp.");
                }

                if (paymentMethod != null)
                {
                    var payAmount = dto.PaidAmount ?? courseClass.Price;
                    if (payAmount < 0)
                    {
                        await transaction.RollbackAsync(CancellationToken.None);
                        return this.ApiBadRequest("Số tiền thanh toán không được nhỏ hơn 0.");
                    }

                    var payNow = DateTime.UtcNow;
                    _context.Payments.Add(new Payment
                    {
                        UserId = dto.UserId,
                        ClassId = dto.ClassId,
                        EnrollmentId = enrollment.Id,
                        TransactionCode = GenerateTransactionCode(enrollment.Id, payNow),
                        Amount = payAmount,
                        PaymentMethod = paymentMethod,
                        Status = paymentStatus,
                        PaidAt = paymentStatus == "completed" ? payNow : null,
                        Note = string.IsNullOrWhiteSpace(dto.PaymentNote) ? null : dto.PaymentNote.Trim(),
                        CreatedAt = payNow,
                        UpdatedAt = payNow
                    });
                    try
                    {
                        await _context.SaveChangesAsync(cancellationToken);
                    }
                    catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
                    {
                        await transaction.RollbackAsync(CancellationToken.None);
                        return this.ApiBadRequest("Mã giao dịch thanh toán bị trùng lặp. Vui lòng thử lại.");
                    }
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

            try
            {
                await AdminCoreHelpers.SyncExpiredClassesAsync(_context, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return this.ApiCancelled();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không thể đồng bộ lớp hết hạn trước khi ghi danh.");
            }

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                // Khóa giao dịch tăng dần: Lớp (100M) -> Ca học shared (700M khi kích hoạt) -> Học viên (800M)
                var classLockKey = ClassLockOffset + targetClassId;
                await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({classLockKey})", cancellationToken);
                if (newStatus == "active")
                {
                    await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock_shared({TimeSlotLockKey})", cancellationToken);
                }
                if (newStatus is "active" or "cancelled")
                {
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
                    .FirstOrDefaultAsync(c => c.Id == enrollment.ClassId, cancellationToken);
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
                    var studentValid = await _context.Users
                        .AsNoTracking()
                        .AnyAsync(u => u.Id == enrollment.UserId && u.Role == "student" && u.IsActive, cancellationToken);
                    if (!studentValid)
                    {
                        await transaction.RollbackAsync(CancellationToken.None);
                        return this.ApiBadRequest("Không thể kích hoạt lại ghi danh vì tài khoản học viên đã bị khóa hoặc đã đổi vai trò.");
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
                if (newStatus == "active" && string.Equals(courseClass.Status, "upcoming", StringComparison.OrdinalIgnoreCase))
                {
                    courseClass.Status = "ongoing";
                    courseClass.UpdatedAt = DateTime.UtcNow;
                }
                if (newStatus == "cancelled")
                {
                    var relatedPayments = await _context.Payments
                        .Where(p => p.EnrollmentId == enrollment.Id
                                    || (p.EnrollmentId == null && p.UserId == enrollment.UserId && p.ClassId == enrollment.ClassId))
                        .ToListAsync(cancellationToken);
                    var payNow = DateTime.UtcNow;
                    foreach (var pay in relatedPayments)
                    {
                        pay.EnrollmentId ??= enrollment.Id;
                        if (string.Equals(pay.Status, "pending", StringComparison.OrdinalIgnoreCase))
                        {
                            pay.Status = "failed";
                            pay.UpdatedAt = payNow;
                        }
                        else if (string.Equals(pay.Status, "completed", StringComparison.OrdinalIgnoreCase))
                        {
                            pay.Status = "refunded";
                            pay.UpdatedAt = payNow;
                        }
                    }
                }
                await _context.SaveChangesAsync(cancellationToken);
                await AdminCoreHelpers.SyncExpiredClassesAsync(_context, cancellationToken);
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

        [HttpGet("/Admin/api/Enrollments/PaymentDetail/{id}")]
        public async Task<IActionResult> PaymentDetail(int id, CancellationToken cancellationToken)
        {
            if (id <= 0)
            {
                return this.ApiBadRequest("Mã ghi danh không hợp lệ.");
            }

            try
            {
                await AdminCoreHelpers.SyncExpiredClassesAsync(_context, cancellationToken);

                var enrollmentInfo = await (
                    from e in _context.Enrollments.AsNoTracking()
                    join u in _context.Users.AsNoTracking() on e.UserId equals u.Id
                    join cc in _context.CourseClasses.AsNoTracking() on e.ClassId equals cc.Id
                    join c in _context.Courses.AsNoTracking() on cc.CourseId equals c.Id
                    where e.Id == id
                    select new
                    {
                        enrollmentId = e.Id,
                        enrolledAt = e.EnrolledAt,
                        enrollmentStatus = e.Status,
                        userId = u.Id,
                        fullName = u.FullName,
                        email = u.Email,
                        isUserActive = u.IsActive,
                        userCreatedAt = u.CreatedAt,
                        classId = cc.Id,
                        classCode = cc.ClassCode,
                        className = cc.ClassName,
                        classStatus = cc.Status,
                        tuitionFee = cc.Price,
                        courseId = c.Id,
                        courseTitle = c.Title
                    }
                ).FirstOrDefaultAsync(cancellationToken);

                if (enrollmentInfo == null)
                {
                    return this.ApiNotFound("Không tìm thấy hồ sơ ghi danh.");
                }

                var enrollmentPayments = await _context.Payments
                    .AsNoTracking()
                    .Where(p => p.EnrollmentId == id
                                || (p.EnrollmentId == null && p.UserId == enrollmentInfo.userId && p.ClassId == enrollmentInfo.classId))
                    .OrderByDescending(p => p.Id)
                    .Select(p => new
                    {
                        id = p.Id,
                        transactionCode = p.TransactionCode,
                        amount = p.Amount,
                        paymentMethod = p.PaymentMethod,
                        status = p.Status,
                        paidAt = p.PaidAt,
                        note = p.Note,
                        createdAt = p.CreatedAt,
                        updatedAt = p.UpdatedAt
                    })
                    .ToListAsync(cancellationToken);

                var studentPaymentHistory = await (
                    from p in _context.Payments.AsNoTracking()
                    join cc in _context.CourseClasses.AsNoTracking() on p.ClassId equals cc.Id
                    join c in _context.Courses.AsNoTracking() on cc.CourseId equals c.Id
                    where p.UserId == enrollmentInfo.userId
                    orderby p.PaidAt ?? p.CreatedAt descending, p.Id descending
                    select new
                    {
                        id = p.Id,
                        enrollmentId = p.EnrollmentId,
                        classId = cc.Id,
                        classCode = cc.ClassCode,
                        className = cc.ClassName,
                        classStatus = cc.Status,
                        courseTitle = c.Title,
                        transactionCode = p.TransactionCode,
                        amount = p.Amount,
                        paymentMethod = p.PaymentMethod,
                        status = p.Status,
                        paidAt = p.PaidAt,
                        createdAt = p.CreatedAt,
                        note = p.Note
                    }
                ).ToListAsync(cancellationToken);

                var studentEnrollments = await _context.Enrollments
                    .AsNoTracking()
                    .Where(e => e.UserId == enrollmentInfo.userId)
                    .Select(e => new { e.Id, e.ClassId, e.Status })
                    .ToListAsync(cancellationToken);

                var enrollmentStatusById = studentEnrollments.ToDictionary(e => e.Id, e => e.Status);
                var enrollmentStatusByClass = studentEnrollments
                    .GroupBy(e => e.ClassId)
                    .ToDictionary(g => g.Key, g => g.First().Status);

                var totalStudentPaid = studentPaymentHistory
                    .Where(p =>
                    {
                        if (!string.Equals(p.status, "completed", StringComparison.OrdinalIgnoreCase)) return false;
                        if (string.Equals(p.classStatus, "cancelled", StringComparison.OrdinalIgnoreCase)) return false;
                        string? enrStatus = null;
                        if (p.enrollmentId.HasValue)
                        {
                            enrollmentStatusById.TryGetValue(p.enrollmentId.Value, out enrStatus);
                        }
                        if (enrStatus == null)
                        {
                            enrollmentStatusByClass.TryGetValue(p.classId, out enrStatus);
                        }
                        return enrStatus != null && !string.Equals(enrStatus, "cancelled", StringComparison.OrdinalIgnoreCase);
                    })
                    .Sum(p => p.amount);

                return Ok(new
                {
                    success = true,
                    student = new
                    {
                        id = enrollmentInfo.userId,
                        fullName = enrollmentInfo.fullName,
                        email = enrollmentInfo.email,
                        isActive = enrollmentInfo.isUserActive,
                        createdAt = enrollmentInfo.userCreatedAt,
                        totalStudentPaid
                    },
                    enrollment = new
                    {
                        id = enrollmentInfo.enrollmentId,
                        enrolledAt = enrollmentInfo.enrolledAt,
                        status = enrollmentInfo.enrollmentStatus,
                        classId = enrollmentInfo.classId,
                        classCode = enrollmentInfo.classCode,
                        className = enrollmentInfo.className,
                        classStatus = enrollmentInfo.classStatus,
                        courseId = enrollmentInfo.courseId,
                        courseTitle = enrollmentInfo.courseTitle,
                        tuitionFee = enrollmentInfo.tuitionFee
                    },
                    currentPayment = enrollmentPayments.FirstOrDefault(),
                    enrollmentPayments,
                    studentPaymentHistory
                });
            }
            catch (OperationCanceledException)
            {
                return this.ApiCancelled();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể tải chi tiết thanh toán cho ghi danh {EnrollmentId}.", id);
                return this.ApiServerError("Không thể tải chi tiết thanh toán của học viên.");
            }
        }

        [HttpPost("/Admin/api/Enrollments/{id}/Payment")]
        public async Task<IActionResult> RecordPayment(int id, [FromBody] EnrollmentPaymentDto? dto, CancellationToken cancellationToken)
        {
            if (id <= 0 || dto == null)
            {
                return this.ApiBadRequest("Dữ liệu thanh toán không hợp lệ.");
            }

            var paymentMethod = (dto.PaymentMethod ?? string.Empty).Trim().ToLowerInvariant();
            if (!ValidPaymentMethods.Contains(paymentMethod))
            {
                return this.ApiBadRequest("Phương thức thanh toán không hợp lệ (chỉ chấp nhận bank_transfer hoặc cash).");
            }

            var paymentStatus = string.IsNullOrWhiteSpace(dto.Status) ? "completed" : dto.Status.Trim().ToLowerInvariant();
            if (!ValidPaymentStatuses.Contains(paymentStatus))
            {
                return this.ApiBadRequest("Trạng thái thanh toán không hợp lệ (pending, completed, failed, refunded).");
            }

            if (dto.Amount.HasValue && dto.Amount.Value < 0)
            {
                return this.ApiBadRequest("Số tiền thanh toán không được nhỏ hơn 0.");
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
                    return this.ApiNotFound("Không tìm thấy hồ sơ ghi danh.");
                }

                targetClassId = targetInfo.ClassId;
                targetUserId = targetInfo.UserId;
                await AdminCoreHelpers.SyncExpiredClassesAsync(_context, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return this.ApiCancelled();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể tra cứu hồ sơ ghi danh {EnrollmentId} khi ghi nhận thanh toán.", id);
                return this.ApiServerError("Không thể cập nhật thông tin thanh toán.");
            }

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                // Khóa giao dịch tăng dần: Lớp (100M) -> Học viên (800M) trước khi đọc tracked entity
                long classLockKey = ClassLockOffset + targetClassId;
                await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({classLockKey})", cancellationToken);
                long studentLockKey = StudentLockOffset + targetUserId;
                await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({studentLockKey})", cancellationToken);

                var enrollment = await _context.Enrollments.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
                if (enrollment == null)
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiNotFound("Không tìm thấy hồ sơ ghi danh.");
                }

                var courseClass = await _context.CourseClasses
                    .AsNoTracking()
                    .Where(cc => cc.Id == enrollment.ClassId)
                    .Select(cc => new { cc.Price, cc.Status })
                    .FirstOrDefaultAsync(cancellationToken);
                if (courseClass == null)
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiNotFound("Không tìm thấy lớp học tương ứng với hồ sơ ghi danh.");
                }

                if (paymentStatus == "completed" &&
                    (string.Equals(enrollment.Status, "cancelled", StringComparison.OrdinalIgnoreCase)
                     || string.Equals(courseClass.Status, "cancelled", StringComparison.OrdinalIgnoreCase)))
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiBadRequest("Không thể ghi nhận thanh toán hoàn tất (completed) cho ghi danh hoặc lớp học đã bị hủy.");
                }

                var amount = dto.Amount ?? courseClass.Price;
                if (amount < 0)
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiBadRequest("Số tiền thanh toán không được nhỏ hơn 0.");
                }

                var now = DateTime.UtcNow;
                var existingPayment = await _context.Payments
                    .Where(p => p.EnrollmentId == enrollment.Id || (p.UserId == enrollment.UserId && p.ClassId == enrollment.ClassId))
                    .OrderByDescending(p => p.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                if (existingPayment != null)
                {
                    existingPayment.EnrollmentId ??= enrollment.Id;
                    existingPayment.PaymentMethod = paymentMethod;
                    existingPayment.Status = paymentStatus;
                    existingPayment.Amount = amount;
                    existingPayment.PaidAt = paymentStatus == "completed" ? (existingPayment.PaidAt ?? now) : null;
                    if (dto.Note != null)
                    {
                        existingPayment.Note = string.IsNullOrWhiteSpace(dto.Note) ? null : dto.Note.Trim();
                    }
                    existingPayment.UpdatedAt = now;
                }
                else
                {
                    existingPayment = new Payment
                    {
                        UserId = enrollment.UserId,
                        ClassId = enrollment.ClassId,
                        EnrollmentId = enrollment.Id,
                        TransactionCode = GenerateTransactionCode(enrollment.Id, now),
                        Amount = amount,
                        PaymentMethod = paymentMethod,
                        Status = paymentStatus,
                        PaidAt = paymentStatus == "completed" ? now : null,
                        Note = string.IsNullOrWhiteSpace(dto.Note) ? null : dto.Note.Trim(),
                        CreatedAt = now,
                        UpdatedAt = now
                    };
                    _context.Payments.Add(existingPayment);
                }

                try
                {
                    await _context.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiBadRequest("Mã giao dịch thanh toán bị trùng lặp. Vui lòng thử lại.");
                }
                await transaction.CommitAsync(cancellationToken);

                return Ok(new
                {
                    success = true,
                    message = "Cập nhật thông tin thanh toán thành công.",
                    payment = new
                    {
                        id = existingPayment.Id,
                        transactionCode = existingPayment.TransactionCode,
                        amount = existingPayment.Amount,
                        paymentMethod = existingPayment.PaymentMethod,
                        status = existingPayment.Status,
                        paidAt = existingPayment.PaidAt
                    }
                });
            }
            catch (OperationCanceledException)
            {
                return this.ApiCancelled();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể cập nhật thanh toán cho ghi danh {EnrollmentId}.", id);
                await transaction.RollbackAsync(CancellationToken.None);
                return this.ApiServerError("Không thể cập nhật thông tin thanh toán.");
            }
        }

        private async Task<PaymentLookup> LoadLatestPaymentsAsync(List<EnrollmentListRow> rows, CancellationToken cancellationToken)
        {
            if (rows.Count == 0)
            {
                return new PaymentLookup(new Dictionary<int, PaymentSummaryRow>(), new Dictionary<(int UserId, int ClassId), PaymentSummaryRow>());
            }

            var enrollmentIds = rows.Select(r => r.Id).Distinct().ToList();
            var userIds = rows.Select(r => r.UserId).Distinct().ToList();
            var classIds = rows.Select(r => r.ClassId).Distinct().ToList();

            var rawPayments = await _context.Payments
                .AsNoTracking()
                .Where(p => (p.EnrollmentId != null && enrollmentIds.Contains(p.EnrollmentId.Value))
                            || (userIds.Contains(p.UserId) && classIds.Contains(p.ClassId)))
                .OrderByDescending(p => p.Id)
                .Select(p => new PaymentSummaryRow(p.Id, p.EnrollmentId, p.UserId, p.ClassId, p.Status, p.PaymentMethod, p.Amount, p.TransactionCode, p.PaidAt, p.CreatedAt, p.Note))
                .ToListAsync(cancellationToken);

            var byEnrollment = rawPayments
                .Where(p => p.EnrollmentId.HasValue)
                .GroupBy(p => p.EnrollmentId!.Value)
                .ToDictionary(g => g.Key, g => g.First());

            var byPair = rawPayments
                .Where(p => !p.EnrollmentId.HasValue)
                .GroupBy(p => (p.UserId, p.ClassId))
                .ToDictionary(g => g.Key, g => g.First());

            return new PaymentLookup(byEnrollment, byPair);
        }

        private static PaymentSummaryRow? ResolvePaymentForRow(PaymentLookup lookup, EnrollmentListRow row)
        {
            if (lookup.ByEnrollment.TryGetValue(row.Id, out var pay))
            {
                return pay;
            }
            lookup.ByPair.TryGetValue((row.UserId, row.ClassId), out pay);
            return pay;
        }

        private static string GenerateTransactionCode(int enrollmentId, DateTime utcNow)
            => $"PAY-{utcNow:yyyyMMddHHmmss}-{enrollmentId}-{Guid.NewGuid().ToString("N")[..4].ToUpperInvariant()}";

        private static string FormatPaymentStatusVi(string? status) => (status ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "completed" => "Đã thanh toán",
            "pending" => "Chờ thanh toán",
            "failed" => "Thất bại",
            "refunded" => "Đã hoàn tiền",
            _ => "Chưa có giao dịch"
        };

        private static string FormatPaymentMethodVi(string? method) => (method ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "bank_transfer" => "Chuyển khoản",
            "vnpay" => "VNPAY",
            "momo" => "MoMo",
            "cash" => "Tiền mặt",
            _ => "--"
        };

        private sealed record PaymentSummaryRow(
            int Id,
            int? EnrollmentId,
            int UserId,
            int ClassId,
            string Status,
            string PaymentMethod,
            decimal Amount,
            string TransactionCode,
            DateTime? PaidAt,
            DateTime? CreatedAt,
            string? Note);

        private sealed record PaymentLookup(
            Dictionary<int, PaymentSummaryRow> ByEnrollment,
            Dictionary<(int UserId, int ClassId), PaymentSummaryRow> ByPair);

        private IQueryable<EnrollmentListRow> BuildFilteredEnrollmentsQuery(string? status, string? paymentStatus, int? classId, string? search)
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
                    ClassStatus = cc.Status,
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

            if (!string.IsNullOrWhiteSpace(paymentStatus) && !paymentStatus.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                var normPay = paymentStatus.Trim().ToLowerInvariant();
                if (normPay is "paid" or "completed")
                {
                    query = query.Where(x =>
                        x.Status != "cancelled"
                        && x.ClassStatus != "cancelled"
                        && _context.Payments
                            .Where(p => p.EnrollmentId == x.Id || (p.EnrollmentId == null && p.UserId == x.UserId && p.ClassId == x.ClassId))
                            .OrderByDescending(p => p.Id)
                            .Select(p => p.Status)
                            .FirstOrDefault() == "completed");
                }
                else if (normPay == "unpaid")
                {
                    query = query.Where(x =>
                        (x.Status == "active" || x.Status == "completed")
                        && x.ClassStatus != "cancelled"
                        && (_context.Payments
                            .Where(p => p.EnrollmentId == x.Id || (p.EnrollmentId == null && p.UserId == x.UserId && p.ClassId == x.ClassId))
                            .OrderByDescending(p => p.Id)
                            .Select(p => p.Status)
                            .FirstOrDefault() ?? string.Empty) != "completed");
                }
                else if (ValidPaymentStatuses.Contains(normPay))
                {
                    query = query.Where(x => _context.Payments
                        .Where(p => p.EnrollmentId == x.Id || (p.EnrollmentId == null && p.UserId == x.UserId && p.ClassId == x.ClassId))
                        .OrderByDescending(p => p.Id)
                        .Select(p => p.Status)
                        .FirstOrDefault() == normPay);
                }
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
            public string ClassStatus { get; set; } = string.Empty;
            public DateTime EnrolledAt { get; set; }
            public string Status { get; set; } = string.Empty;
        }

        public class EnrollmentCreateDto
        {
            public int UserId { get; set; }
            public int ClassId { get; set; }
            public string? Status { get; set; }
            public string? PaymentMethod { get; set; }
            public string? PaymentStatus { get; set; }
            public decimal? PaidAmount { get; set; }
            public string? PaymentNote { get; set; }
        }

        public class EnrollmentPaymentDto
        {
            public string PaymentMethod { get; set; } = "bank_transfer";
            public string? Status { get; set; }
            public decimal? Amount { get; set; }
            public string? Note { get; set; }
        }

        public class EnrollmentStatusDto
        {
            public string Status { get; set; } = string.Empty;
        }
    }
}
