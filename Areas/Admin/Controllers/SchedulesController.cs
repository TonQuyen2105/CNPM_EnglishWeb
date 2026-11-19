using System.Globalization;
using CNPM_EnglishLearning.Data;
using CNPM_EnglishLearning.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CNPM_EnglishLearning.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class SchedulesController : Controller
    {
        // Khớp [MaxLength(50)] của Entity TimeSlot.Name
        private const int MaxSlotNameLength = 50;
        private const int MaxSlotDescriptionLength = 500;
        private const long TimeSlotLockKey = 700_000_000L;

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
                    timeRange = AdminCoreHelpers.FormatTimeRange(ts.StartTime, ts.EndTime),
                    description = ts.Description
                }).ToList();

                var scheduleQuery =
                    from cs in _context.ClassSchedules.AsNoTracking()
                    join cc in _context.CourseClasses.AsNoTracking() on cs.ClassId equals cc.Id
                    join r in _context.Classrooms.AsNoTracking() on cs.ClassroomId equals r.Id
                    join ts in _context.TimeSlots.AsNoTracking() on cs.TimeSlotId equals ts.Id
                    where cc.Status == "upcoming" || cc.Status == "ongoing"
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

                // Lọc theo giảng viên (bao gồm cả giảng viên chính và trợ giảng) ngay trên PostgreSQL (EXISTS)
                if (teacherId.HasValue && teacherId.Value > 0)
                {
                    var tid = teacherId.Value;
                    scheduleQuery = scheduleQuery.Where(x => _context.ClassTeachers.Any(ct =>
                        ct.ClassId == x.classId && ct.TeacherId == tid));
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
                            dayLabel = AdminCoreHelpers.FormatDayFull(s.dayOfWeek),
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

                var allClassrooms = await _context.Classrooms
                    .AsNoTracking()
                    .OrderBy(r => r.Name)
                    .Select(r => new
                    {
                        id = r.Id,
                        name = r.Name,
                        location = r.Location,
                        capacity = r.Capacity,
                        isActive = r.IsActive
                    })
                    .ToListAsync(cancellationToken);

                var filterClassrooms = allClassrooms
                    .Select(r => new
                    {
                        r.id,
                        r.name,
                        label = r.isActive ? r.name : $"{r.name} (Tạm ngưng)",
                        r.location,
                        r.capacity,
                        r.isActive
                    })
                    .ToList();

                var classrooms = allClassrooms
                    .Where(r => r.isActive)
                    .Select(r => new
                    {
                        r.id,
                        r.name,
                        r.location,
                        r.capacity
                    })
                    .ToList();

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
                    filterClassrooms,
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

                // Khóa giao dịch mức PostgreSQL theo thứ tự tăng dần: Lớp (100M) -> Phòng (200M) -> Giảng viên/Trợ giảng (300M asc) -> Ca học shared (700M)
                long classLockKey = 100_000_000L + classId;
                long roomLockKey = 200_000_000L + classroomId;
                await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({classLockKey})", cancellationToken);
                await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({roomLockKey})", cancellationToken);

                var assignedTeachers = await (
                    from ct in _context.ClassTeachers.AsNoTracking()
                    join t in _context.Teachers.AsNoTracking() on ct.TeacherId equals t.Id
                    join u in _context.Users.AsNoTracking() on t.UserId equals u.Id
                    where ct.ClassId == classId
                    select new { TeacherId = t.Id, TeacherName = u.FullName, ct.Role }
                ).ToListAsync(cancellationToken);

                var teacherIds = assignedTeachers.Select(t => t.TeacherId).Distinct().OrderBy(id => id).ToList();
                foreach (var tid in teacherIds)
                {
                    long teacherLockKey = 300_000_000L + tid;
                    await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({teacherLockKey})", cancellationToken);
                }

                // Khóa chia sẻ trên TimeSlots (700M) để tuần tự hóa với UpdateTimeSlot mà không chặn các request Create chạy song song
                await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock_shared({TimeSlotLockKey})", cancellationToken);

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
                    .Select(c => new { c.ClassCode, c.Status, c.MaxStudents, c.StartDate, c.EndDate })
                    .FirstOrDefaultAsync(cancellationToken);
                if (targetClass == null || (targetClass.Status != "upcoming" && targetClass.Status != "ongoing"))
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiBadRequest("Lớp học không tồn tại hoặc không ở trạng thái đang hoạt động (chỉ xếp lịch cho lớp upcoming hoặc ongoing).");
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
                var targetStart = targetClass.StartDate;
                var targetEnd = targetClass.EndDate;
                var timeLabel = $"{timeSlot.Name} ({AdminCoreHelpers.FormatTimeRange(slotStart, slotEnd)})";

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

                // 2. Trùng phòng học theo khoảng giờ & khoảng ngày giao nhau (chỉ xét lớp upcoming / ongoing)
                var roomConflicts = await (
                    from cs in _context.ClassSchedules.AsNoTracking()
                    join cc in _context.CourseClasses.AsNoTracking() on cs.ClassId equals cc.Id
                    join ts in _context.TimeSlots.AsNoTracking() on cs.TimeSlotId equals ts.Id
                    where cs.ClassroomId == classroomId
                          && cs.ClassId != classId
                          && (cc.Status == "upcoming" || cc.Status == "ongoing")
                          && targetStart <= cc.EndDate
                          && cc.StartDate <= targetEnd
                          && days.Contains(cs.DayOfWeek)
                          && ts.StartTime < slotEnd
                          && slotStart < ts.EndTime
                    select new { cs.DayOfWeek, cc.ClassCode, cc.ClassName, ts.Name, ts.StartTime, ts.EndTime }
                ).ToListAsync(cancellationToken);

                // 3. Trùng lịch cho cả giảng viên chính và trợ giảng theo khoảng giờ & khoảng ngày giao nhau
                var teacherNameById = assignedTeachers
                    .GroupBy(t => t.TeacherId)
                    .ToDictionary(g => g.Key, g => g.First().TeacherName);

                var teacherConflicts = teacherIds.Count == 0
                    ? new List<TeacherConflictRow>()
                    : await (
                        from cs in _context.ClassSchedules.AsNoTracking()
                        join cc in _context.CourseClasses.AsNoTracking() on cs.ClassId equals cc.Id
                        join ct in _context.ClassTeachers.AsNoTracking() on cc.Id equals ct.ClassId
                        join ts in _context.TimeSlots.AsNoTracking() on cs.TimeSlotId equals ts.Id
                        where cs.ClassId != classId
                              && (cc.Status == "upcoming" || cc.Status == "ongoing")
                              && targetStart <= cc.EndDate
                              && cc.StartDate <= targetEnd
                              && teacherIds.Contains(ct.TeacherId)
                              && days.Contains(cs.DayOfWeek)
                              && ts.StartTime < slotEnd
                              && slotStart < ts.EndTime
                        select new TeacherConflictRow
                        {
                            TeacherId = ct.TeacherId,
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
                        var conflictRange = $"{sameClassConflict.Name} ({AdminCoreHelpers.FormatTimeRange(sameClassConflict.StartTime, sameClassConflict.EndTime)})";
                        return this.ApiBadRequest(
                            $"Lớp {targetClass.ClassCode} đã có lịch học trùng/giao giờ vào {AdminCoreHelpers.FormatDayFull(day)}, {conflictRange}.");
                    }

                    var roomConflict = roomConflicts.FirstOrDefault(x => x.DayOfWeek == day);
                    if (roomConflict != null)
                    {
                        await transaction.RollbackAsync(CancellationToken.None);
                        var conflictRange = $"{roomConflict.Name} ({AdminCoreHelpers.FormatTimeRange(roomConflict.StartTime, roomConflict.EndTime)})";
                        return this.ApiBadRequest(
                            $"Trùng phòng học: Phòng {classroom.Name} vào {AdminCoreHelpers.FormatDayFull(day)}, khung giờ {timeLabel} bị giao với ca {conflictRange} của lớp {roomConflict.ClassCode} ({roomConflict.ClassName}).");
                    }

                    var teacherConflict = teacherConflicts.FirstOrDefault(x => x.DayOfWeek == day);
                    if (teacherConflict != null)
                    {
                        await transaction.RollbackAsync(CancellationToken.None);
                        var conflictTeacherName = teacherNameById.GetValueOrDefault(teacherConflict.TeacherId, $"GV #{teacherConflict.TeacherId}");
                        var conflictRange = $"{teacherConflict.Name} ({AdminCoreHelpers.FormatTimeRange(teacherConflict.StartTime, teacherConflict.EndTime)})";
                        return this.ApiBadRequest(
                            $"Trùng giảng viên: Giảng viên {conflictTeacherName} đã được phân công lớp {teacherConflict.ClassCode} ({teacherConflict.ClassName}) vào {AdminCoreHelpers.FormatDayFull(day)}, ca {conflictRange} (giao với {timeLabel}).");
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
            if (id <= 0)
            {
                return this.ApiBadRequest("Mã lịch học không hợp lệ.");
            }

            int targetClassId;
            try
            {
                var foundClassId = await _context.ClassSchedules
                    .AsNoTracking()
                    .Where(s => s.Id == id)
                    .Select(s => (int?)s.ClassId)
                    .FirstOrDefaultAsync(cancellationToken);
                if (!foundClassId.HasValue)
                {
                    return this.ApiNotFound("Không tìm thấy lịch học cần xóa.");
                }

                targetClassId = foundClassId.Value;
            }
            catch (OperationCanceledException)
            {
                return this.ApiCancelled();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể tra cứu lịch học {ScheduleId}.", id);
                return this.ApiServerError("Không thể xóa lịch học.");
            }

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                // Khóa lớp (100M + classId) để tuần tự hóa với Create / cập nhật lớp
                long classLockKey = 100_000_000L + targetClassId;
                await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({classLockKey})", cancellationToken);

                var schedule = await _context.ClassSchedules.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
                if (schedule == null)
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiNotFound("Không tìm thấy lịch học cần xóa.");
                }

                var unlinkedClassLessonsCount = await _context.ClassLessons
                    .AsNoTracking()
                    .CountAsync(cl => cl.ScheduleId == id, cancellationToken);

                _context.ClassSchedules.Remove(schedule);
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                var message = unlinkedClassLessonsCount > 0
                    ? $"Đã xóa lịch học thành công (đã gỡ liên kết khỏi {unlinkedClassLessonsCount} buổi nhật ký giảng dạy)."
                    : "Đã xóa lịch học thành công.";

                return Ok(new { success = true, message, unlinkedClassLessonsCount });
            }
            catch (OperationCanceledException)
            {
                return this.ApiCancelled();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể xóa lịch học {ScheduleId}.", id);
                await transaction.RollbackAsync(CancellationToken.None);
                return this.ApiServerError("Không thể xóa lịch học.");
            }
        }

        [HttpGet("/Admin/api/Schedules/TimeSlots")]
        public async Task<IActionResult> ListTimeSlots(CancellationToken cancellationToken)
        {
            try
            {
                var rawSlots = await _context.TimeSlots
                    .AsNoTracking()
                    .OrderBy(ts => ts.StartTime)
                    .ThenBy(ts => ts.Id)
                    .Select(ts => new
                    {
                        ts.Id,
                        ts.Name,
                        ts.StartTime,
                        ts.EndTime,
                        ts.Description,
                        ts.IsActive
                    })
                    .ToListAsync(cancellationToken);

                var activeCounts = await (
                    from cs in _context.ClassSchedules.AsNoTracking()
                    join cc in _context.CourseClasses.AsNoTracking() on cs.ClassId equals cc.Id
                    where cc.Status == "upcoming" || cc.Status == "ongoing"
                    group cs by cs.TimeSlotId into g
                    select new { TimeSlotId = g.Key, Count = g.Count() }
                ).ToDictionaryAsync(x => x.TimeSlotId, x => x.Count, cancellationToken);

                var items = rawSlots.Select(ts => new
                {
                    id = ts.Id,
                    name = ts.Name,
                    slotName = ts.Name,
                    startTime = $"{ts.StartTime:hh\\:mm}",
                    endTime = $"{ts.EndTime:hh\\:mm}",
                    timeRange = AdminCoreHelpers.FormatTimeRange(ts.StartTime, ts.EndTime),
                    description = ts.Description,
                    isActive = ts.IsActive,
                    activeScheduleCount = activeCounts.GetValueOrDefault(ts.Id)
                }).ToList();

                return Ok(new { success = true, items });
            }
            catch (OperationCanceledException)
            {
                return this.ApiCancelled();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể tải danh mục ca học.");
                return this.ApiServerError("Không thể tải danh mục ca học.");
            }
        }

        [HttpPost("/Admin/api/Schedules/TimeSlots/Create")]
        public async Task<IActionResult> CreateTimeSlot([FromBody] TimeSlotUpsertDto? dto, CancellationToken cancellationToken)
        {
            var validationError = ValidateTimeSlotDto(dto, out var slotName, out var startTime, out var endTime, out var description);
            if (validationError != null)
            {
                return this.ApiBadRequest(validationError);
            }

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({TimeSlotLockKey})", cancellationToken);

                var duplicateName = await _context.TimeSlots
                    .AsNoTracking()
                    .AnyAsync(ts => ts.Name.ToLower() == slotName.ToLower(), cancellationToken);
                if (duplicateName)
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiBadRequest($"Tên ca học '{slotName}' đã tồn tại trong hệ thống.");
                }

                var newSlot = new TimeSlot
                {
                    Name = slotName,
                    StartTime = startTime,
                    EndTime = endTime,
                    Description = description,
                    IsActive = dto!.IsActive ?? true
                };

                _context.TimeSlots.Add(newSlot);
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return Ok(new
                {
                    success = true,
                    message = $"Đã thêm ca học '{newSlot.Name}' ({AdminCoreHelpers.FormatTimeRange(newSlot.StartTime, newSlot.EndTime)}) thành công.",
                    id = newSlot.Id
                });
            }
            catch (OperationCanceledException)
            {
                return this.ApiCancelled();
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                await transaction.RollbackAsync(CancellationToken.None);
                return this.ApiBadRequest($"Tên ca học '{slotName}' đã tồn tại trong hệ thống.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể tạo ca học mới {SlotName}.", slotName);
                await transaction.RollbackAsync(CancellationToken.None);
                return this.ApiServerError("Không thể tạo ca học mới.");
            }
        }

        [HttpPost("/Admin/api/Schedules/TimeSlots/Update/{id}")]
        [HttpPut("/Admin/api/Schedules/TimeSlots/Update/{id}")]
        public async Task<IActionResult> UpdateTimeSlot(int id, [FromBody] TimeSlotUpsertDto? dto, CancellationToken cancellationToken)
        {
            if (id <= 0)
            {
                return this.ApiBadRequest("Mã ca học không hợp lệ.");
            }

            var validationError = ValidateTimeSlotDto(dto, out var slotName, out var startTime, out var endTime, out var description);
            if (validationError != null)
            {
                return this.ApiBadRequest(validationError);
            }

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({TimeSlotLockKey})", cancellationToken);

                var slot = await _context.TimeSlots.FirstOrDefaultAsync(ts => ts.Id == id, cancellationToken);
                if (slot == null)
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiNotFound("Không tìm thấy ca học yêu cầu.");
                }

                var duplicateName = await _context.TimeSlots
                    .AsNoTracking()
                    .AnyAsync(ts => ts.Id != id && ts.Name.ToLower() == slotName.ToLower(), cancellationToken);
                if (duplicateName)
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiBadRequest($"Tên ca học '{slotName}' đã tồn tại trong hệ thống.");
                }

                // Chặn tạm ngưng ca học khi đang có lịch của lớp upcoming / ongoing
                if (dto!.IsActive.HasValue && !dto.IsActive.Value && slot.IsActive)
                {
                    var activeClassesOnSlot = await (
                        from cs in _context.ClassSchedules.AsNoTracking()
                        join cc in _context.CourseClasses.AsNoTracking() on cs.ClassId equals cc.Id
                        where cs.TimeSlotId == id && (cc.Status == "upcoming" || cc.Status == "ongoing")
                        select cc.ClassCode
                    ).Distinct().OrderBy(code => code).ToListAsync(cancellationToken);

                    if (activeClassesOnSlot.Count > 0)
                    {
                        await transaction.RollbackAsync(CancellationToken.None);
                        return this.ApiBadRequest(
                            $"Không thể tạm ngưng ca học '{slot.Name}' vì đang có {activeClassesOnSlot.Count} lớp hoạt động xếp lịch tại ca này ({string.Join(", ", activeClassesOnSlot)}).");
                    }
                }

                // Nếu thay đổi giờ học của ca đang có lịch hoạt động (upcoming / ongoing), kiểm tra không gây giao giờ trên cùng lớp / phòng / giảng viên (main & assistant) có khoảng ngày giao nhau
                if (slot.StartTime != startTime || slot.EndTime != endTime)
                {
                    var affectedSchedules = await (
                        from cs in _context.ClassSchedules.AsNoTracking()
                        join cc in _context.CourseClasses.AsNoTracking() on cs.ClassId equals cc.Id
                        join r in _context.Classrooms.AsNoTracking() on cs.ClassroomId equals r.Id
                        where cs.TimeSlotId == id && (cc.Status == "upcoming" || cc.Status == "ongoing")
                        select new
                        {
                            cs.ClassId,
                            cc.ClassCode,
                            cc.ClassName,
                            cc.StartDate,
                            cc.EndDate,
                            cs.ClassroomId,
                            ClassroomName = r.Name,
                            cs.DayOfWeek
                        }
                    ).ToListAsync(cancellationToken);

                    if (affectedSchedules.Count > 0)
                    {
                        var affectedDays = affectedSchedules.Select(x => x.DayOfWeek).Distinct().ToList();

                        var overlappingOthers = await (
                            from cs in _context.ClassSchedules.AsNoTracking()
                            join cc in _context.CourseClasses.AsNoTracking() on cs.ClassId equals cc.Id
                            join r in _context.Classrooms.AsNoTracking() on cs.ClassroomId equals r.Id
                            join ts in _context.TimeSlots.AsNoTracking() on cs.TimeSlotId equals ts.Id
                            where cs.TimeSlotId != id
                                  && (cc.Status == "upcoming" || cc.Status == "ongoing")
                                  && affectedDays.Contains(cs.DayOfWeek)
                                  && ts.StartTime < endTime
                                  && startTime < ts.EndTime
                            select new
                            {
                                cs.ClassId,
                                cc.ClassCode,
                                cc.ClassName,
                                cc.StartDate,
                                cc.EndDate,
                                cs.ClassroomId,
                                ClassroomName = r.Name,
                                cs.DayOfWeek,
                                SlotName = ts.Name,
                                ts.StartTime,
                                ts.EndTime
                            }
                        ).ToListAsync(cancellationToken);

                        if (overlappingOthers.Count > 0)
                        {
                            var involvedClassIds = affectedSchedules.Select(x => x.ClassId)
                                .Concat(overlappingOthers.Select(x => x.ClassId))
                                .Distinct()
                                .ToList();

                            // Lấy toàn bộ giảng viên (cả main và assistant) của các lớp liên quan
                            var allClassTeachersList = await (
                                from ct in _context.ClassTeachers.AsNoTracking()
                                join t in _context.Teachers.AsNoTracking() on ct.TeacherId equals t.Id
                                join u in _context.Users.AsNoTracking() on t.UserId equals u.Id
                                where involvedClassIds.Contains(ct.ClassId)
                                select new { ct.ClassId, TeacherId = t.Id, TeacherName = u.FullName }
                            ).ToListAsync(cancellationToken);

                            var teachersByClass = allClassTeachersList
                                .GroupBy(x => x.ClassId)
                                .ToDictionary(g => g.Key, g => g.ToList());

                            var newRange = AdminCoreHelpers.FormatTimeRange(startTime, endTime);

                            foreach (var mine in affectedSchedules)
                            {
                                teachersByClass.TryGetValue(mine.ClassId, out var mineTeachers);

                                foreach (var other in overlappingOthers.Where(o => o.DayOfWeek == mine.DayOfWeek))
                                {
                                    var otherRange = AdminCoreHelpers.FormatTimeRange(other.StartTime, other.EndTime);

                                    if (other.ClassId == mine.ClassId)
                                    {
                                        await transaction.RollbackAsync(CancellationToken.None);
                                        return this.ApiBadRequest(
                                            $"Không thể đổi giờ ca {slotName} ({newRange}): Lớp {mine.ClassCode} bị trùng/giao giờ vào {AdminCoreHelpers.FormatDayFull(mine.DayOfWeek)} với ca {other.SlotName} ({otherRange}).");
                                    }

                                    // Chỉ coi là xung đột phòng/giảng viên nếu khoảng ngày [StartDate, EndDate] của 2 lớp có giao nhau
                                    var dateOverlap = mine.StartDate <= other.EndDate && other.StartDate <= mine.EndDate;
                                    if (!dateOverlap)
                                    {
                                        continue;
                                    }

                                    if (other.ClassroomId == mine.ClassroomId)
                                    {
                                        await transaction.RollbackAsync(CancellationToken.None);
                                        return this.ApiBadRequest(
                                            $"Không thể đổi giờ ca {slotName} ({newRange}): Phòng {mine.ClassroomName} vào {AdminCoreHelpers.FormatDayFull(mine.DayOfWeek)} bị trùng giữa lớp {mine.ClassCode} và lớp {other.ClassCode} (ca {other.SlotName} {otherRange}).");
                                    }

                                    if (mineTeachers != null && teachersByClass.TryGetValue(other.ClassId, out var otherTeachers))
                                    {
                                        var sharedTeacher = mineTeachers.FirstOrDefault(mt => otherTeachers.Any(ot => ot.TeacherId == mt.TeacherId));
                                        if (sharedTeacher != null)
                                        {
                                            await transaction.RollbackAsync(CancellationToken.None);
                                            return this.ApiBadRequest(
                                                $"Không thể đổi giờ ca {slotName} ({newRange}): Giảng viên {sharedTeacher.TeacherName} vào {AdminCoreHelpers.FormatDayFull(mine.DayOfWeek)} bị trùng lịch giữa lớp {mine.ClassCode} và lớp {other.ClassCode} (ca {other.SlotName} {otherRange}).");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                slot.Name = slotName;
                slot.StartTime = startTime;
                slot.EndTime = endTime;
                slot.Description = description;
                if (dto.IsActive.HasValue)
                {
                    slot.IsActive = dto.IsActive.Value;
                }

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return Ok(new
                {
                    success = true,
                    message = $"Cập nhật ca học '{slot.Name}' ({AdminCoreHelpers.FormatTimeRange(slot.StartTime, slot.EndTime)}) thành công.",
                    id = slot.Id
                });
            }
            catch (OperationCanceledException)
            {
                return this.ApiCancelled();
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                await transaction.RollbackAsync(CancellationToken.None);
                return this.ApiBadRequest($"Tên ca học '{slotName}' đã tồn tại trong hệ thống.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể cập nhật ca học {TimeSlotId}.", id);
                await transaction.RollbackAsync(CancellationToken.None);
                return this.ApiServerError("Không thể cập nhật ca học.");
            }
        }

        [HttpPost("/Admin/api/Schedules/TimeSlots/ToggleStatus/{id}")]
        public async Task<IActionResult> ToggleTimeSlotStatus(int id, CancellationToken cancellationToken)
        {
            if (id <= 0)
            {
                return this.ApiBadRequest("Mã ca học không hợp lệ.");
            }

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({TimeSlotLockKey})", cancellationToken);

                var slot = await _context.TimeSlots.FirstOrDefaultAsync(ts => ts.Id == id, cancellationToken);
                if (slot == null)
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiNotFound("Không tìm thấy ca học yêu cầu.");
                }

                if (slot.IsActive)
                {
                    var activeClassesOnSlot = await (
                        from cs in _context.ClassSchedules.AsNoTracking()
                        join cc in _context.CourseClasses.AsNoTracking() on cs.ClassId equals cc.Id
                        where cs.TimeSlotId == id && (cc.Status == "upcoming" || cc.Status == "ongoing")
                        select cc.ClassCode
                    ).Distinct().OrderBy(code => code).ToListAsync(cancellationToken);

                    if (activeClassesOnSlot.Count > 0)
                    {
                        await transaction.RollbackAsync(CancellationToken.None);
                        return this.ApiBadRequest(
                            $"Không thể tạm ngưng ca học '{slot.Name}' vì đang có {activeClassesOnSlot.Count} lớp hoạt động xếp lịch tại ca này ({string.Join(", ", activeClassesOnSlot)}).");
                    }
                }

                slot.IsActive = !slot.IsActive;
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                var msg = slot.IsActive
                    ? $"Đã kích hoạt ca học '{slot.Name}'."
                    : $"Đã tạm ngưng ca học '{slot.Name}'.";

                return Ok(new
                {
                    success = true,
                    message = msg,
                    id = slot.Id,
                    isActive = slot.IsActive
                });
            }
            catch (OperationCanceledException)
            {
                return this.ApiCancelled();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể đổi trạng thái ca học {TimeSlotId}.", id);
                await transaction.RollbackAsync(CancellationToken.None);
                return this.ApiServerError("Không thể đổi trạng thái ca học.");
            }
        }

        private static string? ValidateTimeSlotDto(
            TimeSlotUpsertDto? dto,
            out string slotName,
            out TimeSpan startTime,
            out TimeSpan endTime,
            out string? description)
        {
            slotName = string.Empty;
            startTime = TimeSpan.Zero;
            endTime = TimeSpan.Zero;
            description = null;

            if (dto == null)
            {
                return "Dữ liệu ca học không hợp lệ.";
            }

            slotName = (dto.SlotName ?? dto.Name ?? string.Empty).Trim();
            if (slotName.Length == 0 || slotName.Length > MaxSlotNameLength)
            {
                return $"Tên ca học không hợp lệ (1 - {MaxSlotNameLength} ký tự).";
            }

            description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
            if (description != null && description.Length > MaxSlotDescriptionLength)
            {
                return $"Mô tả ca học không được vượt quá {MaxSlotDescriptionLength} ký tự.";
            }

            if (!TryParseClockTime(dto.StartTime, out startTime) || !TryParseClockTime(dto.EndTime, out endTime))
            {
                return "Định dạng giờ bắt đầu hoặc giờ kết thúc không hợp lệ (sử dụng HH:mm, ví dụ 08:00).";
            }

            if (startTime >= endTime)
            {
                return "Giờ kết thúc phải lớn hơn giờ bắt đầu.";
            }

            return null;
        }

        private static bool TryParseClockTime(string? value, out TimeSpan result)
        {
            result = TimeSpan.Zero;
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var trimmed = value.Trim();
            var formats = new[] { @"hh\:mm", @"h\:mm", @"hh\:mm\:ss", @"h\:mm\:ss" };
            if (!TimeSpan.TryParseExact(trimmed, formats, CultureInfo.InvariantCulture, out var parsed))
            {
                return false;
            }

            if (parsed < TimeSpan.Zero || parsed >= TimeSpan.FromHours(24))
            {
                return false;
            }

            result = parsed;
            return true;
        }

        private sealed class TeacherConflictRow
        {
            public int TeacherId { get; set; }
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

        public class TimeSlotUpsertDto
        {
            public string? SlotName { get; set; }
            public string? Name { get; set; }
            public string? StartTime { get; set; }
            public string? EndTime { get; set; }
            public string? Description { get; set; }
            public bool? IsActive { get; set; }
        }
    }
}
