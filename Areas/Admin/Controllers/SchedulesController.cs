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

        public SchedulesController(ApplicationDbContext context)
        {
            _context = context;
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

        [HttpGet("/Admin/api/Schedules/Weekly")]
        public async Task<IActionResult> Weekly(int? classroomId, int? teacherId)
        {
            try
            {
                var rawTimeSlots = await _context.TimeSlots.AsNoTracking()
                    .Where(ts => ts.IsActive)
                    .OrderBy(ts => ts.StartTime)
                    .ToListAsync();

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

                var rawSchedules = await scheduleQuery.ToListAsync();

                var allMainTeachers = await (
                    from ct in _context.ClassTeachers.AsNoTracking()
                    join t in _context.Teachers.AsNoTracking() on ct.TeacherId equals t.Id
                    join u in _context.Users.AsNoTracking() on t.UserId equals u.Id
                    where ct.Role == "main"
                    select new
                    {
                        ct.ClassId,
                        TeacherId = t.Id,
                        TeacherName = u.FullName
                    }
                ).ToListAsync();

                var teacherByClass = allMainTeachers
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
                    .Where(s => !teacherId.HasValue || teacherId.Value <= 0 || s.teacherId == teacherId.Value)
                    .OrderBy(s => s.dayOfWeek)
                    .ThenBy(s => s.classCode)
                    .ToList();

                var openClassesRaw = await _context.CourseClasses.AsNoTracking()
                    .Where(cc => cc.Status == "upcoming" || cc.Status == "ongoing")
                    .OrderBy(cc => cc.ClassCode)
                    .ToListAsync();

                var classes = openClassesRaw.Select(cc =>
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

                var classrooms = await _context.Classrooms.AsNoTracking()
                    .Where(r => r.IsActive)
                    .OrderBy(r => r.Name)
                    .Select(r => new
                    {
                        id = r.Id,
                        name = r.Name,
                        location = r.Location,
                        capacity = r.Capacity
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
                        fullName = u.FullName,
                        qualification = t.Qualification
                    }
                ).ToListAsync();

                return Ok(new
                {
                    timeSlots,
                    schedules,
                    classes,
                    classrooms,
                    teachers
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Không thể tải thời khóa biểu tuần.", detail = ex.Message });
            }
        }

        [HttpPost("/Admin/api/Schedules/Create")]
        public async Task<IActionResult> Create([FromBody] ScheduleCreateDto? dto)
        {
            if (dto == null)
            {
                return BadRequest(new { message = "Dữ liệu gửi lên không hợp lệ." });
            }

            if (dto.ClassId <= 0 || dto.ClassroomId <= 0 || dto.TimeSlotId <= 0)
            {
                return BadRequest(new { message = "Vui lòng chọn đầy đủ lớp học, phòng học và ca học." });
            }

            if (dto.DaysOfWeek == null || dto.DaysOfWeek.Count == 0)
            {
                return BadRequest(new { message = "Vui lòng chọn ít nhất một ngày trong tuần." });
            }

            var days = dto.DaysOfWeek.Distinct().OrderBy(d => d).ToList();
            if (days.Any(d => d < 1 || d > 7))
            {
                return BadRequest(new { message = "Giá trị ngày trong tuần phải nằm trong khoảng từ 1 (Thứ 2) đến 7 (Chủ Nhật)." });
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var classId = dto.ClassId;
                var classroomId = dto.ClassroomId;

                // Khóa giao dịch mức PostgreSQL (pg_advisory_xact_lock) theo Lớp, Phòng và Giảng viên chính để chống xếp lịch trùng đồng thời.
                long classLockKey = 100_000_000L + classId;
                long roomLockKey = 200_000_000L + classroomId;
                await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({classLockKey})");
                await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({roomLockKey})");

                var classroom = await _context.Classrooms
                    .AsNoTracking()
                    .FirstOrDefaultAsync(r => r.Id == classroomId);
                if (classroom == null || !classroom.IsActive)
                {
                    await transaction.RollbackAsync();
                    return BadRequest(new { message = "Phòng học không tồn tại hoặc đang tạm ngưng sử dụng." });
                }

                var targetClass = await _context.CourseClasses
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == classId);
                if (targetClass == null || targetClass.Status == "cancelled")
                {
                    await transaction.RollbackAsync();
                    return BadRequest(new { message = "Lớp học không tồn tại hoặc đã bị hủy." });
                }

                if (classroom.Capacity < targetClass.MaxStudents)
                {
                    await transaction.RollbackAsync();
                    return BadRequest(new
                    {
                        message = $"Sức chứa phòng {classroom.Name} ({classroom.Capacity} chỗ) không đủ cho sĩ số tối đa của lớp {targetClass.ClassCode} ({targetClass.MaxStudents} học viên)."
                    });
                }

                var timeSlot = await _context.TimeSlots
                    .AsNoTracking()
                    .FirstOrDefaultAsync(ts => ts.Id == dto.TimeSlotId);
                if (timeSlot == null || !timeSlot.IsActive)
                {
                    await transaction.RollbackAsync();
                    return BadRequest(new { message = "Ca học không tồn tại hoặc đang ngừng sử dụng." });
                }

                var slotStart = timeSlot.StartTime;
                var slotEnd = timeSlot.EndTime;

                var mainTeacher = await (
                    from ct in _context.ClassTeachers.AsNoTracking()
                    join t in _context.Teachers.AsNoTracking() on ct.TeacherId equals t.Id
                    join u in _context.Users.AsNoTracking() on t.UserId equals u.Id
                    where ct.ClassId == classId && ct.Role == "main"
                    select new { TeacherId = t.Id, TeacherName = u.FullName }
                ).FirstOrDefaultAsync();

                if (mainTeacher != null)
                {
                    long teacherLockKey = 300_000_000L + mainTeacher.TeacherId;
                    await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({teacherLockKey})");
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
                ).ToListAsync();

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
                ).ToListAsync();

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
                    ).ToListAsync();

                foreach (var day in days)
                {
                    var sameClassConflict = sameClassConflicts.FirstOrDefault(x => x.DayOfWeek == day);
                    if (sameClassConflict != null)
                    {
                        await transaction.RollbackAsync();
                        var conflictRange = $"{sameClassConflict.Name} ({FormatTimeRange(sameClassConflict.StartTime, sameClassConflict.EndTime)})";
                        return BadRequest(new
                        {
                            message = $"Lớp {targetClass.ClassCode} đã có lịch học trùng/giao giờ vào {FormatDayFull(day)}, {conflictRange}."
                        });
                    }

                    var roomConflict = roomConflicts.FirstOrDefault(x => x.DayOfWeek == day);
                    if (roomConflict != null)
                    {
                        await transaction.RollbackAsync();
                        var conflictRange = $"{roomConflict.Name} ({FormatTimeRange(roomConflict.StartTime, roomConflict.EndTime)})";
                        return BadRequest(new
                        {
                            message = $"Trùng phòng học: Phòng {classroom.Name} vào {FormatDayFull(day)}, khung giờ {timeLabel} bị giao với ca {conflictRange} của lớp {roomConflict.ClassCode} ({roomConflict.ClassName})."
                        });
                    }

                    var teacherConflict = teacherConflicts.FirstOrDefault(x => x.DayOfWeek == day);
                    if (teacherConflict != null && mainTeacher != null)
                    {
                        await transaction.RollbackAsync();
                        var conflictRange = $"{teacherConflict.Name} ({FormatTimeRange(teacherConflict.StartTime, teacherConflict.EndTime)})";
                        return BadRequest(new
                        {
                            message = $"Trùng giảng viên: Giảng viên {mainTeacher.TeacherName} đã dạy lớp {teacherConflict.ClassCode} ({teacherConflict.ClassName}) vào {FormatDayFull(day)}, ca {conflictRange} (giao với {timeLabel})."
                        });
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
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    message = $"Đã xếp thành công {newSchedules.Count} buổi học trong tuần cho lớp {targetClass.ClassCode}.",
                    count = newSchedules.Count
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = "Không thể xếp lịch học.", detail = ex.Message });
            }
        }

        [HttpPost("/Admin/api/Schedules/Delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var schedule = await _context.ClassSchedules.FirstOrDefaultAsync(s => s.Id == id);
                if (schedule == null)
                {
                    return NotFound(new { message = "Không tìm thấy lịch học cần xóa." });
                }

                _context.ClassSchedules.Remove(schedule);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Đã xóa lịch học thành công." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Không thể xóa lịch học.", detail = ex.Message });
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
