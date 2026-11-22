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
    public class ClassesController : Controller
    {
        // Khớp [MaxLength] của Entity CourseClass (ClassCode = 50, ClassName = 150).
        private const int MaxClassCodeLength = 50;
        private const int MaxClassNameLength = 150;
        private const int MaxStudentsLimit = 1000;
        private const decimal MaxPrice = 99_999_999.99m;

        // Không gian khóa pg_advisory_xact_lock tăng dần: Lớp (100M) -> Phòng (200M) -> Giảng viên (300M) -> Ca học (700M) -> Học viên (800M)
        private const long ClassLockOffset = 100_000_000L;
        private const long ClassroomLockOffset = 200_000_000L;
        private const long TeacherLockOffset = 300_000_000L;
        private const long TimeSlotLockKey = 700_000_000L;
        private const long StudentLockOffset = 800_000_000L;

        private readonly ApplicationDbContext _context;
        private readonly ILogger<ClassesController> _logger;

        private static readonly HashSet<string> ValidClassStatuses = new(StringComparer.OrdinalIgnoreCase)
        {
            "upcoming", "ongoing", "completed", "cancelled"
        };

        private static readonly HashSet<string> ValidEnrollmentStatuses = new(StringComparer.OrdinalIgnoreCase)
        {
            "active", "completed", "cancelled"
        };

        private static readonly HashSet<string> ValidTeacherRoles = new(StringComparer.OrdinalIgnoreCase)
        {
            "main", "assistant"
        };

        public ClassesController(ApplicationDbContext context, ILogger<ClassesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View("~/Areas/Admin/Views/classes.html");
        }

        public IActionResult Detail(int id)
        {
            return View("~/Areas/Admin/Views/class-detail.html");
        }

        /// <param name="lookups">
        /// true: trả kèm danh sách khóa học + giảng viên (dropdown lọc & modal tạo/sửa lớp).
        /// Frontend chỉ yêu cầu ở lần tải đầu tiên; các lần lọc/tìm kiếm sau gửi false.
        /// </param>
        [HttpGet("/Admin/api/Classes/List")]
        public async Task<IActionResult> List(
            string? status,
            string? search,
            int? courseId,
            int? teacherId,
            CancellationToken cancellationToken,
            bool lookups = true)
        {
            try
            {
                await AdminCoreHelpers.SyncExpiredClassesAsync(_context, cancellationToken);

                var query = _context.CourseClasses.AsNoTracking().AsQueryable();

                if (courseId.HasValue && courseId.Value > 0)
                {
                    query = query.Where(c => c.CourseId == courseId.Value);
                }

                if (teacherId.HasValue && teacherId.Value > 0)
                {
                    var targetTeacherId = teacherId.Value;
                    query = query.Where(c => _context.ClassTeachers.Any(ct => ct.ClassId == c.Id && ct.TeacherId == targetTeacherId));
                }

                if (!string.IsNullOrWhiteSpace(status) && !status.Equals("all", StringComparison.OrdinalIgnoreCase))
                {
                    var normalizedStatus = status.Trim().ToLowerInvariant();
                    query = query.Where(c => c.Status == normalizedStatus);
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var pattern = AdminCoreHelpers.BuildContainsLikePattern(search.Trim());
                    query = query.Where(c =>
                        EF.Functions.ILike(c.ClassCode, pattern, "\\") ||
                        EF.Functions.ILike(c.ClassName, pattern, "\\") ||
                        (c.Course != null && EF.Functions.ILike(c.Course.Title, pattern, "\\")));
                }

                // Projection: chỉ lấy đúng các cột cần hiển thị (không Include / kéo cả entity Course + CourseClass)
                var classes = await query
                    .OrderByDescending(c => c.Id)
                    .Select(c => new
                    {
                        c.Id,
                        c.CourseId,
                        CourseTitle = c.Course != null ? c.Course.Title : null,
                        c.ClassCode,
                        c.ClassName,
                        c.StartDate,
                        c.EndDate,
                        c.MaxStudents,
                        c.Price,
                        c.Status
                    })
                    .ToListAsync(cancellationToken);

                var allCoursesWithSubject = await (
                    from c in _context.Courses.AsNoTracking()
                    join s in _context.Subjects.AsNoTracking() on c.SubjectId equals s.Id into sJoin
                    from subj in sJoin.DefaultIfEmpty()
                    orderby c.Title
                    select new
                    {
                        c.Id,
                        c.SubjectId,
                        SubjectName = subj != null ? subj.Name : null,
                        c.Title,
                        c.Price,
                        c.Level
                    }
                ).ToListAsync(cancellationToken);

                var coursesBySubjectId = allCoursesWithSubject
                    .GroupBy(c => c.SubjectId)
                    .ToDictionary(g => g.Key, g => g.OrderBy(x => x.Id).ToList());

                var courseDisplayById = allCoursesWithSubject.ToDictionary(
                    c => c.Id,
                    c =>
                    {
                        var siblings = coursesBySubjectId.TryGetValue(c.SubjectId, out var sList) ? sList : [c];
                        var subjectTotal = siblings.Sum(x => x.Price);
                        var breakdown = string.Join(" + ", siblings.Select(x => x.Title));
                        var displayTitle = !string.IsNullOrWhiteSpace(c.SubjectName) && siblings.Count > 1
                            ? $"{c.SubjectName}: {c.Title} (Tổng khóa {c.SubjectName}: {breakdown})"
                            : (!string.IsNullOrWhiteSpace(c.SubjectName) ? $"{c.SubjectName} — {c.Title}" : c.Title);
                        return new
                        {
                            c.Id,
                            c.SubjectId,
                            SubjectName = c.SubjectName ?? c.Title,
                            c.Title,
                            DisplayTitle = displayTitle,
                            CoursePrice = c.Price,
                            SubjectTotalPrice = subjectTotal,
                            SiblingCount = siblings.Count,
                            Breakdown = breakdown,
                            c.Level
                        };
                    });

                var courses = lookups
                    ? allCoursesWithSubject.Select(c =>
                    {
                        var info = courseDisplayById[c.Id];
                        return new
                        {
                            id = c.Id,
                            subjectId = c.SubjectId,
                            subjectName = info.SubjectName,
                            title = info.DisplayTitle,
                            rawTitle = c.Title,
                            coursePrice = c.Price,
                            subjectTotalPrice = info.SubjectTotalPrice,
                            price = info.SubjectTotalPrice > 0 ? info.SubjectTotalPrice : c.Price,
                            level = c.Level
                        };
                    }).ToList()
                    : null;

                var teachers = lookups
                    ? await (
                        from t in _context.Teachers.AsNoTracking()
                        join u in _context.Users.AsNoTracking() on t.UserId equals u.Id
                        where t.IsVerified && u.Role == "teacher" && u.IsActive
                        orderby u.FullName
                        select new
                        {
                            id = t.Id,
                            userId = u.Id,
                            fullName = u.FullName,
                            qualification = t.Qualification
                        }
                    ).ToListAsync(cancellationToken)
                    : null;

                var classroomsLookup = lookups
                    ? await BuildClassroomsLookupAsync(cancellationToken)
                    : null;

                var timeSlots = lookups
                    ? await _context.TimeSlots
                        .AsNoTracking()
                        .Where(ts => ts.IsActive)
                        .OrderBy(ts => ts.StartTime)
                        .Select(ts => new
                        {
                            id = ts.Id,
                            name = ts.Name,
                            timeRange = AdminCoreHelpers.FormatTimeRange(ts.StartTime, ts.EndTime)
                        })
                        .ToListAsync(cancellationToken)
                    : null;

                var allRoomNames = await _context.Classrooms
                    .AsNoTracking()
                    .ToDictionaryAsync(r => r.Id, r => r.Name, cancellationToken);

                if (classes.Count == 0)
                {
                    return Ok(new { success = true, items = Array.Empty<object>(), courses, teachers, classrooms = classroomsLookup, timeSlots });
                }

                var classIds = classes.Select(c => c.Id).ToList();

                var mainTeachers = await (
                    from ct in _context.ClassTeachers.AsNoTracking()
                    join t in _context.Teachers.AsNoTracking() on ct.TeacherId equals t.Id
                    join u in _context.Users.AsNoTracking() on t.UserId equals u.Id
                    where classIds.Contains(ct.ClassId) && ct.Role == "main"
                    select new { ct.ClassId, TeacherId = t.Id, TeacherName = u.FullName }
                ).ToListAsync(cancellationToken);

                var teacherByClass = mainTeachers
                    .GroupBy(x => x.ClassId)
                    .ToDictionary(g => g.Key, g => g.First());

                var schedules = await (
                    from cs in _context.ClassSchedules.AsNoTracking()
                    join r in _context.Classrooms.AsNoTracking() on cs.ClassroomId equals r.Id
                    join ts in _context.TimeSlots.AsNoTracking() on cs.TimeSlotId equals ts.Id
                    where classIds.Contains(cs.ClassId)
                    orderby cs.DayOfWeek, ts.StartTime
                    select new
                    {
                        cs.ClassId,
                        cs.ClassroomId,
                        ClassroomName = r.Name,
                        cs.TimeSlotId,
                        cs.DayOfWeek,
                        ts.StartTime,
                        ts.EndTime
                    }
                ).ToListAsync(cancellationToken);

                var schedulesByClass = schedules
                    .GroupBy(x => x.ClassId)
                    .ToDictionary(g => g.Key, g => g.OrderBy(s => s.DayOfWeek).ThenBy(s => s.StartTime).ToList());

                var activeCounts = await _context.Enrollments
                    .AsNoTracking()
                    .Where(e => classIds.Contains(e.ClassId) && e.Status == "active")
                    .GroupBy(e => e.ClassId)
                    .Select(g => new { ClassId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.ClassId, x => x.Count, cancellationToken);

                var items = classes.Select(c =>
                {
                    teacherByClass.TryGetValue(c.Id, out var teacherInfo);
                    schedulesByClass.TryGetValue(c.Id, out var clsSchedules);

                    var isCancelled = string.Equals(c.Status, "cancelled", StringComparison.OrdinalIgnoreCase);
                    var classroomSummary = isCancelled
                        ? "Đã trả phòng"
                        : ((clsSchedules != null && clsSchedules.Count > 0)
                            ? string.Join(", ", clsSchedules.Select(s => s.ClassroomName).Distinct())
                            : "Chưa xếp phòng");

                    var effectiveRoomId = isCancelled
                        ? null
                        : clsSchedules?.Select(s => (int?)s.ClassroomId).FirstOrDefault();

                    var effectiveTimeSlotId = isCancelled
                        ? null
                        : clsSchedules?.Select(s => (int?)s.TimeSlotId).FirstOrDefault();

                    string? effectiveScheduleDays = null;
                    if (!isCancelled && clsSchedules != null && clsSchedules.Count > 0)
                    {
                        var dSet = clsSchedules.Select(s => s.DayOfWeek).Distinct().OrderBy(d => d).ToArray();
                        if (dSet.SequenceEqual(new[] { 1, 3, 5 })) effectiveScheduleDays = "t246";
                        else if (dSet.SequenceEqual(new[] { 2, 4, 6 })) effectiveScheduleDays = "t357";
                        else if (dSet.SequenceEqual(new[] { 1, 2, 3, 4, 5, 6 })) effectiveScheduleDays = "all";
                    }

                    var scheduleSummary = isCancelled
                        ? "Đã hủy lịch học"
                        : ((clsSchedules != null && clsSchedules.Count > 0)
                            ? string.Join("; ", clsSchedules
                                .GroupBy(s => new { s.StartTime, s.EndTime })
                                .Select(g =>
                                {
                                    var days = string.Join("-", g.Select(x => AdminCoreHelpers.FormatDayShort(x.DayOfWeek)).Distinct());
                                    return $"{days} ({AdminCoreHelpers.FormatTimeRange(g.Key.StartTime, g.Key.EndTime)})";
                                }))
                            : "Chưa xếp lịch");

                    var cInfo = courseDisplayById.TryGetValue(c.CourseId, out var ci) ? ci : null;
                    var effectiveCourseTitle = cInfo != null
                        ? (cInfo.SiblingCount > 1
                            ? $"{cInfo.SubjectName} ({cInfo.Breakdown})"
                            : (!string.IsNullOrWhiteSpace(cInfo.SubjectName) && cInfo.SubjectName != cInfo.Title
                                ? $"{cInfo.SubjectName} — {cInfo.Title}"
                                : cInfo.Title))
                        : (c.CourseTitle ?? "--");
                    var effectivePrice = cInfo != null && cInfo.SubjectTotalPrice > 0 && c.Price == cInfo.CoursePrice
                        ? cInfo.SubjectTotalPrice
                        : c.Price;

                    return new
                    {
                        id = c.Id,
                        courseId = c.CourseId,
                        courseTitle = effectiveCourseTitle,
                        classCode = c.ClassCode,
                        className = c.ClassName,
                        classroomId = effectiveRoomId,
                        scheduleDays = effectiveScheduleDays,
                        timeSlotId = effectiveTimeSlotId,
                        teacherId = teacherInfo?.TeacherId,
                        teacherName = teacherInfo?.TeacherName ?? "Chưa phân công",
                        classroomSummary,
                        scheduleSummary,
                        startDate = c.StartDate,
                        endDate = c.EndDate,
                        activeStudents = activeCounts.GetValueOrDefault(c.Id),
                        maxStudents = c.MaxStudents,
                        price = effectivePrice,
                        status = c.Status
                    };
                }).ToList();

                return Ok(new { success = true, items, courses, teachers, classrooms = classroomsLookup, timeSlots });
            }
            catch (OperationCanceledException)
            {
                return this.ApiCancelled();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể tải danh sách lớp học.");
                return this.ApiServerError("Không thể tải danh sách lớp học.");
            }
        }

        [HttpPost("/Admin/api/Classes/Create")]
        public async Task<IActionResult> Create([FromBody] ClassCreateDto? dto, CancellationToken cancellationToken)
        {
            if (dto == null)
            {
                return this.ApiBadRequest("Dữ liệu gửi lên không hợp lệ.");
            }

            var classCode = (dto.ClassCode ?? string.Empty).Trim();
            var className = (dto.ClassName ?? string.Empty).Trim();

            if (classCode.Length == 0 || className.Length == 0)
            {
                return this.ApiBadRequest("Mã lớp và tên lớp không được để trống.");
            }

            if (classCode.Length > MaxClassCodeLength)
            {
                return this.ApiBadRequest($"Mã lớp không được vượt quá {MaxClassCodeLength} ký tự.");
            }

            if (className.Length > MaxClassNameLength)
            {
                return this.ApiBadRequest($"Tên lớp không được vượt quá {MaxClassNameLength} ký tự.");
            }

            if (dto.EndDate.Date <= dto.StartDate.Date)
            {
                return this.ApiBadRequest("Ngày kết thúc phải sau ngày khai giảng.");
            }

            if (dto.MaxStudents <= 0)
            {
                return this.ApiBadRequest("Sĩ số tối đa phải lớn hơn 0.");
            }

            if (dto.MaxStudents > MaxStudentsLimit)
            {
                return this.ApiBadRequest($"Sĩ số tối đa không được vượt quá {MaxStudentsLimit} học viên.");
            }

            if (dto.Price < 0)
            {
                return this.ApiBadRequest("Học phí không được nhỏ hơn 0.");
            }

            if (dto.Price > MaxPrice)
            {
                return this.ApiBadRequest($"Học phí không được vượt quá {MaxPrice:N0} ₫.");
            }

            var status = string.IsNullOrWhiteSpace(dto.Status) ? "upcoming" : dto.Status.Trim().ToLowerInvariant();
            if (status != "upcoming" && status != "ongoing")
            {
                return this.ApiBadRequest("Khi tạo lớp học mới chỉ được chọn trạng thái Sắp khai giảng (upcoming) hoặc Đang diễn ra (ongoing).");
            }
            if (status == "ongoing")
            {
                return this.ApiBadRequest("Khi mới tạo lớp học chưa có học viên đăng ký, trạng thái bắt buộc là Sắp khai giảng (upcoming). Hệ thống sẽ tự động chuyển sang Đang diễn ra khi có đủ giảng viên dạy, phòng/lịch học và có học viên đăng ký.");
            }
            status = "upcoming";

            var teacherId = dto.TeacherId.HasValue && dto.TeacherId.Value > 0 ? dto.TeacherId : null;
            var classroomId = dto.ClassroomId.HasValue && dto.ClassroomId.Value > 0 ? dto.ClassroomId : null;
            var requestedScheduleDays = string.IsNullOrWhiteSpace(dto.ScheduleDays) ? null : dto.ScheduleDays.Trim().ToLowerInvariant();
            if (requestedScheduleDays != null && requestedScheduleDays is not ("t246" or "t357" or "all"))
            {
                return this.ApiBadRequest("Lịch ngày học không hợp lệ (chọn Thứ 2-4-6, Thứ 3-5-7 hoặc Cả tuần).");
            }
            var requestedTimeSlotId = dto.TimeSlotId.HasValue && dto.TimeSlotId.Value > 0 ? dto.TimeSlotId : null;
            if (classroomId.HasValue && (string.IsNullOrEmpty(requestedScheduleDays) || !requestedTimeSlotId.HasValue))
            {
                return this.ApiBadRequest("Khi chọn phòng học cho lớp, vui lòng chọn đầy đủ Lịch ngày học và Ca học để hệ thống xếp vào Thời khóa biểu.");
            }
            if (dto.CourseId <= 0)
            {
                return this.ApiBadRequest("Khóa học được chọn không tồn tại.");
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
                _logger.LogWarning(ex, "Không thể đồng bộ trạng thái lớp hết hạn trước khi tạo lớp {ClassCode}.", classCode);
            }

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                if (classroomId.HasValue)
                {
                    await AcquireAdvisoryLockAsync(ClassroomLockOffset + classroomId.Value, cancellationToken);
                }

                if (classroomId.HasValue)
                {
                    var roomInfo = await _context.Classrooms
                        .AsNoTracking()
                        .Where(r => r.Id == classroomId.Value)
                        .Select(r => new
                        {
                            r.Id,
                            r.Name,
                            r.Capacity,
                            r.IsActive
                        })
                        .FirstOrDefaultAsync(cancellationToken);
                    if (roomInfo == null || !roomInfo.IsActive)
                    {
                        await transaction.RollbackAsync(CancellationToken.None);
                        return this.ApiBadRequest("Phòng học được chọn không tồn tại hoặc đang tạm ngưng sử dụng.");
                    }
                    if (roomInfo.Capacity < dto.MaxStudents)
                    {
                        await transaction.RollbackAsync(CancellationToken.None);
                        return this.ApiBadRequest($"Sức chứa phòng {roomInfo.Name} ({roomInfo.Capacity} chỗ) nhỏ hơn sĩ số tối đa của lớp ({dto.MaxStudents} học viên).");
                    }

                    if (IsActiveClassStatus(status))
                    {
                        var roomConflictMsg = await CheckExclusiveRoomOccupancyAsync(
                            null, classroomId.Value, roomInfo.Name, dto.StartDate.Date, dto.EndDate.Date, cancellationToken);
                        if (roomConflictMsg != null)
                        {
                            await transaction.RollbackAsync(CancellationToken.None);
                            return this.ApiBadRequest(roomConflictMsg);
                        }
                    }
                }

                if (teacherId.HasValue)
                {
                    // Khóa giảng viên (300M + teacherId) trong transaction để tránh race với thu hồi xác minh hoặc xóa tài khoản GV
                    await AcquireAdvisoryLockAsync(TeacherLockOffset + teacherId.Value, cancellationToken);
                }

                if (!await _context.Courses.AsNoTracking().AnyAsync(c => c.Id == dto.CourseId, cancellationToken))
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiBadRequest("Khóa học được chọn không tồn tại.");
                }

                var codePattern = AdminCoreHelpers.EscapeLikePattern(classCode);
                var duplicateCode = await _context.CourseClasses.AsNoTracking()
                    .AnyAsync(c => EF.Functions.ILike(c.ClassCode, codePattern, "\\"), cancellationToken);
                if (duplicateCode)
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiBadRequest($"Mã lớp '{classCode}' đã tồn tại trong hệ thống.");
                }

                if (teacherId.HasValue)
                {
                    var teacherValid = await (
                        from t in _context.Teachers.AsNoTracking()
                        join u in _context.Users.AsNoTracking() on t.UserId equals u.Id
                        where t.Id == teacherId.Value && t.IsVerified && u.Role == "teacher" && u.IsActive
                        select t.Id
                    ).AnyAsync(cancellationToken);
                    if (!teacherValid)
                    {
                        await transaction.RollbackAsync(CancellationToken.None);
                        return this.ApiBadRequest("Giảng viên được chọn không tồn tại, chưa được xác minh hoặc tài khoản đã bị khóa/đổi vai trò.");
                    }
                }

                var newClass = new CourseClass
                {
                    CourseId = dto.CourseId,
                    ClassCode = classCode,
                    ClassName = className,
                    StartDate = DateTime.SpecifyKind(dto.StartDate.Date, DateTimeKind.Unspecified),
                    EndDate = DateTime.SpecifyKind(dto.EndDate.Date, DateTimeKind.Unspecified),
                    MaxStudents = dto.MaxStudents,
                    Price = dto.Price,
                    Status = status,
                    CreatedAt = DateTime.UtcNow
                };

                _context.CourseClasses.Add(newClass);
                await _context.SaveChangesAsync(cancellationToken);

                if (teacherId.HasValue)
                {
                    _context.ClassTeachers.Add(new ClassTeacher
                    {
                        ClassId = newClass.Id,
                        TeacherId = teacherId.Value,
                        Role = "main",
                        AssignedAt = DateTime.UtcNow
                    });
                    await _context.SaveChangesAsync(cancellationToken);
                }

                // Nếu lớp có chọn phòng + lịch thứ + ca học -> tự động xếp lịch vào class_schedules để hiển thị Thời khóa biểu
                var autoDays = ResolveSchedulePatternDays(requestedScheduleDays);
                if (classroomId.HasValue && IsActiveClassStatus(status) && autoDays.Count > 0 && requestedTimeSlotId.HasValue)
                {
                    await AcquireSharedAdvisoryLockAsync(TimeSlotLockKey, cancellationToken);
                    var tsInfo = await _context.TimeSlots
                        .AsNoTracking()
                        .Where(ts => ts.Id == requestedTimeSlotId.Value && ts.IsActive)
                        .Select(ts => new { ts.Id, ts.Name, ts.StartTime, ts.EndTime })
                        .FirstOrDefaultAsync(cancellationToken);

                    if (tsInfo == null)
                    {
                        await transaction.RollbackAsync(CancellationToken.None);
                        return this.ApiBadRequest("Ca học được chọn không tồn tại hoặc đang ngừng sử dụng.");
                    }

                    if (tsInfo != null)
                    {
                        if (teacherId.HasValue)
                        {
                            var busyTeacherHit = await (
                                from cs in _context.ClassSchedules.AsNoTracking()
                                join cc in _context.CourseClasses.AsNoTracking() on cs.ClassId equals cc.Id
                                join ct in _context.ClassTeachers.AsNoTracking() on cc.Id equals ct.ClassId
                                join ts in _context.TimeSlots.AsNoTracking() on cs.TimeSlotId equals ts.Id
                                where cs.ClassId != newClass.Id
                                      && ct.TeacherId == teacherId.Value
                                      && (cc.Status == "upcoming" || cc.Status == "ongoing")
                                      && newClass.StartDate <= cc.EndDate && cc.StartDate <= newClass.EndDate
                                      && autoDays.Contains(cs.DayOfWeek)
                                      && ts.StartTime < tsInfo.EndTime && tsInfo.StartTime < ts.EndTime
                                select new { cs.DayOfWeek, cc.ClassCode, ts.Name, ts.StartTime, ts.EndTime }
                            ).FirstOrDefaultAsync(cancellationToken);

                            if (busyTeacherHit != null)
                            {
                                await transaction.RollbackAsync(CancellationToken.None);
                                return this.ApiBadRequest(
                                    $"Không thể tạo lớp vì lịch học vào {AdminCoreHelpers.FormatDayFull(busyTeacherHit.DayOfWeek)} ({tsInfo.Name}) bị trùng với lớp {busyTeacherHit.ClassCode} của giảng viên đã chọn.");
                            }
                        }

                        var nowSched = DateTime.UtcNow;
                        foreach (var d in autoDays)
                        {
                            _context.ClassSchedules.Add(new ClassSchedule
                            {
                                ClassId = newClass.Id,
                                ClassroomId = classroomId.Value,
                                TimeSlotId = tsInfo.Id,
                                DayOfWeek = d,
                                CreatedAt = nowSched
                            });
                        }
                        await _context.SaveChangesAsync(cancellationToken);
                    }
                }

                await transaction.CommitAsync(cancellationToken);

                return Ok(new { success = true, message = "Tạo lớp học thành công.", id = newClass.Id, item = newClass });
            }
            catch (OperationCanceledException)
            {
                return this.ApiCancelled();
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                // Hai request tạo cùng một mã lớp đồng thời: request chậm hơn vi phạm unique constraint
                await transaction.RollbackAsync(CancellationToken.None);
                return this.ApiBadRequest($"Mã lớp '{classCode}' đã tồn tại trong hệ thống.");
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation })
            {
                await transaction.RollbackAsync(CancellationToken.None);
                return this.ApiBadRequest("Khóa học hoặc giảng viên được chọn không còn tồn tại trong hệ thống.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể tạo lớp học {ClassCode}.", classCode);
                await transaction.RollbackAsync(CancellationToken.None);
                return this.ApiServerError("Không thể tạo lớp học.");
            }
        }

        [HttpPost("/Admin/api/Classes/Update/{id}")]
        [HttpPut("/Admin/api/Classes/Update/{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] ClassUpdateDto? dto, CancellationToken cancellationToken)
        {
            if (id <= 0)
            {
                return this.ApiBadRequest("Mã lớp học không hợp lệ.");
            }

            if (dto == null)
            {
                return this.ApiBadRequest("Dữ liệu gửi lên không hợp lệ.");
            }

            var classCode = (dto.ClassCode ?? string.Empty).Trim();
            var className = (dto.ClassName ?? string.Empty).Trim();

            if (classCode.Length == 0 || className.Length == 0)
            {
                return this.ApiBadRequest("Mã lớp và tên lớp không được để trống.");
            }

            if (classCode.Length > MaxClassCodeLength)
            {
                return this.ApiBadRequest($"Mã lớp không được vượt quá {MaxClassCodeLength} ký tự.");
            }

            if (className.Length > MaxClassNameLength)
            {
                return this.ApiBadRequest($"Tên lớp không được vượt quá {MaxClassNameLength} ký tự.");
            }

            if (dto.EndDate.Date <= dto.StartDate.Date)
            {
                return this.ApiBadRequest("Ngày kết thúc phải sau ngày khai giảng.");
            }

            if (dto.MaxStudents <= 0)
            {
                return this.ApiBadRequest("Sĩ số tối đa phải lớn hơn 0.");
            }

            if (dto.MaxStudents > MaxStudentsLimit)
            {
                return this.ApiBadRequest($"Sĩ số tối đa không được vượt quá {MaxStudentsLimit} học viên.");
            }

            var requestedPrice = dto.PriceOverride ?? dto.Price;
            if (requestedPrice.HasValue)
            {
                if (requestedPrice.Value < 0)
                {
                    return this.ApiBadRequest("Học phí không được nhỏ hơn 0.");
                }

                if (requestedPrice.Value > MaxPrice)
                {
                    return this.ApiBadRequest($"Học phí không được vượt quá {MaxPrice:N0} ₫.");
                }
            }

            string? requestedStatus = null;
            if (!string.IsNullOrWhiteSpace(dto.Status))
            {
                requestedStatus = dto.Status.Trim().ToLowerInvariant();
                if (!ValidClassStatuses.Contains(requestedStatus))
                {
                    return this.ApiBadRequest("Trạng thái lớp học không hợp lệ.");
                }
            }

            var requestedMainTeacherId = dto.MainTeacherId.HasValue && dto.MainTeacherId.Value > 0
                ? dto.MainTeacherId
                : null;
            var requestedClassroomId = dto.ClassroomId.HasValue && dto.ClassroomId.Value > 0
                ? dto.ClassroomId
                : null;
            var requestedScheduleDays = string.IsNullOrWhiteSpace(dto.ScheduleDays)
                ? null
                : dto.ScheduleDays.Trim().ToLowerInvariant();
            if (requestedScheduleDays != null && requestedScheduleDays is not ("t246" or "t357" or "all"))
            {
                return this.ApiBadRequest("Lịch ngày học không hợp lệ (chọn Thứ 2-4-6, Thứ 3-5-7 hoặc Cả tuần).");
            }
            var requestedTimeSlotId = dto.TimeSlotId.HasValue && dto.TimeSlotId.Value > 0
                ? dto.TimeSlotId
                : null;

            try
            {
                if (dto.CourseId <= 0 || !await _context.Courses.AsNoTracking().AnyAsync(c => c.Id == dto.CourseId, cancellationToken))
                {
                    return this.ApiBadRequest("Khóa học được chọn không tồn tại.");
                }

                await AdminCoreHelpers.SyncExpiredClassesAsync(_context, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return this.ApiCancelled();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể kiểm tra khóa học khi cập nhật lớp {ClassId}.", id);
                return this.ApiServerError("Không thể cập nhật lớp học.");
            }

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                // 1. Khóa lớp học trước (100M + classId)
                await AcquireAdvisoryLockAsync(ClassLockOffset + id, cancellationToken);

                var courseClass = await _context.CourseClasses.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
                if (courseClass == null)
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiNotFound("Không tìm thấy lớp học.");
                }

                var codePattern = AdminCoreHelpers.EscapeLikePattern(classCode);
                var duplicateCode = await _context.CourseClasses
                    .AsNoTracking()
                    .AnyAsync(c => c.Id != id && EF.Functions.ILike(c.ClassCode, codePattern, "\\"), cancellationToken);
                if (duplicateCode)
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiBadRequest($"Mã lớp '{classCode}' đã tồn tại trong hệ thống.");
                }

                var activeStudentCount = await _context.Enrollments
                    .AsNoTracking()
                    .CountAsync(e => e.ClassId == id && e.Status == "active", cancellationToken);
                if (dto.MaxStudents < activeStudentCount)
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiBadRequest($"Sĩ số tối đa ({dto.MaxStudents}) không được nhỏ hơn số học viên đang học ({activeStudentCount}).");
                }

                // Chặn thay đổi khóa học trực thuộc nếu lớp đã có ghi danh hoặc nhật ký buổi dạy
                if (dto.CourseId != courseClass.CourseId)
                {
                    var hasClassData = await _context.Enrollments.AsNoTracking().AnyAsync(e => e.ClassId == id, cancellationToken)
                        || await _context.ClassLessons.AsNoTracking().AnyAsync(cl => cl.ClassId == id, cancellationToken);
                    if (hasClassData)
                    {
                        await transaction.RollbackAsync(CancellationToken.None);
                        return this.ApiBadRequest("Không thể đổi khóa học trực thuộc vì lớp đã có bản ghi ghi danh hoặc nhật ký buổi dạy.");
                    }
                }

                var currentClassTeachers = await _context.ClassTeachers
                    .Where(ct => ct.ClassId == id)
                    .ToListAsync(cancellationToken);
                var currentMain = currentClassTeachers.FirstOrDefault(ct => ct.Role == "main");

                var targetStatus = requestedStatus ?? courseClass.Status;
                if (string.Equals(courseClass.Status, "ongoing", StringComparison.OrdinalIgnoreCase))
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiBadRequest(
                        $"Lớp {courseClass.ClassCode} đang diễn ra (ongoing) nên không được chỉnh sửa hoặc hủy.");
                }

                if (string.Equals(courseClass.Status, "cancelled", StringComparison.OrdinalIgnoreCase))
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiBadRequest(
                        $"Lớp {courseClass.ClassCode} đã bị hủy nên không thể chỉnh sửa.");
                }

                if (string.Equals(courseClass.Status, "completed", StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.Equals(targetStatus, "cancelled", StringComparison.OrdinalIgnoreCase))
                    {
                        await transaction.RollbackAsync(CancellationToken.None);
                        return this.ApiBadRequest(
                            $"Lớp {courseClass.ClassCode} đã kết thúc, chỉ được phép đổi trạng thái sang Đã hủy.");
                    }

                    var cancelNow = DateTime.UtcNow;
                    courseClass.Status = "cancelled";
                    courseClass.UpdatedAt = cancelNow;
                    await CancelClassEffectsAsync(id, cancelNow, cancellationToken);
                    await _context.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);

                    return Ok(new
                    {
                        success = true,
                        message = $"Đã hủy lớp {courseClass.ClassCode}, gỡ khỏi Thời khóa biểu và trả lại phòng học trống.",
                        id = courseClass.Id,
                        status = courseClass.Status
                    });
                }

                if (string.Equals(targetStatus, "completed", StringComparison.OrdinalIgnoreCase))
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiBadRequest("Không thể chọn thủ công trạng thái Đã kết thúc; hệ thống sẽ tự động cập nhật khi đến ngày kết thúc.");
                }

                var isCancelling = string.Equals(targetStatus, "cancelled", StringComparison.OrdinalIgnoreCase);

                var newStartDate = DateTime.SpecifyKind(dto.StartDate.Date, DateTimeKind.Unspecified);
                var newEndDate = DateTime.SpecifyKind(dto.EndDate.Date, DateTimeKind.Unspecified);
                var todayVn = AdminCoreHelpers.GetTodayVnDate();

                if (string.Equals(targetStatus, "ongoing", StringComparison.OrdinalIgnoreCase) && newStartDate.Date > todayVn)
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiBadRequest(
                        $"Không thể đặt trạng thái Đang diễn ra cho lớp {courseClass.ClassCode} vì chưa tới ngày khai giảng ({newStartDate:dd/MM/yyyy}).");
                }

                var wasActive = IsActiveClassStatus(courseClass.Status);
                var willBeActive = IsActiveClassStatus(targetStatus);
                var isActivating = !wasActive && willBeActive; // Mở lại từ cancelled hoặc completed -> upcoming/ongoing
                var datesChanged = courseClass.StartDate.Date != newStartDate.Date || courseClass.EndDate.Date != newEndDate.Date;
                var capacityIncreased = dto.MaxStudents > courseClass.MaxStudents;
                var mainTeacherChanged = currentMain?.TeacherId != requestedMainTeacherId;
                var classroomUpdated = dto.ClassroomId.HasValue;

                if (isActivating && newEndDate.Date <= AdminCoreHelpers.GetTodayVnDate())
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiBadRequest(
                        $"Không thể chuyển lớp {courseClass.ClassCode} sang trạng thái {(targetStatus == "upcoming" ? "Sắp diễn ra" : "Đang diễn ra")} vì ngày kết thúc ({newEndDate:dd/MM/yyyy}) đã đến hoặc đã qua hôm nay. Vui lòng chọn ngày kết thúc sau hôm nay.");
                }

                if (requestedMainTeacherId.HasValue)
                {
                    var teacherInfo = await (
                        from t in _context.Teachers.AsNoTracking()
                        join u in _context.Users.AsNoTracking() on t.UserId equals u.Id
                        where t.Id == requestedMainTeacherId.Value
                        select new { t.Id, t.IsVerified, u.Role, u.IsActive, u.FullName }
                    ).FirstOrDefaultAsync(cancellationToken);

                    if (teacherInfo == null || (mainTeacherChanged && (!teacherInfo.IsVerified || teacherInfo.Role != "teacher" || !teacherInfo.IsActive)))
                    {
                        await transaction.RollbackAsync(CancellationToken.None);
                        return this.ApiBadRequest("Giảng viên chính được chọn không tồn tại, chưa được xác minh hoặc tài khoản đã bị khóa/đổi vai trò.");
                    }
                }

                // Xác định danh sách giảng viên sau khi cập nhật (cả main và assistant)
                var resultingTeacherIds = currentClassTeachers
                    .Where(ct => !mainTeacherChanged || ct.Role != "main")
                    .Select(ct => ct.TeacherId)
                    .ToHashSet();
                if (requestedMainTeacherId.HasValue)
                {
                    resultingTeacherIds.Add(requestedMainTeacherId.Value);
                }

                var needRoomActiveAndCapacityCheck = willBeActive && (isActivating || datesChanged || capacityIncreased);
                var needRoomConflictCheck = willBeActive && (isActivating || datesChanged);
                var needAllTeachersConflictCheck = willBeActive && (isActivating || datesChanged);
                var needNewMainTeacherConflictCheck = willBeActive && mainTeacherChanged && requestedMainTeacherId.HasValue;

                var needScheduleLoad = needRoomActiveAndCapacityCheck || needAllTeachersConflictCheck || needNewMainTeacherConflictCheck || classroomUpdated || isActivating;
                var classSchedules = needScheduleLoad
                    ? await LoadClassScheduleWindowsAsync(id, cancellationToken)
                    : new List<ClassScheduleWindow>();

                var oldClassroomId = classSchedules.Select(s => (int?)s.ClassroomId).FirstOrDefault();

                // 2. Khóa các phòng liên quan theo thứ tự tăng dần (200M + classroomId)
                var roomsToLock = classSchedules.Select(s => s.ClassroomId).ToList();
                if (requestedClassroomId.HasValue) roomsToLock.Add(requestedClassroomId.Value);
                foreach (var roomId in roomsToLock.Distinct().OrderBy(x => x))
                {
                    await AcquireAdvisoryLockAsync(ClassroomLockOffset + roomId, cancellationToken);
                }

                if (requestedClassroomId.HasValue && willBeActive)
                {
                    var roomInfo = await _context.Classrooms
                        .AsNoTracking()
                        .Where(r => r.Id == requestedClassroomId.Value)
                        .Select(r => new { r.Name, r.Capacity, r.IsActive })
                        .FirstOrDefaultAsync(cancellationToken);
                    if (roomInfo == null || !roomInfo.IsActive)
                    {
                        await transaction.RollbackAsync(CancellationToken.None);
                        return this.ApiBadRequest("Phòng học được chọn không tồn tại hoặc đang tạm ngưng sử dụng.");
                    }
                    if (roomInfo.Capacity < dto.MaxStudents)
                    {
                        await transaction.RollbackAsync(CancellationToken.None);
                        return this.ApiBadRequest($"Sức chứa phòng {roomInfo.Name} ({roomInfo.Capacity} chỗ) nhỏ hơn sĩ số tối đa của lớp ({dto.MaxStudents} học viên).");
                    }

                    var classroomChanged = oldClassroomId != requestedClassroomId;
                    if (classroomChanged || isActivating || datesChanged)
                    {
                        var roomOccErr = await CheckExclusiveRoomOccupancyAsync(
                            id, requestedClassroomId.Value, roomInfo.Name, newStartDate, newEndDate, cancellationToken);
                        if (roomOccErr != null)
                        {
                            await transaction.RollbackAsync(CancellationToken.None);
                            return this.ApiBadRequest(roomOccErr);
                        }
                    }
                }

                // 3. Khóa các giảng viên liên quan (cả main và assistant) theo thứ tự tăng dần (300M + teacherId)
                var teacherIdsToCheck = new HashSet<int>();
                if (needAllTeachersConflictCheck && classSchedules.Count > 0)
                {
                    foreach (var tid in resultingTeacherIds) teacherIdsToCheck.Add(tid);
                }
                else if (needNewMainTeacherConflictCheck && classSchedules.Count > 0 && requestedMainTeacherId.HasValue)
                {
                    teacherIdsToCheck.Add(requestedMainTeacherId.Value);
                }

                var teacherIdsToLock = new HashSet<int>(teacherIdsToCheck);
                if (mainTeacherChanged && requestedMainTeacherId.HasValue)
                {
                    teacherIdsToLock.Add(requestedMainTeacherId.Value);
                }
                if (willBeActive && classSchedules.Count == 0 && (isActivating || classroomUpdated))
                {
                    foreach (var tid in resultingTeacherIds) teacherIdsToLock.Add(tid);
                }

                foreach (var tid in teacherIdsToLock.OrderBy(x => x))
                {
                    await AcquireAdvisoryLockAsync(TeacherLockOffset + tid, cancellationToken);
                }

                // 4. Khóa chia sẻ Ca học (700M) khi cần đối chiếu lịch
                if ((needRoomConflictCheck || teacherIdsToCheck.Count > 0) && classSchedules.Count > 0)
                {
                    await AcquireSharedAdvisoryLockAsync(TimeSlotLockKey, cancellationToken);
                }

                if (mainTeacherChanged && requestedMainTeacherId.HasValue)
                {
                    var mainTeacherStillValid = await (
                        from t in _context.Teachers.AsNoTracking()
                        join u in _context.Users.AsNoTracking() on t.UserId equals u.Id
                        where t.Id == requestedMainTeacherId.Value && t.IsVerified && u.Role == "teacher" && u.IsActive
                        select t.Id
                    ).AnyAsync(cancellationToken);
                    if (!mainTeacherStillValid)
                    {
                        await transaction.RollbackAsync(CancellationToken.None);
                        return this.ApiBadRequest("Giảng viên chính được chọn không còn hợp lệ (chưa xác minh hoặc tài khoản bị khóa/đổi vai trò).");
                    }
                }

                if (needRoomActiveAndCapacityCheck && classSchedules.Count > 0)
                {
                    var roomConstraintErr = await CheckRoomsActiveAndCapacityAsync(dto.MaxStudents, classSchedules, cancellationToken);
                    if (roomConstraintErr != null)
                    {
                        await transaction.RollbackAsync(CancellationToken.None);
                        return this.ApiBadRequest(roomConstraintErr);
                    }
                }

                if (needRoomConflictCheck && classSchedules.Count > 0)
                {
                    var timeSlotErr = await CheckTimeSlotsActiveAsync(classSchedules, cancellationToken);
                    if (timeSlotErr != null)
                    {
                        await transaction.RollbackAsync(CancellationToken.None);
                        return this.ApiBadRequest(timeSlotErr);
                    }

                    var roomConflict = await CheckRoomScheduleConflictsAsync(id, newStartDate, newEndDate, classSchedules, cancellationToken);
                    if (roomConflict != null)
                    {
                        await transaction.RollbackAsync(CancellationToken.None);
                        return this.ApiBadRequest(roomConflict);
                    }
                }

                if (teacherIdsToCheck.Count > 0 && classSchedules.Count > 0)
                {
                    var checkList = teacherIdsToCheck.OrderBy(x => x).ToList();
                    var teacherNames = await (
                        from t in _context.Teachers.AsNoTracking()
                        join u in _context.Users.AsNoTracking() on t.UserId equals u.Id
                        where checkList.Contains(t.Id)
                        select new { t.Id, u.FullName }
                    ).ToDictionaryAsync(x => x.Id, x => x.FullName, cancellationToken);

                    foreach (var tid in checkList)
                    {
                        var tName = teacherNames.GetValueOrDefault(tid, $"Giảng viên #{tid}");
                        var teacherConflict = await CheckTeacherScheduleConflictsAsync(
                            id,
                            tid,
                            tName,
                            newStartDate,
                            newEndDate,
                            classSchedules,
                            cancellationToken);
                        if (teacherConflict != null)
                        {
                            await transaction.RollbackAsync(CancellationToken.None);
                            return this.ApiBadRequest(teacherConflict);
                        }
                    }
                }

                var now = DateTime.UtcNow;
                courseClass.CourseId = dto.CourseId;
                courseClass.ClassCode = classCode;
                courseClass.ClassName = className;
                courseClass.StartDate = newStartDate;
                courseClass.EndDate = newEndDate;
                courseClass.MaxStudents = dto.MaxStudents;
                if (requestedPrice.HasValue)
                {
                    courseClass.Price = requestedPrice.Value;
                }
                courseClass.Status = targetStatus;
                courseClass.UpdatedAt = now;

                if (isCancelling)
                {
                    await CancelClassEffectsAsync(id, now, cancellationToken);
                }
                else if (dto.ClassroomId.HasValue && !requestedClassroomId.HasValue)
                {
                    await ClearClassSchedulesAsync(id, cancellationToken);
                    classSchedules.Clear();
                }
                else if (requestedClassroomId.HasValue && !string.IsNullOrEmpty(requestedScheduleDays) && requestedTimeSlotId.HasValue)
                {
                    var autoDays = ResolveSchedulePatternDays(requestedScheduleDays);
                    await AcquireSharedAdvisoryLockAsync(TimeSlotLockKey, cancellationToken);
                    var tsInfo = await _context.TimeSlots
                        .AsNoTracking()
                        .Where(ts => ts.Id == requestedTimeSlotId.Value && ts.IsActive)
                        .Select(ts => new { ts.Id, ts.Name, ts.StartTime, ts.EndTime })
                        .FirstOrDefaultAsync(cancellationToken);
                    if (tsInfo == null)
                    {
                        await transaction.RollbackAsync(CancellationToken.None);
                        return this.ApiBadRequest("Ca học được chọn không tồn tại hoặc đang ngừng sử dụng.");
                    }

                    if (resultingTeacherIds.Count > 0)
                    {
                        var busyHit = await (
                            from cs in _context.ClassSchedules.AsNoTracking()
                            join cc in _context.CourseClasses.AsNoTracking() on cs.ClassId equals cc.Id
                            join ct in _context.ClassTeachers.AsNoTracking() on cc.Id equals ct.ClassId
                            join ts in _context.TimeSlots.AsNoTracking() on cs.TimeSlotId equals ts.Id
                            where cs.ClassId != id
                                  && resultingTeacherIds.Contains(ct.TeacherId)
                                  && (cc.Status == "upcoming" || cc.Status == "ongoing")
                                  && newStartDate <= cc.EndDate && cc.StartDate <= newEndDate
                                  && autoDays.Contains(cs.DayOfWeek)
                                  && ts.StartTime < tsInfo.EndTime && tsInfo.StartTime < ts.EndTime
                            select new { cs.DayOfWeek, cc.ClassCode }
                        ).FirstOrDefaultAsync(cancellationToken);
                        if (busyHit != null)
                        {
                            await transaction.RollbackAsync(CancellationToken.None);
                            return this.ApiBadRequest(
                                $"Không thể cập nhật lịch lớp vì {AdminCoreHelpers.FormatDayFull(busyHit.DayOfWeek)} ({tsInfo.Name}) bị trùng với lớp {busyHit.ClassCode} của giảng viên đã phân công.");
                        }
                    }

                    await ClearClassSchedulesAsync(id, cancellationToken);
                    foreach (var d in autoDays)
                    {
                        _context.ClassSchedules.Add(new ClassSchedule
                        {
                            ClassId = id,
                            ClassroomId = requestedClassroomId.Value,
                            TimeSlotId = tsInfo.Id,
                            DayOfWeek = d,
                            CreatedAt = now
                        });
                    }
                    classSchedules = autoDays.Select(d => new ClassScheduleWindow(
                        requestedClassroomId.Value, "", d, tsInfo.Id, tsInfo.Name, tsInfo.StartTime, tsInfo.EndTime)).ToList();
                }
                else if (dto.ClassroomId.HasValue && oldClassroomId != requestedClassroomId && requestedClassroomId.HasValue && classSchedules.Count > 0)
                {
                    await _context.ClassSchedules
                        .Where(cs => cs.ClassId == id)
                        .ExecuteUpdateAsync(s => s.SetProperty(cs => cs.ClassroomId, requestedClassroomId.Value), cancellationToken);
                }

                if (!isCancelling)
                {
                    var hasTeacher = resultingTeacherIds.Count > 0;
                    var hasSchedule = classSchedules.Count > 0;
                    var hasStudent = activeStudentCount > 0;
                    var hasReachedStart = newStartDate.Date <= todayVn && newEndDate.Date > todayVn;
                    var isReadyForOngoing = hasTeacher && hasSchedule && hasStudent && hasReachedStart;

                    if (string.Equals(requestedStatus, "ongoing", StringComparison.OrdinalIgnoreCase) && !isReadyForOngoing)
                    {
                        var missing = new List<string>();
                        if (!hasTeacher) missing.Add("chưa có giảng viên phụ trách");
                        if (!hasSchedule) missing.Add("chưa có phòng học & lịch học");
                        if (!hasStudent) missing.Add("chưa có học viên đăng ký");
                        if (!hasReachedStart) missing.Add($"chưa tới ngày khai giảng ({newStartDate:dd/MM/yyyy})");
                        await transaction.RollbackAsync(CancellationToken.None);
                        return this.ApiBadRequest(
                            $"Không thể chuyển lớp {courseClass.ClassCode} sang Đang diễn ra vì {string.Join(", ", missing)}.");
                    }

                    courseClass.Status = isReadyForOngoing ? "ongoing" : "upcoming";
                }

                if (mainTeacherChanged)
                {
                    var oldMains = currentClassTeachers
                        .Where(ct => ct.Role == "main" && (!requestedMainTeacherId.HasValue || ct.TeacherId != requestedMainTeacherId.Value))
                        .ToList();
                    if (oldMains.Count > 0)
                    {
                        _context.ClassTeachers.RemoveRange(oldMains);
                    }

                    if (requestedMainTeacherId.HasValue)
                    {
                        var existingAssignment = currentClassTeachers.FirstOrDefault(ct => ct.TeacherId == requestedMainTeacherId.Value);
                        if (existingAssignment != null)
                        {
                            existingAssignment.Role = "main";
                            existingAssignment.AssignedAt = now;
                        }
                        else
                        {
                            _context.ClassTeachers.Add(new ClassTeacher
                            {
                                ClassId = id,
                                TeacherId = requestedMainTeacherId.Value,
                                Role = "main",
                                AssignedAt = now
                            });
                        }
                    }
                }

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return Ok(new
                {
                    success = true,
                    message = $"Cập nhật thông tin lớp {courseClass.ClassCode} thành công.",
                    id = courseClass.Id,
                    status = courseClass.Status
                });
            }
            catch (OperationCanceledException)
            {
                return this.ApiCancelled();
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                await transaction.RollbackAsync(CancellationToken.None);
                return this.ApiBadRequest($"Mã lớp '{classCode}' đã tồn tại trong hệ thống.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể cập nhật lớp học {ClassId}.", id);
                await transaction.RollbackAsync(CancellationToken.None);
                return this.ApiServerError("Không thể cập nhật lớp học.");
            }
        }

        [HttpPost("/Admin/api/Classes/UpdateStatus/{id}")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] ClassStatusUpdateDto? dto, CancellationToken cancellationToken)
        {
            if (id <= 0 || dto == null)
            {
                return this.ApiBadRequest("Dữ liệu cập nhật trạng thái lớp không hợp lệ.");
            }

            var newStatus = (dto.Status ?? string.Empty).Trim().ToLowerInvariant();
            if (!ValidClassStatuses.Contains(newStatus))
            {
                return this.ApiBadRequest("Trạng thái lớp học phải là upcoming, ongoing, completed hoặc cancelled.");
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
                _logger.LogWarning(ex, "Không thể đồng bộ trạng thái hết hạn trước khi cập nhật lớp {ClassId}.", id);
            }

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                // 1. Khóa lớp học (100M + classId)
                await AcquireAdvisoryLockAsync(ClassLockOffset + id, cancellationToken);

                var courseClass = await _context.CourseClasses.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
                if (courseClass == null)
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiNotFound("Không tìm thấy lớp học.");
                }

                if (string.Equals(courseClass.Status, newStatus, StringComparison.OrdinalIgnoreCase))
                {
                    await transaction.CommitAsync(cancellationToken);
                    return Ok(new { success = true, message = "Trạng thái lớp học không thay đổi.", status = courseClass.Status });
                }

                if (string.Equals(courseClass.Status, "ongoing", StringComparison.OrdinalIgnoreCase))
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiBadRequest(
                        $"Lớp {courseClass.ClassCode} đang diễn ra (ongoing) nên không được chỉnh sửa trạng thái hoặc hủy.");
                }

                if (string.Equals(courseClass.Status, "cancelled", StringComparison.OrdinalIgnoreCase))
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiBadRequest(
                        $"Lớp {courseClass.ClassCode} đã bị hủy nên không thể thay đổi trạng thái.");
                }

                if (string.Equals(courseClass.Status, "completed", StringComparison.OrdinalIgnoreCase) && newStatus != "cancelled")
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiBadRequest(
                        $"Lớp {courseClass.ClassCode} đã kết thúc, chỉ được phép đổi trạng thái sang Đã hủy.");
                }

                if (string.Equals(courseClass.Status, "upcoming", StringComparison.OrdinalIgnoreCase) && newStatus == "completed")
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiBadRequest("Không thể chuyển thủ công sang Đã kết thúc; hệ thống sẽ tự động cập nhật khi đến ngày kết thúc.");
                }

                if (newStatus == "ongoing")
                {
                    if (courseClass.StartDate.Date > AdminCoreHelpers.GetTodayVnDate())
                    {
                        await transaction.RollbackAsync(CancellationToken.None);
                        return this.ApiBadRequest(
                            $"Không thể chuyển lớp {courseClass.ClassCode} sang Đang diễn ra vì chưa tới ngày khai giảng ({courseClass.StartDate:dd/MM/yyyy}).");
                    }

                    var hasTeacher = await _context.ClassTeachers.AsNoTracking().AnyAsync(ct => ct.ClassId == id, cancellationToken);
                    var hasSchedule = await _context.ClassSchedules.AsNoTracking().AnyAsync(cs => cs.ClassId == id, cancellationToken);
                    var hasStudent = await _context.Enrollments.AsNoTracking().AnyAsync(e => e.ClassId == id && e.Status == "active", cancellationToken);
                    if (!hasTeacher || !hasSchedule || !hasStudent)
                    {
                        var missing = new List<string>();
                        if (!hasTeacher) missing.Add("chưa có giảng viên phụ trách");
                        if (!hasSchedule) missing.Add("chưa có phòng học & lịch học");
                        if (!hasStudent) missing.Add("chưa có học viên đăng ký");
                        await transaction.RollbackAsync(CancellationToken.None);
                        return this.ApiBadRequest(
                            $"Không thể chuyển lớp {courseClass.ClassCode} sang Đang diễn ra vì {string.Join(", ", missing)}.");
                    }
                }

                // Khi chuyển lớp từ cancelled hoặc completed sang trạng thái hoạt động (upcoming / ongoing)
                if (!IsActiveClassStatus(courseClass.Status) && IsActiveClassStatus(newStatus))
                {
                    if (courseClass.EndDate.Date <= AdminCoreHelpers.GetTodayVnDate())
                    {
                        await transaction.RollbackAsync(CancellationToken.None);
                        return this.ApiBadRequest(
                            $"Không thể chuyển lớp {courseClass.ClassCode} sang trạng thái {(newStatus == "upcoming" ? "Sắp diễn ra" : "Đang diễn ra")} vì ngày kết thúc ({courseClass.EndDate:dd/MM/yyyy}) đã đến hoặc đã qua hôm nay. Hãy sửa ngày kết thúc trước.");
                    }

                    var classSchedules = await LoadClassScheduleWindowsAsync(id, cancellationToken);
                    var assignedTeachers = await (
                        from ct in _context.ClassTeachers.AsNoTracking()
                        join t in _context.Teachers.AsNoTracking() on ct.TeacherId equals t.Id
                        join u in _context.Users.AsNoTracking() on t.UserId equals u.Id
                        where ct.ClassId == id
                        select new { TeacherId = t.Id, u.FullName }
                    ).Distinct().OrderBy(x => x.TeacherId).ToListAsync(cancellationToken);

                    // 2. Khóa các phòng liên quan theo thứ tự tăng dần (200M + classroomId)
                    var roomsToLock = classSchedules.Select(s => s.ClassroomId).ToList();
                    foreach (var roomId in roomsToLock.Distinct().OrderBy(x => x))
                    {
                        await AcquireAdvisoryLockAsync(ClassroomLockOffset + roomId, cancellationToken);
                    }

                    // 3. Khóa toàn bộ giảng viên/trợ giảng theo thứ tự tăng dần (300M + teacherId)
                    foreach (var teacher in assignedTeachers)
                    {
                        await AcquireAdvisoryLockAsync(TeacherLockOffset + teacher.TeacherId, cancellationToken);
                    }

                    // 4. Khóa chia sẻ Ca học (700M)
                    await AcquireSharedAdvisoryLockAsync(TimeSlotLockKey, cancellationToken);

                    if (classSchedules.Count > 0)
                    {
                        var roomConstraintErr = await CheckRoomsActiveAndCapacityAsync(courseClass.MaxStudents, classSchedules, cancellationToken);
                        if (roomConstraintErr != null)
                        {
                            await transaction.RollbackAsync(CancellationToken.None);
                            return this.ApiBadRequest(roomConstraintErr);
                        }

                        var timeSlotErr = await CheckTimeSlotsActiveAsync(classSchedules, cancellationToken);
                        if (timeSlotErr != null)
                        {
                            await transaction.RollbackAsync(CancellationToken.None);
                            return this.ApiBadRequest(timeSlotErr);
                        }

                        var roomConflict = await CheckRoomScheduleConflictsAsync(
                            id,
                            courseClass.StartDate,
                            courseClass.EndDate,
                            classSchedules,
                            cancellationToken);
                        if (roomConflict != null)
                        {
                            await transaction.RollbackAsync(CancellationToken.None);
                            return this.ApiBadRequest(roomConflict);
                        }

                        foreach (var teacher in assignedTeachers)
                        {
                            var teacherConflict = await CheckTeacherScheduleConflictsAsync(
                                id,
                                teacher.TeacherId,
                                teacher.FullName,
                                courseClass.StartDate,
                                courseClass.EndDate,
                                classSchedules,
                                cancellationToken);
                            if (teacherConflict != null)
                            {
                                await transaction.RollbackAsync(CancellationToken.None);
                                return this.ApiBadRequest(teacherConflict);
                            }
                        }
                    }
                }

                var now = DateTime.UtcNow;
                courseClass.Status = newStatus;
                courseClass.UpdatedAt = now;
                if (newStatus == "cancelled")
                {
                    await CancelClassEffectsAsync(id, now, cancellationToken);
                }
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                var statusMsg = newStatus == "cancelled"
                    ? $"Đã hủy lớp {courseClass.ClassCode}, gỡ khỏi Thời khóa biểu và trả lại phòng học trống."
                    : $"Đã cập nhật trạng thái lớp {courseClass.ClassCode} thành công.";

                return Ok(new
                {
                    success = true,
                    message = statusMsg,
                    status = courseClass.Status
                });
            }
            catch (OperationCanceledException)
            {
                return this.ApiCancelled();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể cập nhật trạng thái lớp học {ClassId}.", id);
                await transaction.RollbackAsync(CancellationToken.None);
                return this.ApiServerError("Không thể cập nhật trạng thái lớp học.");
            }
        }

        [HttpPost("/Admin/api/Classes/{id}/Teachers/Assign")]
        public async Task<IActionResult> AssignTeacher(int id, [FromBody] ClassTeacherAssignDto? dto, CancellationToken cancellationToken)
        {
            if (id <= 0 || dto == null || dto.TeacherId <= 0)
            {
                return this.ApiBadRequest("Vui lòng chọn lớp học và giảng viên hợp lệ.");
            }

            var role = string.IsNullOrWhiteSpace(dto.Role) ? "assistant" : dto.Role.Trim().ToLowerInvariant();
            if (!ValidTeacherRoles.Contains(role))
            {
                return this.ApiBadRequest("Vai trò giảng viên phải là main hoặc assistant.");
            }

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                // Khóa tăng dần: Lớp (100M + id) -> Giảng viên (300M + teacherId) -> Ca học shared (700M)
                await AcquireAdvisoryLockAsync(ClassLockOffset + id, cancellationToken);
                await AcquireAdvisoryLockAsync(TeacherLockOffset + dto.TeacherId, cancellationToken);
                await AcquireSharedAdvisoryLockAsync(TimeSlotLockKey, cancellationToken);

                var courseClass = await _context.CourseClasses
                    .AsNoTracking()
                    .Where(c => c.Id == id)
                    .Select(c => new { c.Id, c.ClassCode, c.Status, c.StartDate, c.EndDate })
                    .FirstOrDefaultAsync(cancellationToken);
                if (courseClass == null)
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiNotFound("Không tìm thấy lớp học.");
                }

                if (!string.Equals(courseClass.Status, "upcoming", StringComparison.OrdinalIgnoreCase))
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiBadRequest($"Lớp {courseClass.ClassCode} không ở trạng thái Sắp khai giảng nên không thể chỉnh sửa giảng viên.");
                }

                var teacher = await (
                    from t in _context.Teachers.AsNoTracking()
                    join u in _context.Users.AsNoTracking() on t.UserId equals u.Id
                    where t.Id == dto.TeacherId
                    select new { t.Id, t.IsVerified, u.Role, u.IsActive, u.FullName }
                ).FirstOrDefaultAsync(cancellationToken);
                if (teacher == null || !teacher.IsVerified || teacher.Role != "teacher" || !teacher.IsActive)
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiBadRequest("Giảng viên không tồn tại, chưa được xác minh hoặc tài khoản đã bị khóa/đổi vai trò.");
                }

                var existingAssignments = await _context.ClassTeachers
                    .Where(ct => ct.ClassId == id)
                    .ToListAsync(cancellationToken);

                var now = DateTime.UtcNow;

                if (role == "main")
                {
                    if (existingAssignments.Any(ct => ct.TeacherId == dto.TeacherId && ct.Role == "main"))
                    {
                        await transaction.RollbackAsync(CancellationToken.None);
                        return this.ApiBadRequest($"Giảng viên {teacher.FullName} hiện đã là giảng viên chính của lớp này.");
                    }

                    if (IsActiveClassStatus(courseClass.Status))
                    {
                        var classSchedules = await LoadClassScheduleWindowsAsync(id, cancellationToken);
                        if (classSchedules.Count > 0)
                        {
                            var teacherConflict = await CheckTeacherScheduleConflictsAsync(
                                id,
                                dto.TeacherId,
                                teacher.FullName,
                                courseClass.StartDate,
                                courseClass.EndDate,
                                classSchedules,
                                cancellationToken);
                            if (teacherConflict != null)
                            {
                                await transaction.RollbackAsync(CancellationToken.None);
                                return this.ApiBadRequest(teacherConflict);
                            }
                        }
                    }

                    var oldMains = existingAssignments
                        .Where(ct => ct.Role == "main" && ct.TeacherId != dto.TeacherId)
                        .ToList();
                    if (oldMains.Count > 0)
                    {
                        _context.ClassTeachers.RemoveRange(oldMains);
                    }

                    var existingSelf = existingAssignments.FirstOrDefault(ct => ct.TeacherId == dto.TeacherId);
                    if (existingSelf != null)
                    {
                        existingSelf.Role = "main";
                        existingSelf.AssignedAt = now;
                    }
                    else
                    {
                        _context.ClassTeachers.Add(new ClassTeacher
                        {
                            ClassId = id,
                            TeacherId = dto.TeacherId,
                            Role = "main",
                            AssignedAt = now
                        });
                    }
                }
                else
                {
                    var existingPair = existingAssignments.FirstOrDefault(ct => ct.TeacherId == dto.TeacherId);
                    if (existingPair != null)
                    {
                        await transaction.RollbackAsync(CancellationToken.None);
                        var roleMsg = existingPair.Role == "main"
                            ? $"Giảng viên {teacher.FullName} đang là giảng viên chính của lớp này."
                            : $"Giảng viên {teacher.FullName} đã được phân công làm trợ giảng của lớp này.";
                        return this.ApiBadRequest(roleMsg);
                    }

                    // Kiểm tra xung đột lịch dạy cho trợ giảng khi lớp đang hoạt động (upcoming / ongoing)
                    if (IsActiveClassStatus(courseClass.Status))
                    {
                        var classSchedules = await LoadClassScheduleWindowsAsync(id, cancellationToken);
                        if (classSchedules.Count > 0)
                        {
                            var teacherConflict = await CheckTeacherScheduleConflictsAsync(
                                id,
                                dto.TeacherId,
                                teacher.FullName,
                                courseClass.StartDate,
                                courseClass.EndDate,
                                classSchedules,
                                cancellationToken);
                            if (teacherConflict != null)
                            {
                                await transaction.RollbackAsync(CancellationToken.None);
                                return this.ApiBadRequest(teacherConflict);
                            }
                        }
                    }

                    _context.ClassTeachers.Add(new ClassTeacher
                    {
                        ClassId = id,
                        TeacherId = dto.TeacherId,
                        Role = "assistant",
                        AssignedAt = now
                    });
                }

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                var successMsg = role == "main"
                    ? $"Đã phân công {teacher.FullName} làm giảng viên chính của lớp {courseClass.ClassCode}."
                    : $"Đã thêm trợ giảng {teacher.FullName} vào lớp {courseClass.ClassCode}.";
                return Ok(new { success = true, message = successMsg });
            }
            catch (OperationCanceledException)
            {
                return this.ApiCancelled();
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                await transaction.RollbackAsync(CancellationToken.None);
                return this.ApiBadRequest("Giảng viên này đã được phân công trong lớp học.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể phân công giảng viên {TeacherId} cho lớp {ClassId}.", dto.TeacherId, id);
                await transaction.RollbackAsync(CancellationToken.None);
                return this.ApiServerError("Không thể phân công giảng viên cho lớp học.");
            }
        }

        [HttpPost("/Admin/api/Classes/{id}/Teachers/Remove/{teacherId}")]
        [HttpDelete("/Admin/api/Classes/{id}/Teachers/Remove/{teacherId}")]
        public async Task<IActionResult> RemoveTeacher(int id, int teacherId, CancellationToken cancellationToken)
        {
            if (id <= 0 || teacherId <= 0)
            {
                return this.ApiBadRequest("Mã lớp học hoặc giảng viên không hợp lệ.");
            }

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                // Khóa tăng dần: Lớp (100M + id) -> Giảng viên (300M + teacherId)
                await AcquireAdvisoryLockAsync(ClassLockOffset + id, cancellationToken);
                await AcquireAdvisoryLockAsync(TeacherLockOffset + teacherId, cancellationToken);

                var targetClass = await _context.CourseClasses
                    .AsNoTracking()
                    .Where(c => c.Id == id)
                    .Select(c => new { c.Id, c.ClassCode, c.Status })
                    .FirstOrDefaultAsync(cancellationToken);
                if (targetClass == null)
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiNotFound("Không tìm thấy lớp học.");
                }

                if (!string.Equals(targetClass.Status, "upcoming", StringComparison.OrdinalIgnoreCase))
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiBadRequest($"Lớp {targetClass.ClassCode} không ở trạng thái Sắp khai giảng nên không thể gỡ giảng viên.");
                }

                var assignments = await _context.ClassTeachers
                    .Where(ct => ct.ClassId == id && ct.TeacherId == teacherId)
                    .ToListAsync(cancellationToken);
                if (assignments.Count == 0)
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiNotFound("Không tìm thấy phân công giảng viên này trong lớp.");
                }

                _context.ClassTeachers.RemoveRange(assignments);
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return Ok(new { success = true, message = "Đã gỡ giảng viên khỏi lớp học." });
            }
            catch (OperationCanceledException)
            {
                return this.ApiCancelled();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể gỡ giảng viên {TeacherId} khỏi lớp {ClassId}.", teacherId, id);
                await transaction.RollbackAsync(CancellationToken.None);
                return this.ApiServerError("Không thể gỡ giảng viên khỏi lớp học.");
            }
        }

        [HttpGet("/Admin/api/Classes/Detail/{id}")]
        public async Task<IActionResult> DetailApi(int id, CancellationToken cancellationToken, bool lookups = true)
        {
            if (id <= 0)
            {
                return this.ApiBadRequest("Mã lớp học không hợp lệ.");
            }

            try
            {
                await AdminCoreHelpers.SyncExpiredClassesAsync(_context, cancellationToken);

                var cls = await _context.CourseClasses
                    .AsNoTracking()
                    .Where(c => c.Id == id)
                    .Select(c => new
                    {
                        c.Id,
                        c.CourseId,
                        CourseTitle = c.Course != null ? c.Course.Title : null,
                        c.ClassCode,
                        c.ClassName,
                        c.StartDate,
                        c.EndDate,
                        c.MaxStudents,
                        c.Price,
                        c.Status
                    })
                    .FirstOrDefaultAsync(cancellationToken);

                if (cls == null)
                {
                    return this.ApiNotFound("Không tìm thấy lớp học.");
                }

                var courseId = cls.CourseId;

                var teachersAssigned = await (
                    from ct in _context.ClassTeachers.AsNoTracking()
                    join t in _context.Teachers.AsNoTracking() on ct.TeacherId equals t.Id
                    join u in _context.Users.AsNoTracking() on t.UserId equals u.Id
                    where ct.ClassId == id
                    orderby ct.Role == "main" ? 0 : 1, ct.AssignedAt ascending, ct.Id ascending
                    select new
                    {
                        teacherId = t.Id,
                        fullName = u.FullName,
                        email = u.Email,
                        qualification = t.Qualification,
                        isVerified = t.IsVerified,
                        role = ct.Role,
                        assignedAt = ct.AssignedAt
                    }
                ).ToListAsync(cancellationToken);

                var mainTeacher = teachersAssigned.FirstOrDefault(t => string.Equals(t.role, "main", StringComparison.OrdinalIgnoreCase));

                var schedules = await (
                    from cs in _context.ClassSchedules.AsNoTracking()
                    join r in _context.Classrooms.AsNoTracking() on cs.ClassroomId equals r.Id
                    join ts in _context.TimeSlots.AsNoTracking() on cs.TimeSlotId equals ts.Id
                    where cs.ClassId == id
                    orderby cs.DayOfWeek, ts.StartTime
                    select new
                    {
                        id = cs.Id,
                        dayOfWeek = cs.DayOfWeek,
                        classroomName = r.Name,
                        classroomLocation = r.Location,
                        timeSlotName = ts.Name,
                        startTime = ts.StartTime,
                        endTime = ts.EndTime
                    }
                ).ToListAsync(cancellationToken);

                var scheduleFormatted = schedules
                    .OrderBy(s => s.dayOfWeek)
                    .ThenBy(s => s.startTime)
                    .Select(s => new
                {
                    s.id,
                    s.dayOfWeek,
                    dayLabel = AdminCoreHelpers.FormatDayFull(s.dayOfWeek),
                    dayShort = AdminCoreHelpers.FormatDayShort(s.dayOfWeek),
                    s.classroomName,
                    s.classroomLocation,
                    s.timeSlotName,
                    timeRange = AdminCoreHelpers.FormatTimeRange(s.startTime, s.endTime)
                }).ToList();

                var rawClassLessons = await (
                    from cl in _context.ClassLessons.AsNoTracking()
                    join l in _context.Lessons.AsNoTracking() on cl.LessonId equals l.Id
                    join t in _context.Teachers.AsNoTracking() on cl.TeacherId equals t.Id into tGroup
                    from teacher in tGroup.DefaultIfEmpty()
                    join u in _context.Users.AsNoTracking() on teacher.UserId equals u.Id into uGroup
                    from teacherUser in uGroup.DefaultIfEmpty()
                    join cs in _context.ClassSchedules.AsNoTracking() on cl.ScheduleId equals cs.Id into csGroup
                    from sched in csGroup.DefaultIfEmpty()
                    join r in _context.Classrooms.AsNoTracking() on sched.ClassroomId equals r.Id into rGroup
                    from room in rGroup.DefaultIfEmpty()
                    join ts in _context.TimeSlots.AsNoTracking() on sched.TimeSlotId equals ts.Id into tsGroup
                    from slot in tsGroup.DefaultIfEmpty()
                    where cl.ClassId == id
                    orderby cl.ScheduleDate == null, cl.ScheduleDate, l.OrderIndex, cl.Id
                    select new
                    {
                        id = cl.Id,
                        lessonId = cl.LessonId,
                        lessonOrder = l.OrderIndex,
                        lessonTitle = l.Title,
                        contentType = l.ContentType,
                        scheduleId = cl.ScheduleId,
                        teacherId = cl.TeacherId,
                        teacherName = teacherUser != null ? teacherUser.FullName : null,
                        classroomName = room != null ? room.Name : null,
                        dayOfWeek = sched != null ? (int?)sched.DayOfWeek : null,
                        startTime = slot != null ? (TimeSpan?)slot.StartTime : null,
                        endTime = slot != null ? (TimeSpan?)slot.EndTime : null,
                        scheduleDate = cl.ScheduleDate,
                        status = cl.Status,
                        notes = cl.Notes,
                        createdAt = cl.CreatedAt
                    }
                ).ToListAsync(cancellationToken);

                var classLessons = rawClassLessons.Select(cl => new
                {
                    cl.id,
                    cl.lessonId,
                    cl.lessonOrder,
                    cl.lessonTitle,
                    cl.contentType,
                    cl.scheduleId,
                    cl.teacherId,
                    cl.teacherName,
                    cl.classroomName,
                    scheduleLabel = cl.dayOfWeek.HasValue && cl.startTime.HasValue && cl.endTime.HasValue
                        ? $"{AdminCoreHelpers.FormatDayShort(cl.dayOfWeek.Value)} ({AdminCoreHelpers.FormatTimeRange(cl.startTime.Value, cl.endTime.Value)})"
                        : null,
                    cl.scheduleDate,
                    cl.status,
                    cl.notes,
                    cl.createdAt
                }).ToList();

                var (totalLessons, studentRows) = await LoadClassStudentsWithStatsAsync(id, courseId, cancellationToken);

                var students = studentRows.Select(s => new
                {
                    enrollmentId = s.EnrollmentId,
                    userId = s.UserId,
                    fullName = s.FullName,
                    email = s.Email,
                    enrolledAt = s.EnrolledAt,
                    status = s.Status,
                    completedLessons = s.CompletedLessons,
                    totalLessons = s.TotalLessons,
                    attendancePercent = s.AttendancePercent,
                    midtermScore = s.MidtermScore
                }).ToList();

                // Loại học viên đang học (active) hoặc đã hoàn thành (completed); giữ lại học viên chưa từng học hoặc từng hủy (cancelled) để tái ghi danh
                var blockedStudentIds = studentRows
                    .Where(s => string.Equals(s.Status, "active", StringComparison.OrdinalIgnoreCase)
                             || string.Equals(s.Status, "completed", StringComparison.OrdinalIgnoreCase))
                    .Select(s => s.UserId)
                    .Distinct()
                    .ToList();

                var activeStudentCount = studentRows.Count(s => s.Status == "active");
                var attendancePool = studentRows.Where(s => s.Status != "cancelled").ToList();
                var averageAttendance = attendancePool.Count > 0
                    ? Math.Round(attendancePool.Average(s => s.AttendancePercent), 1)
                    : 0.0;

                var scoredStudents = attendancePool.Where(s => s.MidtermScore.HasValue).ToList();
                decimal? averageMidtermScore = scoredStudents.Count > 0
                    ? Math.Round(scoredStudents.Average(s => s.MidtermScore!.Value), 2)
                    : null;

                var availableStudents = lookups
                    ? await _context.Users
                        .AsNoTracking()
                        .Where(u => u.Role == "student" && u.IsActive && !blockedStudentIds.Contains(u.Id))
                        .OrderBy(u => u.FullName)
                        .Select(u => new
                        {
                            id = u.Id,
                            fullName = u.FullName,
                            email = u.Email
                        })
                        .ToListAsync(cancellationToken)
                    : null;

                var allCoursesWithSubject = await (
                    from c in _context.Courses.AsNoTracking()
                    join s in _context.Subjects.AsNoTracking() on c.SubjectId equals s.Id into sJoin
                    from subj in sJoin.DefaultIfEmpty()
                    orderby c.Title
                    select new
                    {
                        c.Id,
                        c.SubjectId,
                        SubjectName = subj != null ? subj.Name : null,
                        c.Title,
                        c.Price,
                        c.Level
                    }
                ).ToListAsync(cancellationToken);

                var coursesBySubjectId = allCoursesWithSubject
                    .GroupBy(c => c.SubjectId)
                    .ToDictionary(g => g.Key, g => g.OrderBy(x => x.Id).ToList());

                var courseDisplayById = allCoursesWithSubject.ToDictionary(
                    c => c.Id,
                    c =>
                    {
                        var siblings = coursesBySubjectId.TryGetValue(c.SubjectId, out var sList) ? sList : [c];
                        var subjectTotal = siblings.Sum(x => x.Price);
                        var breakdown = string.Join(" + ", siblings.Select(x => x.Title));
                        var displayTitle = !string.IsNullOrWhiteSpace(c.SubjectName) && siblings.Count > 1
                            ? $"{c.SubjectName}: {c.Title} (Tổng khóa {c.SubjectName}: {breakdown})"
                            : (!string.IsNullOrWhiteSpace(c.SubjectName) ? $"{c.SubjectName} — {c.Title}" : c.Title);
                        return new
                        {
                            c.Id,
                            c.SubjectId,
                            SubjectName = c.SubjectName ?? c.Title,
                            c.Title,
                            DisplayTitle = displayTitle,
                            CoursePrice = c.Price,
                            SubjectTotalPrice = subjectTotal,
                            SiblingCount = siblings.Count,
                            Breakdown = breakdown,
                            c.Level
                        };
                    });

                var courses = lookups
                    ? allCoursesWithSubject.Select(c =>
                    {
                        var info = courseDisplayById[c.Id];
                        return new
                        {
                            id = c.Id,
                            subjectId = c.SubjectId,
                            subjectName = info.SubjectName,
                            title = info.DisplayTitle,
                            rawTitle = c.Title,
                            coursePrice = c.Price,
                            subjectTotalPrice = info.SubjectTotalPrice,
                            price = info.SubjectTotalPrice > 0 ? info.SubjectTotalPrice : c.Price,
                            level = c.Level
                        };
                    }).ToList()
                    : null;

                var teachers = lookups
                    ? await (
                        from t in _context.Teachers.AsNoTracking()
                        join u in _context.Users.AsNoTracking() on t.UserId equals u.Id
                        where t.IsVerified && u.Role == "teacher" && u.IsActive
                        orderby u.FullName
                        select new
                        {
                            id = t.Id,
                            userId = u.Id,
                            fullName = u.FullName,
                            email = u.Email,
                            qualification = t.Qualification
                        }
                    ).ToListAsync(cancellationToken)
                    : null;

                return Ok(new
                {
                    success = true,
                    classInfo = new
                    {
                        id = cls.Id,
                        courseId = cls.CourseId,
                        courseTitle = cls.CourseTitle ?? "--",
                        classCode = cls.ClassCode,
                        className = cls.ClassName,
                        startDate = cls.StartDate,
                        endDate = cls.EndDate,
                        maxStudents = cls.MaxStudents,
                        activeStudents = activeStudentCount,
                        price = cls.Price,
                        status = cls.Status,
                        mainTeacherId = mainTeacher?.teacherId,
                        teacherName = mainTeacher?.fullName ?? "Chưa phân công",
                        teacherEmail = mainTeacher?.email,
                        teacherQualification = mainTeacher?.qualification,
                        totalLessons,
                        averageAttendance,
                        averageMidtermScore
                    },
                    teachersAssigned,
                    schedules = scheduleFormatted,
                    classLessons,
                    students,
                    availableStudents,
                    courses,
                    teachers
                });
            }
            catch (OperationCanceledException)
            {
                return this.ApiCancelled();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể tải chi tiết lớp học {ClassId}.", id);
                return this.ApiServerError("Không thể tải chi tiết lớp học.");
            }
        }

        [HttpGet("/Admin/api/Classes/{id}/ExportCsv")]
        public async Task<IActionResult> ExportCsv(int id, CancellationToken cancellationToken)
        {
            if (id <= 0)
            {
                return this.ApiBadRequest("Mã lớp học không hợp lệ.");
            }

            try
            {
                await AdminCoreHelpers.SyncExpiredClassesAsync(_context, cancellationToken);

                var cls = await _context.CourseClasses
                    .AsNoTracking()
                    .Where(c => c.Id == id)
                    .Select(c => new
                    {
                        c.Id,
                        c.CourseId,
                        c.ClassCode,
                        c.ClassName,
                        c.Price
                    })
                    .FirstOrDefaultAsync(cancellationToken);

                if (cls == null)
                {
                    return this.ApiNotFound("Không tìm thấy lớp học.");
                }

                var (_, students) = await LoadClassStudentsWithStatsAsync(cls.Id, cls.CourseId, cancellationToken);

                var sb = new StringBuilder();
                sb.AppendLine("STT,Ho ten,Email,Ngay ghi danh,Hoc phi ap dung (VND),Trang thai,So bai hoan thanh,Chuyen can (%),Diem cao nhat");

                for (var i = 0; i < students.Count; i++)
                {
                    var st = students[i];
                    var appliedFee = string.Equals(st.Status, "cancelled", StringComparison.OrdinalIgnoreCase) ? 0m : cls.Price;
                    sb.AppendLine(string.Join(",",
                        (i + 1).ToString(CultureInfo.InvariantCulture),
                        AdminCoreHelpers.EscapeCsv(st.FullName),
                        AdminCoreHelpers.EscapeCsv(st.Email),
                        AdminCoreHelpers.EscapeCsv(st.EnrolledAt.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)),
                        appliedFee.ToString("0", CultureInfo.InvariantCulture),
                        AdminCoreHelpers.EscapeCsv(AdminCoreHelpers.FormatEnrollmentStatusVi(st.Status)),
                        AdminCoreHelpers.EscapeCsv($"{st.CompletedLessons}/{st.TotalLessons} bài"),
                        st.AttendancePercent.ToString("0.0", CultureInfo.InvariantCulture),
                        st.MidtermScore.HasValue ? st.MidtermScore.Value.ToString("0.##", CultureInfo.InvariantCulture) : "--"));
                }

                var preamble = Encoding.UTF8.GetPreamble();
                var body = Encoding.UTF8.GetBytes(sb.ToString());
                var bytes = new byte[preamble.Length + body.Length];
                Buffer.BlockCopy(preamble, 0, bytes, 0, preamble.Length);
                Buffer.BlockCopy(body, 0, bytes, preamble.Length, body.Length);

                var safeCode = SanitizeFileNamePart(cls.ClassCode);
                return File(bytes, "text/csv; charset=utf-8", $"bang-diem-chuyen-can-{safeCode}.csv");
            }
            catch (OperationCanceledException)
            {
                return this.ApiCancelled();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể xuất CSV bảng điểm cho lớp {ClassId}.", id);
                return this.ApiServerError("Không thể xuất tệp CSV bảng điểm và chuyên cần.");
            }
        }

        [HttpPost("/Admin/api/Classes/UpdateEnrollmentStatus")]
        public async Task<IActionResult> UpdateEnrollmentStatus([FromBody] UpdateEnrollmentStatusDto? dto, CancellationToken cancellationToken)
        {
            if (dto == null || dto.EnrollmentId <= 0)
            {
                return this.ApiBadRequest("Dữ liệu cập nhật không hợp lệ.");
            }

            var newStatus = (dto.Status ?? string.Empty).Trim().ToLowerInvariant();
            if (!ValidEnrollmentStatuses.Contains(newStatus))
            {
                return this.ApiBadRequest("Trạng thái ghi danh phải là active, completed hoặc cancelled.");
            }

            int targetClassId;
            int targetUserId;
            try
            {
                var targetInfo = await _context.Enrollments
                    .AsNoTracking()
                    .Where(e => e.Id == dto.EnrollmentId)
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
                _logger.LogError(ex, "Không thể tra cứu ghi danh {EnrollmentId}.", dto.EnrollmentId);
                return this.ApiServerError("Không thể cập nhật trạng thái ghi danh.");
            }

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                // Khóa giao dịch theo thứ tự tăng dần: Lớp (100M) -> Ca học shared (700M) -> Học viên (800M)
                await AcquireAdvisoryLockAsync(ClassLockOffset + targetClassId, cancellationToken);
                if (newStatus == "active")
                {
                    await AcquireSharedAdvisoryLockAsync(TimeSlotLockKey, cancellationToken);
                    await AcquireAdvisoryLockAsync(StudentLockOffset + targetUserId, cancellationToken);
                }

                var enrollment = await _context.Enrollments.FirstOrDefaultAsync(e => e.Id == dto.EnrollmentId, cancellationToken);
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
                    var studentValid = await _context.Users
                        .AsNoTracking()
                        .AnyAsync(u => u.Id == enrollment.UserId && u.Role == "student" && u.IsActive, cancellationToken);
                    if (!studentValid)
                    {
                        await transaction.RollbackAsync(CancellationToken.None);
                        return this.ApiBadRequest("Không thể kích hoạt lại ghi danh vì tài khoản học viên đã bị khóa hoặc đã đổi vai trò.");
                    }

                    if (!IsActiveClassStatus(courseClass.Status))
                    {
                        await transaction.RollbackAsync(CancellationToken.None);
                        var statusLabel = courseClass.Status == "completed" ? "đã kết thúc" : "đã bị hủy";
                        return this.ApiBadRequest($"Không thể kích hoạt lại ghi danh vì lớp {courseClass.ClassCode} {statusLabel}.");
                    }

                    var hasOtherActive = await _context.Enrollments
                        .AnyAsync(e => e.Id != enrollment.Id && e.UserId == enrollment.UserId && e.ClassId == enrollment.ClassId && e.Status == "active", cancellationToken);
                    if (hasOtherActive)
                    {
                        await transaction.RollbackAsync(CancellationToken.None);
                        return this.ApiBadRequest("Học viên này đã có một ghi danh đang hoạt động (active) khác trong lớp.");
                    }

                    var activeCount = await _context.Enrollments
                        .CountAsync(e => e.ClassId == enrollment.ClassId && e.Status == "active", cancellationToken);
                    if (activeCount >= courseClass.MaxStudents)
                    {
                        await transaction.RollbackAsync(CancellationToken.None);
                        return this.ApiBadRequest($"Lớp {courseClass.ClassCode} đã đạt sĩ số tối đa ({activeCount}/{courseClass.MaxStudents}).");
                    }

                    var studentScheduleConflict = await AdminCoreHelpers.CheckStudentScheduleConflictAsync(
                        _context,
                        enrollment.UserId,
                        enrollment.ClassId,
                        courseClass.ClassCode,
                        courseClass.StartDate,
                        courseClass.EndDate,
                        cancellationToken);
                    if (studentScheduleConflict != null)
                    {
                        await transaction.RollbackAsync(CancellationToken.None);
                        return this.ApiBadRequest(studentScheduleConflict);
                    }
                }

                enrollment.Status = newStatus;
                if (newStatus == "cancelled")
                {
                    var pendingPayments = await _context.Payments
                        .Where(p => p.EnrollmentId == enrollment.Id && p.Status == "pending")
                        .ToListAsync(cancellationToken);
                    var payNow = DateTime.UtcNow;
                    foreach (var pendingPay in pendingPayments)
                    {
                        pendingPay.Status = "failed";
                        pendingPay.UpdatedAt = payNow;
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
                _logger.LogError(ex, "Không thể cập nhật trạng thái ghi danh {EnrollmentId}.", dto.EnrollmentId);
                await transaction.RollbackAsync(CancellationToken.None);
                return this.ApiServerError("Không thể cập nhật trạng thái ghi danh.");
            }
        }

        private async Task CancelClassEffectsAsync(int classId, DateTime now, CancellationToken cancellationToken)
        {
            await _context.Enrollments
                .Where(e => e.ClassId == classId && e.Status == "active")
                .ExecuteUpdateAsync(s => s.SetProperty(e => e.Status, "cancelled"), cancellationToken);

            await _context.Payments
                .Where(p => p.ClassId == classId && p.Status == "pending")
                .ExecuteUpdateAsync(s => s
                    .SetProperty(p => p.Status, "failed")
                    .SetProperty(p => p.UpdatedAt, now), cancellationToken);

            await ClearClassSchedulesAsync(classId, cancellationToken);
        }

        private async Task ClearClassSchedulesAsync(int classId, CancellationToken cancellationToken)
        {
            var scheduleIds = await _context.ClassSchedules
                .Where(cs => cs.ClassId == classId)
                .Select(cs => cs.Id)
                .ToListAsync(cancellationToken);

            if (scheduleIds.Count > 0)
            {
                await _context.ClassLessons
                    .Where(cl => cl.ScheduleId.HasValue && scheduleIds.Contains(cl.ScheduleId.Value))
                    .ExecuteUpdateAsync(s => s.SetProperty(cl => cl.ScheduleId, (int?)null), cancellationToken);

                await _context.ClassSchedules
                    .Where(cs => cs.ClassId == classId)
                    .ExecuteDeleteAsync(cancellationToken);
            }
        }

        private static bool IsActiveClassStatus(string? status) =>
            string.Equals(status, "upcoming", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "ongoing", StringComparison.OrdinalIgnoreCase);

        private Task AcquireAdvisoryLockAsync(long lockKey, CancellationToken cancellationToken)
        {
            return _context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({lockKey})", cancellationToken);
        }

        private Task AcquireSharedAdvisoryLockAsync(long lockKey, CancellationToken cancellationToken)
        {
            return _context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock_shared({lockKey})", cancellationToken);
        }

        private async Task<List<ClassScheduleWindow>> LoadClassScheduleWindowsAsync(int classId, CancellationToken cancellationToken)
        {
            return await (
                from cs in _context.ClassSchedules.AsNoTracking()
                join r in _context.Classrooms.AsNoTracking() on cs.ClassroomId equals r.Id
                join ts in _context.TimeSlots.AsNoTracking() on cs.TimeSlotId equals ts.Id
                where cs.ClassId == classId
                orderby cs.DayOfWeek, ts.StartTime
                select new ClassScheduleWindow(
                    cs.ClassroomId,
                    r.Name,
                    cs.DayOfWeek,
                    cs.TimeSlotId,
                    ts.Name,
                    ts.StartTime,
                    ts.EndTime)
            ).ToListAsync(cancellationToken);
        }

        private async Task<string?> CheckTimeSlotsActiveAsync(
            List<ClassScheduleWindow> classSchedules,
            CancellationToken cancellationToken)
        {
            if (classSchedules.Count == 0)
            {
                return null;
            }

            var slotIds = classSchedules.Select(s => s.TimeSlotId).Distinct().ToList();
            var inactiveSlot = await _context.TimeSlots
                .AsNoTracking()
                .Where(ts => slotIds.Contains(ts.Id) && !ts.IsActive)
                .Select(ts => new { ts.Name })
                .FirstOrDefaultAsync(cancellationToken);

            return inactiveSlot != null
                ? $"Không thể kích hoạt/cập nhật lớp vì ca học {inactiveSlot.Name} (đang được xếp lịch cho lớp) đang tạm ngưng sử dụng."
                : null;
        }

        private async Task<string?> CheckRoomsActiveAndCapacityAsync(
            int targetMaxStudents,
            List<ClassScheduleWindow> classSchedules,
            CancellationToken cancellationToken)
        {
            if (classSchedules.Count == 0)
            {
                return null;
            }

            var roomIds = classSchedules.Select(s => s.ClassroomId).Distinct().ToList();
            var rooms = await _context.Classrooms
                .AsNoTracking()
                .Where(r => roomIds.Contains(r.Id))
                .Select(r => new { r.Id, r.Name, r.Capacity, r.IsActive })
                .ToListAsync(cancellationToken);

            foreach (var room in rooms)
            {
                if (!room.IsActive)
                {
                    return $"Không thể kích hoạt/cập nhật lớp vì phòng học {room.Name} (đang được xếp lịch cho lớp) đang tạm ngưng sử dụng.";
                }

                if (room.Capacity < targetMaxStudents)
                {
                    return $"Sĩ số tối đa ({targetMaxStudents} học viên) vượt quá sức chứa của phòng {room.Name} ({room.Capacity} chỗ) đã xếp lịch cho lớp này.";
                }
            }

            return null;
        }

        private async Task<string?> CheckRoomScheduleConflictsAsync(
            int classId,
            DateTime startDate,
            DateTime endDate,
            List<ClassScheduleWindow> classSchedules,
            CancellationToken cancellationToken)
        {
            if (classSchedules.Count == 0)
            {
                return null;
            }

            var roomIds = classSchedules.Select(s => s.ClassroomId).Distinct().ToList();
            var days = classSchedules.Select(s => s.DayOfWeek).Distinct().ToList();

            var otherSchedules = await (
                from cs in _context.ClassSchedules.AsNoTracking()
                join cc in _context.CourseClasses.AsNoTracking() on cs.ClassId equals cc.Id
                join ts in _context.TimeSlots.AsNoTracking() on cs.TimeSlotId equals ts.Id
                where cs.ClassId != classId
                      && (cc.Status == "upcoming" || cc.Status == "ongoing")
                      && startDate <= cc.EndDate
                      && cc.StartDate <= endDate
                      && roomIds.Contains(cs.ClassroomId)
                      && days.Contains(cs.DayOfWeek)
                select new
                {
                    cs.ClassroomId,
                    cs.DayOfWeek,
                    cc.ClassCode,
                    cc.ClassName,
                    ts.Name,
                    ts.StartTime,
                    ts.EndTime
                }
            ).ToListAsync(cancellationToken);

            foreach (var mine in classSchedules)
            {
                var conflict = otherSchedules.FirstOrDefault(other =>
                    other.ClassroomId == mine.ClassroomId
                    && other.DayOfWeek == mine.DayOfWeek
                    && other.StartTime < mine.EndTime
                    && mine.StartTime < other.EndTime);

                if (conflict != null)
                {
                    var myRange = $"{mine.TimeSlotName} ({AdminCoreHelpers.FormatTimeRange(mine.StartTime, mine.EndTime)})";
                    var conflictRange = $"{conflict.Name} ({AdminCoreHelpers.FormatTimeRange(conflict.StartTime, conflict.EndTime)})";
                    return $"Trùng phòng học: Phòng {mine.ClassroomName} vào {AdminCoreHelpers.FormatDayFull(mine.DayOfWeek)}, ca {myRange} bị giao với ca {conflictRange} của lớp {conflict.ClassCode} ({conflict.ClassName}).";
                }
            }

            return null;
        }

        private async Task<string?> CheckTeacherScheduleConflictsAsync(
            int classId,
            int teacherId,
            string teacherName,
            DateTime startDate,
            DateTime endDate,
            List<ClassScheduleWindow> classSchedules,
            CancellationToken cancellationToken)
        {
            if (classSchedules.Count == 0)
            {
                return null;
            }

            var days = classSchedules.Select(s => s.DayOfWeek).Distinct().ToList();

            var otherSchedules = await (
                from cs in _context.ClassSchedules.AsNoTracking()
                join cc in _context.CourseClasses.AsNoTracking() on cs.ClassId equals cc.Id
                join ct in _context.ClassTeachers.AsNoTracking() on cc.Id equals ct.ClassId
                join ts in _context.TimeSlots.AsNoTracking() on cs.TimeSlotId equals ts.Id
                where cs.ClassId != classId
                      && (cc.Status == "upcoming" || cc.Status == "ongoing")
                      && startDate <= cc.EndDate
                      && cc.StartDate <= endDate
                      && ct.TeacherId == teacherId
                      && days.Contains(cs.DayOfWeek)
                select new
                {
                    cs.DayOfWeek,
                    cc.ClassCode,
                    cc.ClassName,
                    ts.Name,
                    ts.StartTime,
                    ts.EndTime
                }
            ).ToListAsync(cancellationToken);

            foreach (var mine in classSchedules)
            {
                var conflict = otherSchedules.FirstOrDefault(other =>
                    other.DayOfWeek == mine.DayOfWeek
                    && other.StartTime < mine.EndTime
                    && mine.StartTime < other.EndTime);

                if (conflict != null)
                {
                    var myRange = $"{mine.TimeSlotName} ({AdminCoreHelpers.FormatTimeRange(mine.StartTime, mine.EndTime)})";
                    var conflictRange = $"{conflict.Name} ({AdminCoreHelpers.FormatTimeRange(conflict.StartTime, conflict.EndTime)})";
                    return $"Trùng lịch giảng viên: Giảng viên {teacherName} đã được phân công lớp {conflict.ClassCode} ({conflict.ClassName}) vào {AdminCoreHelpers.FormatDayFull(mine.DayOfWeek)}, ca {conflictRange} (giao với {myRange}).";
                }
            }

            return null;
        }

        private async Task<(int TotalLessons, List<ClassStudentStatsRow> Students)> LoadClassStudentsWithStatsAsync(
            int classId,
            int courseId,
            CancellationToken cancellationToken)
        {
            var totalLessons = await _context.Lessons
                .AsNoTracking()
                .CountAsync(l => l.CourseId == courseId, cancellationToken);

            var enrollments = await (
                from e in _context.Enrollments.AsNoTracking()
                join u in _context.Users.AsNoTracking() on e.UserId equals u.Id
                where e.ClassId == classId
                orderby e.EnrolledAt ascending, e.Id ascending
                select new
                {
                    EnrollmentId = e.Id,
                    UserId = u.Id,
                    u.FullName,
                    u.Email,
                    e.EnrolledAt,
                    e.Status
                }
            ).ToListAsync(cancellationToken);

            var studentIds = enrollments.Select(e => e.UserId).Distinct().ToList();

            var completedCountByUser = totalLessons > 0 && studentIds.Count > 0
                ? await (
                    from up in _context.UserProgresses.AsNoTracking()
                    join l in _context.Lessons.AsNoTracking() on up.LessonId equals l.Id
                    where l.CourseId == courseId && up.IsCompleted && studentIds.Contains(up.UserId)
                    group up.LessonId by up.UserId into g
                    select new { UserId = g.Key, Count = g.Distinct().Count() }
                ).ToDictionaryAsync(x => x.UserId, x => x.Count, cancellationToken)
                : new Dictionary<int, int>();

            var maxScoreByUser = studentIds.Count > 0
                ? await (
                    from ue in _context.UserExams.AsNoTracking()
                    join exam in _context.Exams.AsNoTracking() on ue.ExamId equals exam.Id
                    where exam.CourseId == courseId
                          && studentIds.Contains(ue.UserId)
                          && ue.Status != "in_progress"
                          && ue.TotalScore != null
                    group ue.TotalScore by ue.UserId into g
                    select new { UserId = g.Key, MaxScore = g.Max() }
                ).ToDictionaryAsync(x => x.UserId, x => x.MaxScore, cancellationToken)
                : new Dictionary<int, int?>();

            var rows = enrollments.Select(e =>
            {
                var completedLessons = completedCountByUser.GetValueOrDefault(e.UserId);
                var attendancePercent = totalLessons > 0
                    ? Math.Round(completedLessons * 100.0 / totalLessons, 1)
                    : 0.0;
                decimal? midtermScore = maxScoreByUser.TryGetValue(e.UserId, out var score) ? score : null;

                return new ClassStudentStatsRow(
                    e.EnrollmentId,
                    e.UserId,
                    e.FullName,
                    e.Email,
                    e.EnrolledAt,
                    e.Status,
                    completedLessons,
                    totalLessons,
                    attendancePercent,
                    midtermScore);
            }).ToList();

            return (totalLessons, rows);
        }

        private static string SanitizeFileNamePart(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return "lop-hoc";
            }

            var chars = input.Trim()
                .Select(ch => char.IsLetterOrDigit(ch) || ch is '-' or '_' ? ch : '-')
                .ToArray();
            var cleaned = new string(chars).Trim('-');
            return string.IsNullOrEmpty(cleaned) ? "lop-hoc" : cleaned;
        }

        private sealed record ClassScheduleWindow(
            int ClassroomId,
            string ClassroomName,
            int DayOfWeek,
            int TimeSlotId,
            string TimeSlotName,
            TimeSpan StartTime,
            TimeSpan EndTime);

        private static List<int> ResolveSchedulePatternDays(string? scheduleDays) => (scheduleDays ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "t246" => [1, 3, 5],
            "t357" => [2, 4, 6],
            "all" => [1, 2, 3, 4, 5, 6],
            _ => []
        };

        private static string? FormatScheduleDaysLabel(string? scheduleDays) => (scheduleDays ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "t246" => "Thứ 2 - Thứ 4 - Thứ 6",
            "t357" => "Thứ 3 - Thứ 5 - Thứ 7",
            "all" => "Cả tuần (Thứ 2 - Thứ 7)",
            _ => null
        };

        private async Task<List<object>> BuildClassroomsLookupAsync(CancellationToken cancellationToken)
        {
            var rooms = await _context.Classrooms
                .AsNoTracking()
                .Where(r => r.IsActive)
                .OrderBy(r => r.Name)
                .Select(r => new
                {
                    r.Id,
                    r.Name,
                    r.Location,
                    r.Capacity
                })
                .ToListAsync(cancellationToken);

            var activeScheduled = await (
                from cs in _context.ClassSchedules.AsNoTracking()
                join cc in _context.CourseClasses.AsNoTracking() on cs.ClassId equals cc.Id
                where cc.Status == "upcoming" || cc.Status == "ongoing"
                select new { RoomId = cs.ClassroomId, cc.Id, cc.ClassCode, cc.ClassName, cc.StartDate, cc.EndDate, cc.Status }
            ).ToListAsync(cancellationToken);

            var byRoom = activeScheduled
                .GroupBy(x => (x.RoomId, x.Id))
                .Select(g => g.First())
                .GroupBy(x => x.RoomId)
                .ToDictionary(g => g.Key, g => g.OrderBy(x => x.ClassCode).Select(x => new
                {
                    id = x.Id,
                    classCode = x.ClassCode,
                    className = x.ClassName,
                    startDate = x.StartDate,
                    endDate = x.EndDate,
                    status = x.Status
                }).ToList());

            return rooms.Select(r =>
            {
                var roomClasses = byRoom.TryGetValue(r.Id, out var list) ? list : [];
                return (object)new
                {
                    id = r.Id,
                    name = r.Name,
                    location = r.Location,
                    capacity = r.Capacity,
                    defaultTeacherId = (int?)null,
                    defaultTeacherName = (string?)null,
                    defaultCourseId = (int?)null,
                    defaultCourseTitle = (string?)null,
                    scheduleDays = (string?)null,
                    scheduleDaysLabel = (string?)null,
                    defaultTimeSlotId = (int?)null,
                    defaultTimeSlotLabel = (string?)null,
                    openDate = (DateTime?)null,
                    closeDate = (DateTime?)null,
                    isOccupied = roomClasses.Count > 0,
                    activeClasses = roomClasses
                };
            }).ToList();
        }

        private async Task<string?> CheckExclusiveRoomOccupancyAsync(
            int? excludeClassId,
            int classroomId,
            string classroomName,
            DateTime targetStart,
            DateTime targetEnd,
            CancellationToken cancellationToken)
        {
            var scheduleHit = await (
                from cs in _context.ClassSchedules.AsNoTracking()
                join cc in _context.CourseClasses.AsNoTracking() on cs.ClassId equals cc.Id
                where (!excludeClassId.HasValue || cc.Id != excludeClassId.Value)
                      && cs.ClassroomId == classroomId
                      && (cc.Status == "upcoming" || cc.Status == "ongoing")
                      && targetStart <= cc.EndDate
                      && cc.StartDate <= targetEnd
                select new { cc.ClassCode, cc.ClassName, cc.StartDate, cc.EndDate }
            ).FirstOrDefaultAsync(cancellationToken);

            if (scheduleHit != null)
            {
                return $"Không thể mở chung phòng: Phòng {classroomName} đang có lịch mở của lớp {scheduleHit.ClassCode} ({scheduleHit.ClassName}) từ {scheduleHit.StartDate:dd/MM/yyyy} đến {scheduleHit.EndDate:dd/MM/yyyy}.";
            }

            return null;
        }

        private sealed record ClassStudentStatsRow(
            int EnrollmentId,
            int UserId,
            string FullName,
            string Email,
            DateTime EnrolledAt,
            string Status,
            int CompletedLessons,
            int TotalLessons,
            double AttendancePercent,
            decimal? MidtermScore);

        public class ClassCreateDto
        {
            public string ClassCode { get; set; } = string.Empty;
            public string ClassName { get; set; } = string.Empty;
            public int CourseId { get; set; }
            public int? ClassroomId { get; set; }
            public int? TeacherId { get; set; }
            public string? ScheduleDays { get; set; }
            public int? TimeSlotId { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public int MaxStudents { get; set; } = 20;
            public decimal Price { get; set; } = 0;
            public string? Status { get; set; }
        }

        public class ClassUpdateDto
        {
            public string ClassCode { get; set; } = string.Empty;
            public string ClassName { get; set; } = string.Empty;
            public int CourseId { get; set; }
            public int? ClassroomId { get; set; }
            public string? ScheduleDays { get; set; }
            public int? TimeSlotId { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public int MaxStudents { get; set; } = 20;
            public decimal? PriceOverride { get; set; }
            public decimal? Price { get; set; }
            public string? Status { get; set; }
            public int? MainTeacherId { get; set; }
        }

        public class ClassStatusUpdateDto
        {
            public string Status { get; set; } = string.Empty;
        }

        public class ClassTeacherAssignDto
        {
            public int TeacherId { get; set; }
            public string Role { get; set; } = "assistant";
        }

        public class UpdateEnrollmentStatusDto
        {
            public int EnrollmentId { get; set; }
            public string Status { get; set; } = string.Empty;
        }
    }
}
