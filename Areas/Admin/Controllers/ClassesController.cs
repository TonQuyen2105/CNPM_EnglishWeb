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

        private static string FormatDayShort(int day) => day switch
        {
            1 => "T2",
            2 => "T3",
            3 => "T4",
            4 => "T5",
            5 => "T6",
            6 => "T7",
            7 => "CN",
            _ => $"T{day}"
        };

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

        private static string FormatTimeRange(TimeSpan start, TimeSpan end)
        {
            return $"{start:hh\\:mm} - {end:hh\\:mm}";
        }

        /// <param name="lookups">
        /// true: trả kèm danh sách khóa học + giảng viên (dropdown modal tạo lớp).
        /// Frontend chỉ yêu cầu ở lần tải đầu tiên; các lần lọc/tìm kiếm sau gửi false.
        /// </param>
        [HttpGet("/Admin/api/Classes/List")]
        public async Task<IActionResult> List(string? status, string? search, int? courseId, CancellationToken cancellationToken, bool lookups = true)
        {
            try
            {
                var query = _context.CourseClasses.AsNoTracking().AsQueryable();

                if (courseId.HasValue && courseId.Value > 0)
                {
                    query = query.Where(c => c.CourseId == courseId.Value);
                }

                if (!string.IsNullOrWhiteSpace(status) && !status.Equals("all", StringComparison.OrdinalIgnoreCase))
                {
                    var normalizedStatus = status.Trim().ToLowerInvariant();
                    query = query.Where(c => c.Status == normalizedStatus);
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var term = search.Trim().ToLower();
                    query = query.Where(c =>
                        c.ClassCode.ToLower().Contains(term) ||
                        c.ClassName.ToLower().Contains(term) ||
                        (c.Course != null && c.Course.Title.ToLower().Contains(term)));
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

                var courses = lookups
                    ? await _context.Courses
                        .AsNoTracking()
                        .OrderBy(c => c.Title)
                        .Select(c => new
                        {
                            id = c.Id,
                            title = c.Title,
                            price = c.Price,
                            level = c.Level
                        })
                        .ToListAsync(cancellationToken)
                    : null;

                var teachers = lookups
                    ? await (
                        from t in _context.Teachers.AsNoTracking()
                        join u in _context.Users.AsNoTracking() on t.UserId equals u.Id
                        where t.IsVerified
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

                if (classes.Count == 0)
                {
                    return Ok(new { success = true, items = Array.Empty<object>(), courses, teachers });
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
                        ClassroomName = r.Name,
                        cs.DayOfWeek,
                        ts.StartTime,
                        ts.EndTime
                    }
                ).ToListAsync(cancellationToken);

                var schedulesByClass = schedules
                    .GroupBy(x => x.ClassId)
                    .ToDictionary(g => g.Key, g => g.ToList());

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

                    var classroomSummary = (clsSchedules != null && clsSchedules.Count > 0)
                        ? string.Join(", ", clsSchedules.Select(s => s.ClassroomName).Distinct())
                        : "Chưa xếp phòng";

                    var scheduleSummary = (clsSchedules != null && clsSchedules.Count > 0)
                        ? string.Join("; ", clsSchedules
                            .GroupBy(s => new { s.StartTime, s.EndTime })
                            .Select(g =>
                            {
                                var days = string.Join("-", g.Select(x => FormatDayShort(x.DayOfWeek)).Distinct());
                                return $"{days} ({FormatTimeRange(g.Key.StartTime, g.Key.EndTime)})";
                            }))
                        : "Chưa xếp lịch";

                    return new
                    {
                        id = c.Id,
                        courseId = c.CourseId,
                        courseTitle = c.CourseTitle ?? "--",
                        classCode = c.ClassCode,
                        className = c.ClassName,
                        teacherId = teacherInfo?.TeacherId,
                        teacherName = teacherInfo?.TeacherName ?? "Chưa phân công",
                        classroomSummary,
                        scheduleSummary,
                        startDate = c.StartDate,
                        endDate = c.EndDate,
                        activeStudents = activeCounts.GetValueOrDefault(c.Id),
                        maxStudents = c.MaxStudents,
                        price = c.Price,
                        status = c.Status
                    };
                }).ToList();

                return Ok(new { success = true, items, courses, teachers });
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
            if (!ValidClassStatuses.Contains(status))
            {
                return this.ApiBadRequest("Trạng thái lớp học không hợp lệ.");
            }

            var teacherId = dto.TeacherId.HasValue && dto.TeacherId.Value > 0 ? dto.TeacherId : null;

            try
            {
                if (dto.CourseId <= 0 || !await _context.Courses.AsNoTracking().AnyAsync(c => c.Id == dto.CourseId, cancellationToken))
                {
                    return this.ApiBadRequest("Khóa học được chọn không tồn tại.");
                }

                var duplicateCode = await _context.CourseClasses.AsNoTracking()
                    .AnyAsync(c => c.ClassCode.ToLower() == classCode.ToLower(), cancellationToken);
                if (duplicateCode)
                {
                    return this.ApiBadRequest($"Mã lớp '{classCode}' đã tồn tại trong hệ thống.");
                }

                if (teacherId.HasValue)
                {
                    var teacherValid = await _context.Teachers.AsNoTracking()
                        .AnyAsync(t => t.Id == teacherId.Value && t.IsVerified, cancellationToken);
                    if (!teacherValid)
                    {
                        return this.ApiBadRequest("Giảng viên được chọn không tồn tại hoặc chưa được xác minh.");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                return this.ApiCancelled();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể kiểm tra dữ liệu tạo lớp học {ClassCode}.", classCode);
                return this.ApiServerError("Không thể tạo lớp học.");
            }

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể tạo lớp học {ClassCode}.", classCode);
                await transaction.RollbackAsync(CancellationToken.None);
                return this.ApiServerError("Không thể tạo lớp học.");
            }
        }

        [HttpGet("/Admin/api/Classes/Detail/{id}")]
        public async Task<IActionResult> DetailApi(int id, CancellationToken cancellationToken)
        {
            try
            {
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

                var mainTeacher = await (
                    from ct in _context.ClassTeachers.AsNoTracking()
                    join t in _context.Teachers.AsNoTracking() on ct.TeacherId equals t.Id
                    join u in _context.Users.AsNoTracking() on t.UserId equals u.Id
                    where ct.ClassId == id && ct.Role == "main"
                    select new
                    {
                        teacherId = t.Id,
                        fullName = u.FullName,
                        email = u.Email,
                        qualification = t.Qualification
                    }
                ).FirstOrDefaultAsync(cancellationToken);

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

                var scheduleFormatted = schedules.Select(s => new
                {
                    s.id,
                    s.dayOfWeek,
                    dayLabel = FormatDayFull(s.dayOfWeek),
                    dayShort = FormatDayShort(s.dayOfWeek),
                    s.classroomName,
                    s.classroomLocation,
                    s.timeSlotName,
                    timeRange = FormatTimeRange(s.startTime, s.endTime)
                }).ToList();

                // Đếm số bài học bằng COUNT(*) trên PostgreSQL thay vì kéo toàn bộ danh sách Id về RAM
                var totalLessons = await _context.Lessons
                    .AsNoTracking()
                    .CountAsync(l => l.CourseId == courseId, cancellationToken);

                var enrollments = await (
                    from e in _context.Enrollments.AsNoTracking()
                    join u in _context.Users.AsNoTracking() on e.UserId equals u.Id
                    where e.ClassId == id
                    orderby e.EnrolledAt ascending, e.Id ascending
                    select new
                    {
                        enrollmentId = e.Id,
                        userId = u.Id,
                        fullName = u.FullName,
                        email = u.Email,
                        enrolledAt = e.EnrolledAt,
                        status = e.Status
                    }
                ).ToListAsync(cancellationToken);

                var studentIds = enrollments.Select(e => e.userId).Distinct().ToList();

                // Số bài học hoàn thành mỗi học viên: JOIN + GROUP BY + COUNT(DISTINCT) hoàn toàn trên PostgreSQL
                var completedCountByUser = totalLessons > 0 && studentIds.Count > 0
                    ? await (
                        from up in _context.UserProgresses.AsNoTracking()
                        join l in _context.Lessons.AsNoTracking() on up.LessonId equals l.Id
                        where l.CourseId == courseId && up.IsCompleted && studentIds.Contains(up.UserId)
                        group up.LessonId by up.UserId into g
                        select new { UserId = g.Key, Count = g.Distinct().Count() }
                    ).ToDictionaryAsync(x => x.UserId, x => x.Count, cancellationToken)
                    : new Dictionary<int, int>();

                // Điểm cao nhất mỗi học viên: JOIN + GROUP BY + MAX trên PostgreSQL
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

                var students = enrollments.Select(e =>
                {
                    var completedLessons = completedCountByUser.GetValueOrDefault(e.userId);
                    var attendancePercent = totalLessons > 0
                        ? Math.Round(completedLessons * 100.0 / totalLessons, 1)
                        : 0.0;

                    decimal? midtermScore = maxScoreByUser.TryGetValue(e.userId, out var score) ? score : null;

                    return new
                    {
                        e.enrollmentId,
                        e.userId,
                        e.fullName,
                        e.email,
                        e.enrolledAt,
                        e.status,
                        completedLessons,
                        totalLessons,
                        attendancePercent,
                        midtermScore
                    };
                }).ToList();

                var activeStudentCount = students.Count(s => s.status == "active");
                var attendancePool = students.Where(s => s.status != "cancelled").ToList();
                var averageAttendance = attendancePool.Count > 0
                    ? Math.Round(attendancePool.Average(s => s.attendancePercent), 1)
                    : 0.0;

                var scoredStudents = attendancePool.Where(s => s.midtermScore.HasValue).ToList();
                decimal? averageMidtermScore = scoredStudents.Count > 0
                    ? Math.Round(scoredStudents.Average(s => s.midtermScore!.Value), 2)
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
                        teacherName = mainTeacher?.fullName ?? "Chưa phân công",
                        teacherEmail = mainTeacher?.email,
                        teacherQualification = mainTeacher?.qualification,
                        totalLessons,
                        averageAttendance,
                        averageMidtermScore
                    },
                    schedules = scheduleFormatted,
                    students
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

            int? targetClassId;
            try
            {
                targetClassId = await _context.Enrollments
                    .AsNoTracking()
                    .Where(e => e.Id == dto.EnrollmentId)
                    .Select(e => (int?)e.ClassId)
                    .FirstOrDefaultAsync(cancellationToken);
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

                var enrollment = await _context.Enrollments.FirstOrDefaultAsync(e => e.Id == dto.EnrollmentId, cancellationToken);
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
                _logger.LogError(ex, "Không thể cập nhật trạng thái ghi danh {EnrollmentId}.", dto.EnrollmentId);
                await transaction.RollbackAsync(CancellationToken.None);
                return this.ApiServerError("Không thể cập nhật trạng thái ghi danh.");
            }
        }

        public class ClassCreateDto
        {
            public string ClassCode { get; set; } = string.Empty;
            public string ClassName { get; set; } = string.Empty;
            public int CourseId { get; set; }
            public int? TeacherId { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public int MaxStudents { get; set; } = 20;
            public decimal Price { get; set; } = 0;
            public string? Status { get; set; }
        }

        public class UpdateEnrollmentStatusDto
        {
            public int EnrollmentId { get; set; }
            public string Status { get; set; } = string.Empty;
        }
    }
}
