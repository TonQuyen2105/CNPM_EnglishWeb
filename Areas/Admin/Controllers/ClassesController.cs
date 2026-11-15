using CNPM_EnglishLearning.Data;
using CNPM_EnglishLearning.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CNPM_EnglishLearning.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ClassesController : Controller
    {
        private readonly ApplicationDbContext _context;

        private static readonly HashSet<string> ValidClassStatuses = new(StringComparer.OrdinalIgnoreCase)
        {
            "upcoming", "ongoing", "completed", "cancelled"
        };

        private static readonly HashSet<string> ValidEnrollmentStatuses = new(StringComparer.OrdinalIgnoreCase)
        {
            "active", "completed", "cancelled"
        };

        public ClassesController(ApplicationDbContext context)
        {
            _context = context;
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

        [HttpGet("/Admin/api/Classes/List")]
        public async Task<IActionResult> List(string? status, string? search)
        {
            try
            {
                var query = _context.CourseClasses.AsNoTracking()
                    .Include(c => c.Course)
                    .AsQueryable();

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

                var classes = await query
                    .OrderByDescending(c => c.Id)
                    .ToListAsync();

                var courses = await _context.Courses.AsNoTracking()
                    .OrderBy(c => c.Title)
                    .Select(c => new
                    {
                        id = c.Id,
                        title = c.Title,
                        price = c.Price,
                        level = c.Level
                    })
                    .ToListAsync();

                var teachers = await (
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
                ).ToListAsync();

                if (classes.Count == 0)
                {
                    return Ok(new { items = Array.Empty<object>(), courses, teachers });
                }

                var classIds = classes.Select(c => c.Id).ToList();

                var mainTeachers = await (
                    from ct in _context.ClassTeachers.AsNoTracking()
                    join t in _context.Teachers.AsNoTracking() on ct.TeacherId equals t.Id
                    join u in _context.Users.AsNoTracking() on t.UserId equals u.Id
                    where classIds.Contains(ct.ClassId) && ct.Role == "main"
                    select new { ct.ClassId, TeacherId = t.Id, TeacherName = u.FullName }
                ).ToListAsync();

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
                ).ToListAsync();

                var schedulesByClass = schedules
                    .GroupBy(x => x.ClassId)
                    .ToDictionary(g => g.Key, g => g.ToList());

                var activeCounts = await _context.Enrollments.AsNoTracking()
                    .Where(e => classIds.Contains(e.ClassId) && e.Status == "active")
                    .GroupBy(e => e.ClassId)
                    .Select(g => new { ClassId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.ClassId, x => x.Count);

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
                        courseTitle = c.Course?.Title ?? "--",
                        classCode = c.ClassCode,
                        className = c.ClassName,
                        teacherId = teacherInfo?.TeacherId,
                        teacherName = teacherInfo?.TeacherName ?? "Chưa phân công",
                        classroomSummary,
                        scheduleSummary,
                        startDate = c.StartDate,
                        endDate = c.EndDate,
                        activeStudents = activeCounts.TryGetValue(c.Id, out var cnt) ? cnt : 0,
                        maxStudents = c.MaxStudents,
                        price = c.Price,
                        status = c.Status
                    };
                });

                return Ok(new { items, courses, teachers });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Không thể tải danh sách lớp học.", detail = ex.Message });
            }
        }

        [HttpPost("/Admin/api/Classes/Create")]
        public async Task<IActionResult> Create([FromBody] ClassCreateDto? dto)
        {
            if (dto == null)
            {
                return BadRequest(new { message = "Dữ liệu gửi lên không hợp lệ." });
            }

            if (string.IsNullOrWhiteSpace(dto.ClassCode) || string.IsNullOrWhiteSpace(dto.ClassName))
            {
                return BadRequest(new { message = "Mã lớp và tên lớp không được để trống." });
            }

            var classCode = dto.ClassCode.Trim();
            var className = dto.ClassName.Trim();

            if (classCode.Length > 50)
            {
                return BadRequest(new { message = "Mã lớp không được vượt quá 50 ký tự." });
            }

            if (className.Length > 150)
            {
                return BadRequest(new { message = "Tên lớp không được vượt quá 150 ký tự." });
            }

            if (dto.CourseId <= 0 || !await _context.Courses.AnyAsync(c => c.Id == dto.CourseId))
            {
                return BadRequest(new { message = "Khóa học được chọn không tồn tại." });
            }

            var duplicateCode = await _context.CourseClasses
                .AnyAsync(c => c.ClassCode.ToLower() == classCode.ToLower());
            if (duplicateCode)
            {
                return BadRequest(new { message = $"Mã lớp '{classCode}' đã tồn tại trong hệ thống." });
            }

            if (dto.EndDate.Date <= dto.StartDate.Date)
            {
                return BadRequest(new { message = "Ngày kết thúc phải sau ngày khai giảng." });
            }

            if (dto.MaxStudents <= 0)
            {
                return BadRequest(new { message = "Sĩ số tối đa phải lớn hơn 0." });
            }

            if (dto.Price < 0)
            {
                return BadRequest(new { message = "Học phí không được nhỏ hơn 0." });
            }

            var status = string.IsNullOrWhiteSpace(dto.Status) ? "upcoming" : dto.Status.Trim().ToLowerInvariant();
            if (!ValidClassStatuses.Contains(status))
            {
                return BadRequest(new { message = "Trạng thái lớp học không hợp lệ." });
            }

            if (dto.TeacherId.HasValue && dto.TeacherId.Value > 0)
            {
                var teacherValid = await _context.Teachers.AnyAsync(t => t.Id == dto.TeacherId.Value && t.IsVerified);
                if (!teacherValid)
                {
                    return BadRequest(new { message = "Giảng viên được chọn không tồn tại hoặc chưa được xác minh." });
                }
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
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
                await _context.SaveChangesAsync();

                if (dto.TeacherId.HasValue && dto.TeacherId.Value > 0)
                {
                    _context.ClassTeachers.Add(new ClassTeacher
                    {
                        ClassId = newClass.Id,
                        TeacherId = dto.TeacherId.Value,
                        Role = "main",
                        AssignedAt = DateTime.UtcNow
                    });
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();

                return Ok(new { message = "Tạo lớp học thành công.", id = newClass.Id, item = newClass });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = "Không thể tạo lớp học.", detail = ex.Message });
            }
        }

        [HttpGet("/Admin/api/Classes/Detail/{id}")]
        public async Task<IActionResult> DetailApi(int id)
        {
            try
            {
                var cls = await _context.CourseClasses.AsNoTracking()
                    .Include(c => c.Course)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (cls == null)
                {
                    return NotFound(new { message = "Không tìm thấy lớp học." });
                }

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
                ).FirstOrDefaultAsync();

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
                ).ToListAsync();

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

                var lessonIds = await _context.Lessons.AsNoTracking()
                    .Where(l => l.CourseId == cls.CourseId)
                    .Select(l => l.Id)
                    .ToListAsync();
                var totalLessons = lessonIds.Count;

                var examIds = await _context.Exams.AsNoTracking()
                    .Where(e => e.CourseId == cls.CourseId)
                    .Select(e => e.Id)
                    .ToListAsync();

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
                ).ToListAsync();

                var studentIds = enrollments.Select(e => e.userId).Distinct().ToList();

                var completedCountByUser = new Dictionary<int, int>();
                if (totalLessons > 0 && studentIds.Count > 0)
                {
                    var completedRows = await _context.UserProgresses.AsNoTracking()
                        .Where(up => studentIds.Contains(up.UserId) && lessonIds.Contains(up.LessonId) && up.IsCompleted)
                        .Select(up => new { up.UserId, up.LessonId })
                        .ToListAsync();

                    completedCountByUser = completedRows
                        .GroupBy(x => x.UserId)
                        .ToDictionary(g => g.Key, g => g.Select(x => x.LessonId).Distinct().Count());
                }

                var maxScoreByUser = new Dictionary<int, decimal?>();
                if (examIds.Count > 0 && studentIds.Count > 0)
                {
                    var examRows = await _context.UserExams.AsNoTracking()
                        .Where(ue => studentIds.Contains(ue.UserId) && examIds.Contains(ue.ExamId) && ue.Status != "in_progress" && ue.TotalScore != null)
                        .Select(ue => new { ue.UserId, ue.TotalScore })
                        .ToListAsync();

                    maxScoreByUser = examRows
                        .GroupBy(x => x.UserId)
                        .ToDictionary(g => g.Key, g => g.Max(x => x.TotalScore));
                }

                var students = enrollments.Select(e =>
                {
                    var completedLessons = completedCountByUser.TryGetValue(e.userId, out var cCount) ? cCount : 0;
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
                    classInfo = new
                    {
                        id = cls.Id,
                        courseId = cls.CourseId,
                        courseTitle = cls.Course?.Title ?? "--",
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
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Không thể tải chi tiết lớp học.", detail = ex.Message });
            }
        }

        [HttpPost("/Admin/api/Classes/UpdateEnrollmentStatus")]
        public async Task<IActionResult> UpdateEnrollmentStatus([FromBody] UpdateEnrollmentStatusDto? dto)
        {
            if (dto == null || dto.EnrollmentId <= 0)
            {
                return BadRequest(new { message = "Dữ liệu cập nhật không hợp lệ." });
            }

            var newStatus = (dto.Status ?? string.Empty).Trim().ToLowerInvariant();
            if (!ValidEnrollmentStatuses.Contains(newStatus))
            {
                return BadRequest(new { message = "Trạng thái ghi danh phải là active, completed hoặc cancelled." });
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var enrollment = await _context.Enrollments.FirstOrDefaultAsync(e => e.Id == dto.EnrollmentId);
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

                    var hasOtherActive = await _context.Enrollments
                        .AnyAsync(e => e.Id != enrollment.Id && e.UserId == enrollment.UserId && e.ClassId == enrollment.ClassId && e.Status == "active");
                    if (hasOtherActive)
                    {
                        await transaction.RollbackAsync();
                        return BadRequest(new { message = "Học viên này đã có một ghi danh đang hoạt động (active) khác trong lớp." });
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
