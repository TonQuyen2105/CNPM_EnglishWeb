using System.Net.Mail;
using CNPM_EnglishLearning.Data;
using CNPM_EnglishLearning.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CNPM_EnglishLearning.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class TeachersController : Controller
    {
        private const int MaxFullNameLength = 100;
        private const int MaxEmailLength = 150;
        private const int MaxQualificationLength = 200;
        private const int MaxBioLength = 2000;
        private const long TeacherLockOffset = 300_000_000L;

        private readonly ApplicationDbContext _context;
        private readonly ILogger<TeachersController> _logger;

        public TeachersController(ApplicationDbContext context, ILogger<TeachersController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: /Admin/Teachers
        public IActionResult Index()
        {
            return View("~/Areas/Admin/Views/teachers.html");
        }

        [HttpGet("/Admin/api/Teachers/List")]
        public async Task<IActionResult> List(string? search, string? verified, CancellationToken cancellationToken)
        {
            try
            {
                var verifyStats = await (
                    from t in _context.Teachers.AsNoTracking()
                    join u in _context.Users.AsNoTracking() on t.UserId equals u.Id
                    where u.Role == "teacher"
                    group new { t.IsVerified, u.IsActive } by new { t.IsVerified, u.IsActive } into g
                    select new
                    {
                        g.Key.IsVerified,
                        g.Key.IsActive,
                        Count = g.Count()
                    }
                ).ToListAsync(cancellationToken);

                var totalCount = verifyStats.Sum(x => x.Count);
                var verifiedCount = verifyStats.Where(x => x.IsVerified && x.IsActive).Sum(x => x.Count);
                var pendingCount = verifyStats.Where(x => !x.IsVerified && x.IsActive).Sum(x => x.Count);

                var query =
                    from t in _context.Teachers.AsNoTracking()
                    join u in _context.Users.AsNoTracking() on t.UserId equals u.Id
                    where u.Role == "teacher"
                    select new
                    {
                        id = t.Id,
                        userId = u.Id,
                        fullName = u.FullName,
                        email = u.Email,
                        avatarUrl = u.AvatarUrl,
                        isUserActive = u.IsActive,
                        qualification = t.Qualification,
                        bio = t.Bio,
                        isVerified = t.IsVerified,
                        createdAt = t.CreatedAt,
                        updatedAt = t.UpdatedAt
                    };

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var term = search.Trim().ToLower();
                    query = query.Where(x =>
                        x.fullName.ToLower().Contains(term) ||
                        x.email.ToLower().Contains(term) ||
                        (x.qualification != null && x.qualification.ToLower().Contains(term)) ||
                        (x.bio != null && x.bio.ToLower().Contains(term)));
                }

                if (!string.IsNullOrWhiteSpace(verified) && !verified.Equals("all", StringComparison.OrdinalIgnoreCase))
                {
                    var norm = verified.Trim().ToLowerInvariant();
                    if (norm is "1" or "true" or "verified")
                    {
                        query = query.Where(x => x.isVerified);
                    }
                    else if (norm is "0" or "false" or "pending")
                    {
                        query = query.Where(x => !x.isVerified);
                    }
                    else if (norm is "inactive" or "locked")
                    {
                        query = query.Where(x => !x.isUserActive);
                    }
                }

                var teachers = await query
                    .OrderByDescending(x => x.isUserActive)
                    .ThenBy(x => x.isVerified)
                    .ThenByDescending(x => x.id)
                    .ToListAsync(cancellationToken);

                if (teachers.Count == 0)
                {
                    return Ok(new
                    {
                        success = true,
                        items = Array.Empty<object>(),
                        stats = new { totalCount, verifiedCount, pendingCount }
                    });
                }

                var teacherIds = teachers.Select(t => t.id).ToList();

                var assignedClasses = await (
                    from ct in _context.ClassTeachers.AsNoTracking()
                    join cc in _context.CourseClasses.AsNoTracking() on ct.ClassId equals cc.Id
                    join c in _context.Courses.AsNoTracking() on cc.CourseId equals c.Id
                    where teacherIds.Contains(ct.TeacherId)
                    orderby cc.StartDate descending, cc.Id descending
                    select new
                    {
                        ct.TeacherId,
                        classId = cc.Id,
                        classCode = cc.ClassCode,
                        className = cc.ClassName,
                        courseTitle = c.Title,
                        role = ct.Role,
                        status = cc.Status
                    }
                ).ToListAsync(cancellationToken);

                var classesByTeacher = assignedClasses
                    .GroupBy(x => x.TeacherId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(x => new
                        {
                            x.classId,
                            x.classCode,
                            x.className,
                            x.courseTitle,
                            x.role,
                            x.status
                        }).ToList()
                    );

                var teacherSchedules = await (
                    from cs in _context.ClassSchedules.AsNoTracking()
                    join ct in _context.ClassTeachers.AsNoTracking() on cs.ClassId equals ct.ClassId
                    join cc in _context.CourseClasses.AsNoTracking() on cs.ClassId equals cc.Id
                    join cr in _context.Classrooms.AsNoTracking() on cs.ClassroomId equals cr.Id
                    join ts in _context.TimeSlots.AsNoTracking() on cs.TimeSlotId equals ts.Id
                    where teacherIds.Contains(ct.TeacherId) && (cc.Status == "upcoming" || cc.Status == "ongoing")
                    orderby cs.DayOfWeek, ts.StartTime
                    select new
                    {
                        ct.TeacherId,
                        scheduleId = cs.Id,
                        classId = cc.Id,
                        classCode = cc.ClassCode,
                        classroomName = cr.Name,
                        dayOfWeek = cs.DayOfWeek,
                        slotName = ts.Name,
                        startTime = ts.StartTime,
                        endTime = ts.EndTime
                    }
                ).ToListAsync(cancellationToken);

                var schedulesByTeacher = teacherSchedules
                    .GroupBy(x => x.TeacherId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(x => new
                        {
                            x.scheduleId,
                            x.classId,
                            x.classCode,
                            x.classroomName,
                            x.dayOfWeek,
                            dayLabel = FormatDayFull(x.dayOfWeek),
                            x.slotName,
                            timeRange = $"{x.startTime:hh\\:mm} - {x.endTime:hh\\:mm}"
                        }).ToList()
                    );

                var items = teachers.Select(t =>
                {
                    var clsList = classesByTeacher.TryGetValue(t.id, out var list) ? list : null;
                    var schedList = schedulesByTeacher.TryGetValue(t.id, out var sl) ? sl : null;
                    var activeClassCount = clsList?
                        .Where(c => c.status == "upcoming" || c.status == "ongoing")
                        .Select(c => c.classId)
                        .Distinct()
                        .Count() ?? 0;

                    return new
                    {
                        t.id,
                        t.userId,
                        t.fullName,
                        t.email,
                        t.avatarUrl,
                        t.isUserActive,
                        t.qualification,
                        t.bio,
                        t.isVerified,
                        t.createdAt,
                        t.updatedAt,
                        classCount = clsList?.Select(c => c.classId).Distinct().Count() ?? 0,
                        activeClassCount,
                        scheduleCount = schedList?.Count ?? 0,
                        classes = clsList ?? (object)Array.Empty<object>(),
                        schedules = schedList ?? (object)Array.Empty<object>()
                    };
                }).ToList();

                return Ok(new
                {
                    success = true,
                    items,
                    stats = new { totalCount, verifiedCount, pendingCount }
                });
            }
            catch (OperationCanceledException)
            {
                return this.ApiCancelled();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể tải danh sách giảng viên.");
                return this.ApiServerError("Không thể tải danh sách giảng viên.");
            }
        }

        [HttpPost("/Admin/api/Teachers/Create")]
        public async Task<IActionResult> Create([FromBody] TeacherCreateDto? dto, CancellationToken cancellationToken)
        {
            if (dto == null)
            {
                return this.ApiBadRequest("Dữ liệu gửi lên không hợp lệ.");
            }

            var fullName = (dto.FullName ?? string.Empty).Trim();
            var email = (dto.Email ?? string.Empty).Trim();
            var qualification = (dto.Qualification ?? string.Empty).Trim();
            var bio = string.IsNullOrWhiteSpace(dto.Bio) ? null : dto.Bio.Trim();
            var password = string.IsNullOrWhiteSpace(dto.Password) ? "123456" : dto.Password.Trim();

            if (fullName.Length == 0 || fullName.Length > MaxFullNameLength)
            {
                return this.ApiBadRequest($"Họ và tên giảng viên không hợp lệ (1 - {MaxFullNameLength} ký tự).");
            }
            if (email.Length == 0 || email.Length > MaxEmailLength || !IsValidEmail(email))
            {
                return this.ApiBadRequest("Địa chỉ Email giảng viên không hợp lệ.");
            }
            if (qualification.Length == 0 || qualification.Length > MaxQualificationLength)
            {
                return this.ApiBadRequest($"Trình độ / Chứng chỉ không hợp lệ (1 - {MaxQualificationLength} ký tự).");
            }
            if (bio != null && bio.Length > MaxBioLength)
            {
                return this.ApiBadRequest($"Giới thiệu kinh nghiệm không được vượt quá {MaxBioLength} ký tự.");
            }
            if (password.Length < 6 || password.Length > 255)
            {
                return this.ApiBadRequest("Mật khẩu tài khoản giảng viên phải từ 6 đến 255 ký tự.");
            }

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var emailLower = email.ToLowerInvariant();
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == emailLower, cancellationToken);

                var now = DateTime.UtcNow;
                User targetUser;

                if (existingUser != null)
                {
                    long teacherLockKey = 600_000_000L + existingUser.Id;
                    await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({teacherLockKey})", cancellationToken);

                    var hasTeacherProfile = await _context.Teachers
                        .AsNoTracking()
                        .AnyAsync(t => t.UserId == existingUser.Id, cancellationToken);
                    if (hasTeacherProfile)
                    {
                        await transaction.RollbackAsync(CancellationToken.None);
                        return this.ApiBadRequest("Email này đã có hồ sơ giảng viên trong hệ thống.");
                    }

                    if (!string.Equals(existingUser.Role, "teacher", StringComparison.OrdinalIgnoreCase))
                    {
                        await transaction.RollbackAsync(CancellationToken.None);
                        return this.ApiBadRequest($"Email này đã thuộc tài khoản vai trò '{existingUser.Role}'. Vui lòng dùng email khác hoặc đổi vai trò trước.");
                    }

                    if (dto.IsVerified == true && !existingUser.IsActive)
                    {
                        await transaction.RollbackAsync(CancellationToken.None);
                        return this.ApiBadRequest("Tài khoản liên kết đang bị khóa nên không thể đặt trạng thái đã xác minh.");
                    }

                    existingUser.FullName = fullName;
                    existingUser.UpdatedAt = now;
                    targetUser = existingUser;
                }
                else
                {
                    targetUser = new User
                    {
                        FullName = fullName,
                        Email = email,
                        Password = password,
                        Role = "teacher",
                        IsActive = true,
                        CreatedAt = now,
                        UpdatedAt = now
                    };
                    _context.Users.Add(targetUser);
                    try
                    {
                        await _context.SaveChangesAsync(cancellationToken);
                    }
                    catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
                    {
                        await transaction.RollbackAsync(CancellationToken.None);
                        return this.ApiBadRequest("Email giảng viên đã tồn tại trong hệ thống.");
                    }
                }

                var teacher = new Models.Entities.Teacher
                {
                    UserId = targetUser.Id,
                    Qualification = qualification,
                    Bio = bio,
                    IsVerified = dto.IsVerified ?? false,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                _context.Teachers.Add(teacher);
                try
                {
                    await _context.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiBadRequest("Tài khoản này đã có hồ sơ giảng viên trong hệ thống.");
                }
                await transaction.CommitAsync(cancellationToken);

                return Ok(new
                {
                    success = true,
                    message = "Thêm giảng viên vào hệ thống thành công.",
                    id = teacher.Id
                });
            }
            catch (OperationCanceledException)
            {
                return this.ApiCancelled();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể thêm giảng viên mới.");
                await transaction.RollbackAsync(CancellationToken.None);
                return this.ApiServerError("Không thể thêm giảng viên mới.");
            }
        }

        [HttpPost("/Admin/api/Teachers/Update/{id}")]
        [HttpPut("/Admin/api/Teachers/Update/{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] TeacherUpdateDto? dto, CancellationToken cancellationToken)
        {
            if (id <= 0)
            {
                return this.ApiBadRequest("Mã giảng viên không hợp lệ.");
            }

            if (dto == null)
            {
                return this.ApiBadRequest("Dữ liệu cập nhật không hợp lệ.");
            }

            var fullName = (dto.FullName ?? string.Empty).Trim();
            var qualification = (dto.Qualification ?? string.Empty).Trim();
            var bio = string.IsNullOrWhiteSpace(dto.Bio) ? null : dto.Bio.Trim();

            if (fullName.Length == 0 || fullName.Length > MaxFullNameLength)
            {
                return this.ApiBadRequest($"Họ và tên không hợp lệ (1 - {MaxFullNameLength} ký tự).");
            }
            if (qualification.Length == 0 || qualification.Length > MaxQualificationLength)
            {
                return this.ApiBadRequest($"Trình độ / Chứng chỉ không hợp lệ (1 - {MaxQualificationLength} ký tự).");
            }
            if (bio != null && bio.Length > MaxBioLength)
            {
                return this.ApiBadRequest($"Giới thiệu kinh nghiệm không được vượt quá {MaxBioLength} ký tự.");
            }

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await AcquireTeacherLockAsync(id, cancellationToken);

                var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
                if (teacher == null)
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiNotFound("Không tìm thấy hồ sơ giảng viên.");
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == teacher.UserId, cancellationToken);
                if (user == null || !string.Equals(user.Role, "teacher", StringComparison.OrdinalIgnoreCase))
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiBadRequest("Tài khoản liên kết không còn vai trò giảng viên.");
                }

                if (dto.IsVerified == true && !user.IsActive)
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiBadRequest("Không thể xác minh hồ sơ vì tài khoản giảng viên đang bị khóa.");
                }

                if (dto.IsVerified == false && teacher.IsVerified)
                {
                    var activeClasses = await GetActiveClassCodesAsync(id, cancellationToken);
                    if (activeClasses.Count > 0)
                    {
                        await transaction.RollbackAsync(CancellationToken.None);
                        return this.ApiBadRequest($"Không thể thu hồi xác minh vì giảng viên đang phụ trách lớp đang mở/diễn ra ({string.Join(", ", activeClasses)}).");
                    }
                }

                var now = DateTime.UtcNow;

                user.FullName = fullName;
                user.UpdatedAt = now;

                teacher.Qualification = qualification;
                teacher.Bio = bio;
                if (dto.IsVerified.HasValue)
                {
                    teacher.IsVerified = dto.IsVerified.Value;
                }
                teacher.UpdatedAt = now;

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return Ok(new
                {
                    success = true,
                    message = "Cập nhật hồ sơ giảng viên thành công.",
                    id = teacher.Id
                });
            }
            catch (OperationCanceledException)
            {
                return this.ApiCancelled();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể cập nhật hồ sơ giảng viên {TeacherId}.", id);
                await transaction.RollbackAsync(CancellationToken.None);
                return this.ApiServerError("Không thể cập nhật hồ sơ giảng viên.");
            }
        }

        [HttpPost("/Admin/api/Teachers/Verify/{id}")]
        public Task<IActionResult> Verify(int id, CancellationToken cancellationToken)
            => SetVerificationAsync(id, targetIsVerified: true, cancellationToken);

        [HttpPost("/Admin/api/Teachers/ToggleVerify/{id}")]
        public Task<IActionResult> ToggleVerify(int id, CancellationToken cancellationToken)
            => SetVerificationAsync(id, targetIsVerified: null, cancellationToken);

        private async Task<IActionResult> SetVerificationAsync(int id, bool? targetIsVerified, CancellationToken cancellationToken)
        {
            if (id <= 0)
            {
                return this.ApiBadRequest("Mã giảng viên không hợp lệ.");
            }

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await AcquireTeacherLockAsync(id, cancellationToken);

                var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
                if (teacher == null)
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiNotFound("Không tìm thấy hồ sơ giảng viên.");
                }

                var nextVerified = targetIsVerified ?? !teacher.IsVerified;
                if (nextVerified)
                {
                    var user = await _context.Users
                        .AsNoTracking()
                        .Where(u => u.Id == teacher.UserId)
                        .Select(u => new { u.Role, u.IsActive })
                        .FirstOrDefaultAsync(cancellationToken);

                    if (user == null || !string.Equals(user.Role, "teacher", StringComparison.OrdinalIgnoreCase))
                    {
                        await transaction.RollbackAsync(CancellationToken.None);
                        return this.ApiBadRequest("Không thể xác minh hồ sơ vì tài khoản liên kết không còn vai trò giảng viên.");
                    }

                    if (!user.IsActive)
                    {
                        await transaction.RollbackAsync(CancellationToken.None);
                        return this.ApiBadRequest("Không thể xác minh hồ sơ vì tài khoản giảng viên đang bị khóa.");
                    }
                }
                else if (teacher.IsVerified)
                {
                    var activeClasses = await GetActiveClassCodesAsync(id, cancellationToken);
                    if (activeClasses.Count > 0)
                    {
                        await transaction.RollbackAsync(CancellationToken.None);
                        return this.ApiBadRequest($"Không thể thu hồi xác minh vì giảng viên đang phụ trách lớp đang mở/diễn ra ({string.Join(", ", activeClasses)}).");
                    }
                }

                teacher.IsVerified = nextVerified;
                teacher.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                var message = targetIsVerified == true
                    ? "Đã duyệt hồ sơ giảng viên thành công."
                    : teacher.IsVerified
                        ? "Đã xác minh hồ sơ giảng viên thành công."
                        : "Đã thu hồi xác minh hồ sơ giảng viên.";

                return Ok(new
                {
                    success = true,
                    message,
                    id = teacher.Id,
                    isVerified = teacher.IsVerified
                });
            }
            catch (OperationCanceledException)
            {
                return this.ApiCancelled();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể cập nhật trạng thái xác minh giảng viên {TeacherId}.", id);
                await transaction.RollbackAsync(CancellationToken.None);
                return this.ApiServerError(targetIsVerified == true
                    ? "Không thể duyệt hồ sơ giảng viên."
                    : "Không thể thay đổi trạng thái xác minh giảng viên.");
            }
        }

        private Task<List<string>> GetActiveClassCodesAsync(int teacherId, CancellationToken cancellationToken)
        {
            return (
                from ct in _context.ClassTeachers.AsNoTracking()
                join cc in _context.CourseClasses.AsNoTracking() on ct.ClassId equals cc.Id
                where ct.TeacherId == teacherId && (cc.Status == "upcoming" || cc.Status == "ongoing")
                orderby cc.ClassCode
                select cc.ClassCode
            ).Distinct().ToListAsync(cancellationToken);
        }

        private Task AcquireTeacherLockAsync(int teacherId, CancellationToken cancellationToken)
        {
            long teacherLockKey = TeacherLockOffset + teacherId;
            return _context.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock({teacherLockKey})",
                cancellationToken);
        }

        private static string FormatDayFull(int day) => day switch
        {
            1 => "Thứ 2",
            2 => "Thứ 3",
            3 => "Thứ 4",
            4 => "Thứ 5",
            5 => "Thứ 6",
            6 => "Thứ 7",
            7 => "Chủ Nhật",
            _ => $"Thứ {day}"
        };

        private static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new MailAddress(email);
                return string.Equals(addr.Address, email, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        public class TeacherCreateDto
        {
            public string? FullName { get; set; }
            public string? Email { get; set; }
            public string? Password { get; set; }
            public string? Qualification { get; set; }
            public string? Bio { get; set; }
            public bool? IsVerified { get; set; }
        }

        public class TeacherUpdateDto
        {
            public string? FullName { get; set; }
            public string? Qualification { get; set; }
            public string? Bio { get; set; }
            public bool? IsVerified { get; set; }
        }
    }
}