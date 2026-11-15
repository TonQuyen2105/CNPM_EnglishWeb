using CNPM_EnglishLearning.Data;
using CNPM_EnglishLearning.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CNPM_EnglishLearning.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class SchedulesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SchedulesController> _logger;

        public SchedulesController(ApplicationDbContext context, ILogger<SchedulesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View("~/Areas/Admin/Views/schedules.html");
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
            _ => $"Ngày {day}"
        };

        private static string FormatTimeRange(TimeSpan start, TimeSpan end)
        {
            return $"{start:hh\\:mm} - {end:hh\\:mm}";
        }

        /// <param name="lookups">
        /// true: trả kèm danh sách phòng, giảng viên, lớp đang mở (dropdown bộ lọc + modal).
        /// Frontend chỉ yêu cầu ở lần tải đầu tiên; các lần lọc sau gửi false.
        /// </param>
        [HttpGet("/Admin/api/Schedules/Weekly")]
        public async Task<IActionResult> Weekly(int? classroomId, int? teacherId, CancellationToken cancellationToken, bool lookups = true)
        {
            try
            {
                var rawTimeSlots = await _context.TimeSlots
                    .AsNoTracking()
                    .Where(ts => ts.IsActive)
                    .OrderBy(ts => ts.StartTime)
                    .Select(ts => new { ts.Id, ts.Name, ts.StartTime, ts.EndTime, ts.Description })
                    .ToListAsync(cancellationToken);

                var timeSlots = rawTimeSlots.Select(ts => new
                {
                    id = ts.Id,
                    name = ts.Name,
                    startTime = ts.StartTime,
                    endTime = ts.EndTime,
                    timeRange = FormatTimeRange(ts.StartTime, ts.EndTime),
                    description = ts.Description
                }).ToList();

                var scheduleQuery =
                    from cs in _context.ClassSchedules.AsNoTracking()
                    join cc in _context.CourseClasses.AsNoTracking() on cs.ClassId equals cc.Id
                    join r in _context.Classrooms.AsNoTracking() on cs.ClassroomId equals r.Id
                    join ts in _context.TimeSlots.AsNoTracking() on cs.TimeSlotId equals ts.Id
                    where cc.Status != "cancelled"
                    select new
                    {
                        id = cs.Id,
                        classId = cc.Id,
                        classCode = cc.ClassCode,
                        className = cc.ClassName,
                        classStatus = cc.Status,
                        classroomId = r.Id,
                        classroomName = r.Name,
                        timeSlotId = ts.Id,
                        dayOfWeek = cs.DayOfWeek
                    };

                if (classroomId.HasValue && classroomId.Value > 0)
                {
                    var cid = classroomId.Value;
                    scheduleQuery = scheduleQuery.Where(x => x.classroomId == cid);
                }

                // Lọc theo giảng viên chính ngay trên PostgreSQL (EXISTS) thay vì kéo toàn bộ lịch về RAM rồi lọc
                if (teacherId.HasValue && teacherId.Value > 0)
                {
                    var tid = teacherId.Value;
                    scheduleQuery = scheduleQuery.Where(x => _context.ClassTeachers.Any(ct =>
                        ct.ClassId == x.classId && ct.Role == "main" && ct.TeacherId == tid));
                }

                var rawSchedules = await scheduleQuery.ToListAsync(cancellationToken);

                var openClasses = lookups
                    ? await _context.CourseClasses
                        .AsNoTracking()
                        .Where(cc => cc.Status == "upcoming" || cc.Status == "ongoing")
                        .OrderBy(cc => cc.ClassCode)
                        .Select(cc => new { cc.Id, cc.ClassCode, cc.ClassName, cc.Status })
                        .ToListAsync(cancellationToken)
                    : [];

                // Chỉ tải giảng viên chính của các lớp thực sự xuất hiện trong kết quả
                var neededClassIds = rawSchedules.Select(s => s.classId)
                    .Concat(openClasses.Select(c => c.Id))
                    .Distinct()
                    .ToList();

                var mainTeachers = neededClassIds.Count == 0
                    ? []
                    : await (
                        from ct in _context.ClassTeachers.AsNoTracking()
                        join t in _context.Teachers.AsNoTracking() on ct.TeacherId equals t.Id
                        join u in _context.Users.AsNoTracking() on t.UserId equals u.Id
                        where ct.Role == "main" && neededClassIds.Contains(ct.ClassId)
                        select new
                        {
                            ct.ClassId,
                            TeacherId = t.Id,
                            TeacherName = u.FullName
                        }
                    ).ToListAsync(cancellationToken);

                var teacherByClass = mainTeachers
                    .GroupBy(x => x.ClassId)
                    .ToDictionary(g => g.Key, g => g.First());

                var schedules = rawSchedules
                    .Select(s =>
                    {
                        teacherByClass.TryGetValue(s.classId, out var tInfo);
                        return new
                        {
                            s.id,
                            s.classId,
                            s.classCode,
                            s.className,
                            s.classStatus,
                            s.classroomId,
                            s.classroomName,
                            s.timeSlotId,
                            s.dayOfWeek,
                            dayLabel = FormatDayFull(s.dayOfWeek),
                            teacherId = tInfo?.TeacherId,
                            teacherName = tInfo?.TeacherName ?? "Chưa phân công"
                        };
                    })
                    .OrderBy(s => s.dayOfWeek)
                    .ThenBy(s => s.classCode)
                    .ToList();

                if (!lookups)
                {
                    return Ok(new { success = true, timeSlots, schedules });
                }

                var classes = openClasses.Select(cc =>
                {
                    teacherByClass.TryGetValue(cc.Id, out var tInfo);
                    return new
                    {
                        id = cc.Id,
                        classCode = cc.ClassCode,
                        className = cc.ClassName,
                        status = cc.Status,
                        teacherId = tInfo?.TeacherId,
                        teacherName = tInfo?.TeacherName ?? "Chưa phân công"
                    };
                }).ToList();

                var classrooms = await _context.Classrooms
                    .AsNoTracking()
                    .Where(r => r.IsActive)
                    .OrderBy(r => r.Name)
                    .Select(r => new
                    {
                        id = r.Id,
                        name = r.Name,
                        location = r.Location,
                        capacity = r.Capacity
                    })
                    .ToListAsync(cancellationToken);

                var teachers = await (
                    from t in _context.Teachers.AsNoTracking()
                    join u in _context.Users.AsNoTracking() on t.UserId equals u.Id
                    where t.IsVerified
                    orderby u.FullName
                    select new
                    {
                        id = t.Id,
                        fullName = u.FullName,
                        qualification = t.Qualification
                    }
                ).ToListAsync(cancellationToken);

                return Ok(new
                {
                    success = true,
                    timeSlots,
                    schedules,
                    classes,
                    classrooms,
                    teachers
                });
            }
            catch (OperationCanceledException)
            {
                return this.ApiCancelled();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể tải thời khóa biểu tuần.");
                return this.ApiServerError("Không thể tải thời khóa biểu tuần.");
            }
        }

        [HttpPost("/Admin/api/Schedules/Create")]
        public async Task<IActionResult> Create([FromBody] ScheduleCreateDto? dto, CancellationToken cancellationToken)
        {
            if (dto == null)
            {
                return this.ApiBadRequest("Dữ liệu gửi lên không hợp lệ.");
            }

            if (dto.ClassId <= 0 || dto.ClassroomId <= 0 || dto.TimeSlotId <= 0)
            {
                return this.ApiBadRequest("Vui lòng chọn đầy đủ lớp học, phòng học và ca học.");
            }

            if (dto.DaysOfWeek == null || dto.DaysOfWeek.Count == 0)
            {
                return this.ApiBadRequest("Vui lòng chọn ít nhất một ngày trong tuần.");
            }

            var days = dto.DaysOfWeek.Distinct().OrderBy(d => d).ToList();
            if (days.Any(d => d < 1 || d > 7))
            {
                return this.ApiBadRequest("Giá trị ngày trong tuần phải nằm trong khoảng từ 1 (Thứ 2) đến 7 (Chủ Nhật).");
            }

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var classId = dto.ClassId;
                var classroomId = dto.ClassroomId;

                // Khóa giao dịch mức PostgreSQL (pg_advisory_xact_lock) theo Lớp, Phòng và Giảng viên chính để chống xếp lịch trùng đồng thời.
                long classLockKey = 100_000_000L + classId;
                long roomLockKey = 200_000_000L + classroomId;
                await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({classLockKey})", cancellationToken);
                await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({roomLockKey})", cancellationToken);

                var classroom = await _context.Classrooms
                    .AsNoTracking()
                    .Where(r => r.Id == classroomId)
                    .Select(r => new { r.Name, r.Capacity, r.IsActive })
                    .FirstOrDefaultAsync(cancellationToken);
                if (classroom == null || !classroom.IsActive)
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiBadRequest("Phòng học không tồn tại hoặc đang tạm ngưng sử dụng.");
                }

                var targetClass = await _context.CourseClasses
                    .AsNoTracking()
                    .Where(c => c.Id == classId)
                    .Select(c => new { c.ClassCode, c.Status, c.MaxStudents })
                    .FirstOrDefaultAsync(cancellationToken);
                if (targetClass == null || targetClass.Status == "cancelled")
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiBadRequest("Lớp học không tồn tại hoặc đã bị hủy.");
                }

                if (classroom.Capacity < targetClass.MaxStudents)
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiBadRequest(
                        $"Sức chứa phòng {classroom.Name} ({classroom.Capacity} chỗ) không đủ cho sĩ số tối đa của lớp {targetClass.ClassCode} ({targetClass.MaxStudents} học viên).");
                }

                var timeSlot = await _context.TimeSlots
                    .AsNoTracking()
                    .Where(ts => ts.Id == dto.TimeSlotId)
                    .Select(ts => new { ts.Name, ts.StartTime, ts.EndTime, ts.IsActive })
                    .FirstOrDefaultAsync(cancellationToken);
                if (timeSlot == null || !timeSlot.IsActive)
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiBadRequest("Ca học không tồn tại hoặc đang ngừng sử dụng.");
                }

                var slotStart = timeSlot.StartTime;
                var slotEnd = timeSlot.EndTime;

                var mainTeacher = await (
                    from ct in _context.ClassTeachers.AsNoTracking()
                    join t in _context.Teachers.AsNoTracking() on ct.TeacherId equals t.Id
                    join u in _context.Users.AsNoTracking() on t.UserId equals u.Id
                    where ct.ClassId == classId && ct.Role == "main"
                    select new { TeacherId = t.Id, TeacherName = u.FullName }
                ).FirstOrDefaultAsync(cancellationToken);

                if (mainTeacher != null)
                {
                    long teacherLockKey = 300_000_000L + mainTeacher.TeacherId;
                    await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({teacherLockKey})", cancellationToken);
                }

                var timeLabel = $"{timeSlot.Name} ({FormatTimeRange(slotStart, slotEnd)})";

                // Mỗi loại xung đột chỉ cần 1 truy vấn cho toàn bộ các ngày được chọn (không truy vấn trong vòng lặp).
                // 1. Lớp này đã có lịch giao nhau vào cùng ngày chưa
                var sameClassConflicts = await (
                    from cs in _context.ClassSchedules.AsNoTracking()
                    join ts in _context.TimeSlots.AsNoTracking() on cs.TimeSlotId equals ts.Id
                    where cs.ClassId == classId
                          && days.Contains(cs.DayOfWeek)
                          && ts.StartTime < slotEnd
                          && slotStart < ts.EndTime
                    select new { cs.DayOfWeek, ts.Name, ts.StartTime, ts.EndTime }
                ).ToListAsync(cancellationToken);

                // 2. Trùng phòng học theo khoảng thời gian giao nhau (lớp có Status != "cancelled")
                var roomConflicts = await (
                    from cs in _context.ClassSchedules.AsNoTracking()
                    join cc in _context.CourseClasses.AsNoTracking() on cs.ClassId equals cc.Id
                    join ts in _context.TimeSlots.AsNoTracking() on cs.TimeSlotId equals ts.Id
                    where cs.ClassroomId == classroomId
                          && days.Contains(cs.DayOfWeek)
                          && cc.Status != "cancelled"
                          && ts.StartTime < slotEnd
                          && slotStart < ts.EndTime
                    select new { cs.DayOfWeek, cc.ClassCode, cc.ClassName, ts.Name, ts.StartTime, ts.EndTime }
                ).ToListAsync(cancellationToken);

                // 3. Trùng giảng viên chính theo khoảng thời gian giao nhau
                var teacherConflicts = mainTeacher == null
                    ? new List<TeacherConflictRow>()
                    : await (
                        from cs in _context.ClassSchedules.AsNoTracking()
                        join cc in _context.CourseClasses.AsNoTracking() on cs.ClassId equals cc.Id
                        join ct in _context.ClassTeachers.AsNoTracking() on cc.Id equals ct.ClassId
                        join ts in _context.TimeSlots.AsNoTracking() on cs.TimeSlotId equals ts.Id
                        where cs.ClassId != classId
                              && cc.Status != "cancelled"
                              && ct.TeacherId == mainTeacher.TeacherId
                              && ct.Role == "main"
                              && days.Contains(cs.DayOfWeek)
                              && ts.StartTime < slotEnd
                              && slotStart < ts.EndTime
                        select new TeacherConflictRow
                        {
                            DayOfWeek = cs.DayOfWeek,
                            ClassCode = cc.ClassCode,
                            ClassName = cc.ClassName,
                            Name = ts.Name,
                            StartTime = ts.StartTime,
                            EndTime = ts.EndTime
                        }
                    ).ToListAsync(cancellationToken);

                foreach (var day in days)
                {
                    var sameClassConflict = sameClassConflicts.FirstOrDefault(x => x.DayOfWeek == day);
                    if (sameClassConflict != null)
                    {
                        await transaction.RollbackAsync(CancellationToken.None);
                        var conflictRange = $"{sameClassConflict.Name} ({FormatTimeRange(sameClassConflict.StartTime, sameClassConflict.EndTime)})";
                        return this.ApiBadRequest(
                            $"Lớp {targetClass.ClassCode} đã có lịch học trùng/giao giờ vào {FormatDayFull(day)}, {conflictRange}.");
                    }

                    var roomConflict = roomConflicts.FirstOrDefault(x => x.DayOfWeek == day);
                    if (roomConflict != null)
                    {
                        await transaction.RollbackAsync(CancellationToken.None);
                        var conflictRange = $"{roomConflict.Name} ({FormatTimeRange(roomConflict.StartTime, roomConflict.EndTime)})";
                        return this.ApiBadRequest(
                            $"Trùng phòng học: Phòng {classroom.Name} vào {FormatDayFull(day)}, khung giờ {timeLabel} bị giao với ca {conflictRange} của lớp {roomConflict.ClassCode} ({roomConflict.ClassName}).");
                    }

                    var teacherConflict = teacherConflicts.FirstOrDefault(x => x.DayOfWeek == day);
                    if (teacherConflict != null && mainTeacher != null)
                    {
                        await transaction.RollbackAsync(CancellationToken.None);
                        var conflictRange = $"{teacherConflict.Name} ({FormatTimeRange(teacherConflict.StartTime, teacherConflict.EndTime)})";
                        return this.ApiBadRequest(
                            $"Trùng giảng viên: Giảng viên {mainTeacher.TeacherName} đã dạy lớp {teacherConflict.ClassCode} ({teacherConflict.ClassName}) vào {FormatDayFull(day)}, ca {conflictRange} (giao với {timeLabel}).");
                    }
                }

                var now = DateTime.UtcNow;
                var newSchedules = days.Select(day => new ClassSchedule
                {
                    ClassId = dto.ClassId,
                    ClassroomId = dto.ClassroomId,
                    TimeSlotId = dto.TimeSlotId,
                    DayOfWeek = day,
                    CreatedAt = now
                }).ToList();

                _context.ClassSchedules.AddRange(newSchedules);
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return Ok(new
                {
                    success = true,
                    message = $"Đã xếp thành công {newSchedules.Count} buổi học trong tuần cho lớp {targetClass.ClassCode}.",
                    count = newSchedules.Count
                });
            }
            catch (OperationCanceledException)
            {
                return this.ApiCancelled();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể xếp lịch học cho lớp {ClassId}.", dto.ClassId);
                await transaction.RollbackAsync(CancellationToken.None);
                return this.ApiServerError("Không thể xếp lịch học.");
            }
        }

        [HttpPost("/Admin/api/Schedules/Delete/{id}")]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            try
            {
                var schedule = await _context.ClassSchedules.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
                if (schedule == null)
                {
                    return this.ApiNotFound("Không tìm thấy lịch học cần xóa.");
                }

                _context.ClassSchedules.Remove(schedule);
                await _context.SaveChangesAsync(cancellationToken);

                return Ok(new { success = true, message = "Đã xóa lịch học thành công." });
            }
            catch (OperationCanceledException)
            {
                return this.ApiCancelled();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể xóa lịch học {ScheduleId}.", id);
                return this.ApiServerError("Không thể xóa lịch học.");
            }
        }

        private sealed class TeacherConflictRow
        {
            public int DayOfWeek { get; set; }
            public string ClassCode { get; set; } = string.Empty;
            public string ClassName { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public TimeSpan StartTime { get; set; }
            public TimeSpan EndTime { get; set; }
        }

        public class ScheduleCreateDto
        {
            public int ClassId { get; set; }
            public int ClassroomId { get; set; }
            public int TimeSlotId { get; set; }
            public List<int> DaysOfWeek { get; set; } = new();
        }
    }
}
