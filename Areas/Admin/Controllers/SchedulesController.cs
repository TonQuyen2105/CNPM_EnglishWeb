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
                await AdminCoreHelpers.SyncExpiredClassesAsync(_context, cancellationToken);

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
                    startTime = AdminCoreHelpers.FormatTime24(ts.StartTime),
                    endTime = AdminCoreHelpers.FormatTime24(ts.EndTime),
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
                        startDate = cc.StartDate,
                        endDate = cc.EndDate,
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
                        .Select(cc => new { cc.Id, cc.ClassCode, cc.ClassName, cc.Status, cc.MaxStudents, cc.StartDate, cc.EndDate })
                        .ToListAsync(cancellationToken)
                    : [];

                // Nạp đồng thời xung đột lớp học & cấu hình lịch phòng học từ shared helper
                var systemState = await AdminCoreHelpers.DetectSystemScheduleStateAsync(_context, cancellationToken);
                var systemConflicts = systemState.Conflicts;
                var conflictsByEntryKey = new Dictionary<string, List<AdminCoreHelpers.SystemConflictDetail>>();
                foreach (var c in systemConflicts)
                {
                    foreach (var key in new[] { c.EntryKey, c.OtherEntryKey }.Distinct())
                    {
                        if (!conflictsByEntryKey.TryGetValue(key, out var list1))
                        {
                            list1 = [];
                            conflictsByEntryKey[key] = list1;
                        }
                        list1.Add(c);
                    }
                }

                var classDaysByClassId = systemState.ActiveClassSchedules
                    .GroupBy(s => s.ClassId)
                    .ToDictionary(g => g.Key, g => g.Select(x => x.DayOfWeek).Distinct().ToList());

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

                var classScheduleItems = rawSchedules.Select(s =>
                {
                    teacherByClass.TryGetValue(s.classId, out var tInfo);
                    var entryKey = $"cs-{s.id}";
                    conflictsByEntryKey.TryGetValue(entryKey, out var schedConflicts);
                    var msgList = schedConflicts?.Select(c => c.Message).Distinct().ToList() ?? [];
                    var classDays = classDaysByClassId.TryGetValue(s.classId, out var dl) ? dl : [s.dayOfWeek];
                    var cStats = AdminCoreHelpers.CalculateSessionStats(classDays, s.startDate, s.endDate);

                    return new
                    {
                        entryKey,
                        sourceType = "class",
                        id = s.id,
                        classId = s.classId,
                        classCode = s.classCode,
                        className = s.className,
                        classStatus = s.classStatus,
                        startDate = (DateTime?)s.startDate,
                        endDate = (DateTime?)s.endDate,
                        classroomId = s.classroomId,
                        classroomName = s.classroomName,
                        timeSlotId = s.timeSlotId,
                        dayOfWeek = s.dayOfWeek,
                        dayLabel = AdminCoreHelpers.FormatDayFull(s.dayOfWeek),
                        teacherId = tInfo?.TeacherId,
                        teacherName = tInfo?.TeacherName ?? "Chưa phân công",
                        courseTitle = (string?)null,
                        scheduleDays = (string?)null,
                        scheduleDaysLabel = (string?)null,
                        sessionsPerWeek = cStats.SessionsPerWeek,
                        totalWeeks = cStats.TotalWeeks,
                        totalSessions = cStats.TotalSessions,
                        hasConflict = msgList.Count > 0,
                        conflictTypes = schedConflicts?.Select(c => c.ConflictType).Distinct().ToList() ?? [],
                        conflictMessages = msgList
                    };
                }).ToList();

                var schedules = classScheduleItems
                    .OrderBy(s => s.dayOfWeek)
                    .ThenBy(s => s.classCode)
                    .ToList();

                var activeRoomClassesMap = systemState.ActiveClassSchedules
                    .Select(s => new { s.ClassroomId, s.ClassCode })
                    .GroupBy(s => s.ClassroomId)
                    .ToDictionary(g => g.Key, g => g.Select(x => x.ClassCode).Distinct().OrderBy(c => c).ToList());

                var roomSummaries = Array.Empty<object>();

                var visibleEntryKeys = schedules.Select(s => s.entryKey).ToHashSet();
                var filterActive = (classroomId.HasValue && classroomId.Value > 0) || (teacherId.HasValue && teacherId.Value > 0);
                var conflictsPayload = systemConflicts
                    .Where(c => !filterActive
                        || visibleEntryKeys.Contains(c.EntryKey)
                        || visibleEntryKeys.Contains(c.OtherEntryKey))
                    .Select(c => new
                    {
                        entryKey = c.EntryKey,
                        otherEntryKey = c.OtherEntryKey,
                        sourceType = c.SourceType,
                        otherSourceType = c.OtherSourceType,
                        scheduleId = c.ScheduleId,
                        otherScheduleId = c.OtherScheduleId,
                        conflictType = c.ConflictType,
                        classId = c.ClassId,
                        classCode = c.CodeLabel,
                        className = c.TitleLabel,
                        otherClassId = c.OtherClassId,
                        otherClassCode = c.OtherCodeLabel,
                        otherClassName = c.OtherTitleLabel,
                        teacherId = c.TeacherId,
                        teacherName = c.TeacherName,
                        classroomId = c.ClassroomId,
                        classroomName = c.ClassroomName,
                        otherClassroomId = c.OtherClassroomId,
                        dayOfWeek = c.DayOfWeek,
                        dayLabel = AdminCoreHelpers.FormatDayFull(c.DayOfWeek),
                        timeSlotId = c.TimeSlotId,
                        timeSlotName = c.TimeSlotName,
                        timeRange = c.TimeRange,
                        message = c.Message
                    })
                    .ToList();

                if (!lookups)
                {
                    return Ok(new { success = true, timeSlots, schedules, roomSummaries, conflicts = conflictsPayload });
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
                        maxStudents = cc.MaxStudents,
                        startDate = cc.StartDate,
                        endDate = cc.EndDate,
                        teacherId = tInfo?.TeacherId,
                        teacherName = tInfo?.TeacherName ?? "Chưa phân công"
                    };
                }).ToList();

                var teachers = await (
                    from t in _context.Teachers.AsNoTracking()
                    join u in _context.Users.AsNoTracking() on t.UserId equals u.Id
                    where t.IsVerified && u.Role == "teacher" && u.IsActive
                    orderby u.FullName
                    select new
                    {
                        id = t.Id,
                        fullName = u.FullName,
                        qualification = t.Qualification
                    }
                ).ToListAsync(cancellationToken);

                var teacherNameByIdLookup = teachers.ToDictionary(t => t.id, t => t.fullName);

                var allClassrooms = await _context.Classrooms
                    .AsNoTracking()
                    .OrderBy(r => r.Name)
                    .Select(r => new
                    {
                        id = r.Id,
                        name = r.Name,
                        location = r.Location,
                        capacity = r.Capacity,
                        description = r.Description,
                        isActive = r.IsActive
                    })
                    .ToListAsync(cancellationToken);

                var roomOccupiedBySchedule = await (
                    from cs in _context.ClassSchedules.AsNoTracking()
                    join cc in _context.CourseClasses.AsNoTracking() on cs.ClassId equals cc.Id
                    where cc.Status == "upcoming" || cc.Status == "ongoing"
                    select new { cs.ClassroomId, cc.ClassCode }
                ).ToListAsync(cancellationToken);

                var occupiedByRoom = roomOccupiedBySchedule
                    .GroupBy(x => x.ClassroomId)
                    .ToDictionary(g => g.Key, g => g.Select(x => x.ClassCode).Distinct().OrderBy(c => c).ToList());

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
                    .Select(r =>
                    {
                        var occCodes = occupiedByRoom.TryGetValue(r.id, out var codes) ? codes : new List<string>();
                        return new
                        {
                            r.id,
                            r.name,
                            r.location,
                            r.capacity,
                            r.description,
                            defaultTeacherId = (int?)null,
                            defaultTeacherName = (string?)null,
                            defaultCourseId = (int?)null,
                            scheduleDays = (string?)null,
                            defaultTimeSlotId = (int?)null,
                            openDate = (DateTime?)null,
                            closeDate = (DateTime?)null,
                            isOccupied = occCodes.Count > 0,
                            occupiedClassCodes = occCodes
                        };
                    })
                    .ToList();

                return Ok(new
                {
                    success = true,
                    timeSlots,
                    schedules,
                    roomSummaries,
                    conflicts = conflictsPayload,
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
                _logger.LogWarning(ex, "Không thể đồng bộ lớp hết hạn trước khi xếp lịch.");
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

                var requestedTeacherId = dto.TeacherId.HasValue && dto.TeacherId.Value > 0 ? dto.TeacherId.Value : (int?)null;

                var lockTeacherIds = assignedTeachers.Select(t => t.TeacherId).ToList();
                if (requestedTeacherId.HasValue)
                {
                    lockTeacherIds.Add(requestedTeacherId.Value);
                }

                var distinctLockTeacherIds = lockTeacherIds.Distinct().OrderBy(id => id).ToList();
                foreach (var tid in distinctLockTeacherIds)
                {
                    long teacherLockKey = 300_000_000L + tid;
                    await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({teacherLockKey})", cancellationToken);
                }

                // Khóa chia sẻ trên TimeSlots (700M) để tuần tự hóa với UpdateTimeSlot mà không chặn các request Create chạy song song
                await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock_shared({TimeSlotLockKey})", cancellationToken);

                List<int> teacherIds;
                Dictionary<int, string> teacherNameByIdOverride;

                if (requestedTeacherId.HasValue)
                {
                    var validTeacher = await LoadVerifiedActiveTeacherAsync(requestedTeacherId.Value, cancellationToken);
                    if (validTeacher == null)
                    {
                        await transaction.RollbackAsync(CancellationToken.None);
                        return this.ApiBadRequest("Giảng viên được chọn không tồn tại, chưa xác minh hoặc đang bị khóa.");
                    }

                    // Nếu thay đổi giảng viên chính khác, cần kiểm tra xung đột cho GV mới trên cả lịch cũ của lớp lẫn lịch mới
                    var effectiveTeacherIds = assignedTeachers
                        .Where(t => t.Role != "main")
                        .Select(t => t.TeacherId)
                        .Append(requestedTeacherId.Value)
                        .Distinct()
                        .ToList();
                    teacherIds = effectiveTeacherIds;
                    teacherNameByIdOverride = assignedTeachers
                        .Where(t => t.Role != "main")
                        .ToDictionary(t => t.TeacherId, t => t.TeacherName);
                    teacherNameByIdOverride[requestedTeacherId.Value] = validTeacher.FullName;
                }
                else
                {
                    teacherIds = assignedTeachers.Select(t => t.TeacherId).Distinct().OrderBy(id => id).ToList();
                    teacherNameByIdOverride = assignedTeachers
                        .GroupBy(t => t.TeacherId)
                        .ToDictionary(g => g.Key, g => g.First().TeacherName);
                }

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

                // Quy tắc phòng-ca học: Một phòng có thể mở nhiều lớp đang hoạt động,
                // nhưng trong cùng khoảng ngày [StartDate, EndDate] giao nhau, mỗi lớp bắt buộc phải học ở ca học khác nhau (không trùng TimeSlotId và không giao giờ).
                var roomSlotConflict = await (
                    from cs in _context.ClassSchedules.AsNoTracking()
                    join cc in _context.CourseClasses.AsNoTracking() on cs.ClassId equals cc.Id
                    join ts in _context.TimeSlots.AsNoTracking() on cs.TimeSlotId equals ts.Id
                    where cs.ClassroomId == classroomId
                          && cs.ClassId != classId
                          && (cc.Status == "upcoming" || cc.Status == "ongoing")
                          && targetStart <= cc.EndDate
                          && cc.StartDate <= targetEnd
                          && (cs.TimeSlotId == dto.TimeSlotId
                              || (ts.StartTime < slotEnd && slotStart < ts.EndTime))
                    select new { cc.ClassCode, cc.ClassName, SlotName = ts.Name, ts.StartTime, ts.EndTime }
                ).FirstOrDefaultAsync(cancellationToken);

                if (roomSlotConflict != null)
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    var hitRange = AdminCoreHelpers.FormatTimeRange(roomSlotConflict.StartTime, roomSlotConflict.EndTime);
                    return this.ApiBadRequest(
                        $"Trùng ca học trong cùng phòng: Phòng {classroom.Name} đã có lớp {roomSlotConflict.ClassCode} ({roomSlotConflict.ClassName}) đăng ký ở ca {roomSlotConflict.SlotName} ({hitRange}). Mỗi lớp trong cùng một phòng bắt buộc phải chọn ca học khác nhau.");
                }

                // Không cho cùng 1 lớp đăng ký 2 ca học giao giờ khác nhau trong cùng 1 phòng
                var sameClassRoomSlotOverlap = await (
                    from cs in _context.ClassSchedules.AsNoTracking()
                    join ts in _context.TimeSlots.AsNoTracking() on cs.TimeSlotId equals ts.Id
                    where cs.ClassroomId == classroomId
                          && cs.ClassId == classId
                          && cs.TimeSlotId != dto.TimeSlotId
                          && ts.StartTime < slotEnd
                          && slotStart < ts.EndTime
                    select new { SlotName = ts.Name, ts.StartTime, ts.EndTime }
                ).FirstOrDefaultAsync(cancellationToken);

                if (sameClassRoomSlotOverlap != null)
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    var hitRange = AdminCoreHelpers.FormatTimeRange(sameClassRoomSlotOverlap.StartTime, sameClassRoomSlotOverlap.EndTime);
                    return this.ApiBadRequest(
                        $"Chồng chéo ca học trong cùng phòng: Lớp {targetClass.ClassCode} đã đăng ký ca {sameClassRoomSlotOverlap.SlotName} ({hitRange}) tại phòng {classroom.Name}, giao giờ với ca {timeLabel}.");
                }

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

                // 2. Trùng lịch cho cả giảng viên chính và trợ giảng theo khoảng giờ & khoảng ngày giao nhau
                var teacherNameById = teacherNameByIdOverride;

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

                if (requestedTeacherId.HasValue)
                {
                    var existingTeacherConflict = await CheckTeacherConflictAgainstClassSchedulesAsync(
                        classId,
                        requestedTeacherId.Value,
                        teacherNameById.GetValueOrDefault(requestedTeacherId.Value, $"GV #{requestedTeacherId.Value}"),
                        targetStart,
                        targetEnd,
                        excludeScheduleId: null,
                        cancellationToken);
                    if (existingTeacherConflict != null)
                    {
                        await transaction.RollbackAsync(CancellationToken.None);
                        return this.ApiBadRequest(existingTeacherConflict);
                    }

                    await ApplyMainTeacherAsync(classId, requestedTeacherId.Value, now, cancellationToken);
                }

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

        [HttpPost("/Admin/api/Schedules/CheckConflicts")]
        public async Task<IActionResult> CheckConflicts([FromBody] ScheduleCreateDto? dto, CancellationToken cancellationToken)
        {
            if (dto == null || dto.ClassId <= 0 || dto.ClassroomId <= 0 || dto.TimeSlotId <= 0 || dto.DaysOfWeek == null || dto.DaysOfWeek.Count == 0)
            {
                return Ok(new
                {
                    success = true,
                    hasConflicts = false,
                    conflicts = Array.Empty<object>(),
                    teacherStatuses = Array.Empty<object>()
                });
            }

            try
            {
                var days = dto.DaysOfWeek.Where(d => d >= 1 && d <= 7).Distinct().OrderBy(d => d).ToList();
                var result = await InspectCandidateAndTeachersAsync(
                    dto.ClassId,
                    dto.ClassroomId,
                    dto.TimeSlotId,
                    days,
                    dto.TeacherId,
                    dto.ExcludeScheduleId,
                    cancellationToken);

                return Ok(new
                {
                    success = true,
                    hasConflicts = result.Conflicts.Count > 0,
                    conflicts = result.Conflicts,
                    teacherStatuses = result.TeacherStatuses
                });
            }
            catch (OperationCanceledException)
            {
                return this.ApiCancelled();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể kiểm tra trùng lịch.");
                return this.ApiServerError("Không thể kiểm tra trùng lịch.");
            }
        }

        [HttpPost("/Admin/api/Schedules/CheckRoomConflicts")]
        public async Task<IActionResult> CheckRoomConflicts([FromBody] RoomConflictCheckDto? dto, CancellationToken cancellationToken)
        {
            if (dto == null || dto.ClassroomId <= 0)
            {
                return Ok(new
                {
                    success = true,
                    hasConflicts = false,
                    conflicts = Array.Empty<string>(),
                    teacherStatuses = Array.Empty<object>(),
                    dayOptionStatuses = Array.Empty<object>()
                });
            }

            try
            {
                var systemState = await AdminCoreHelpers.DetectSystemScheduleStateAsync(_context, cancellationToken);
                var otherRooms = systemState.ConfiguredRooms
                    .Where(r => r.ClassroomId != dto.ClassroomId)
                    .ToList();

                var classIds = systemState.ActiveClassSchedules.Select(s => s.ClassId).Distinct().ToList();
                var classTeachers = classIds.Count == 0
                    ? []
                    : await (
                        from ct in _context.ClassTeachers.AsNoTracking()
                        join t in _context.Teachers.AsNoTracking() on ct.TeacherId equals t.Id
                        join u in _context.Users.AsNoTracking() on t.UserId equals u.Id
                        where classIds.Contains(ct.ClassId)
                        select new { ct.ClassId, TeacherId = t.Id, TeacherName = u.FullName }
                    ).ToListAsync(cancellationToken);

                var teacherIdsByClass = classTeachers
                    .GroupBy(x => x.ClassId)
                    .ToDictionary(g => g.Key, g => g.Select(x => x.TeacherId).Distinct().ToList());

                var verifiedTeachers = await (
                    from t in _context.Teachers.AsNoTracking()
                    join u in _context.Users.AsNoTracking() on t.UserId equals u.Id
                    where t.IsVerified && u.Role == "teacher" && u.IsActive
                    orderby u.FullName
                    select new SimpleTeacherInfo(t.Id, u.FullName, t.Qualification)
                ).ToListAsync(cancellationToken);

                var teacherNameMap = verifiedTeachers.ToDictionary(t => t.Id, t => t.FullName);

                var targetSlot = dto.TimeSlotId.HasValue && dto.TimeSlotId.Value > 0
                    ? await _context.TimeSlots.AsNoTracking()
                        .Where(ts => ts.Id == dto.TimeSlotId.Value)
                        .Select(ts => new { ts.Id, ts.Name, ts.StartTime, ts.EndTime })
                        .FirstOrDefaultAsync(cancellationToken)
                    : null;

                var candStart = dto.OpenDate?.Date ?? DateTime.MinValue.Date;
                var candEnd = dto.CloseDate?.Date ?? DateTime.MaxValue.Date;

                List<string> EvaluateCandidate(int? candTeacherId, string? candScheduleDays)
                {
                    var issues = new List<string>();
                    var days = AdminCoreHelpers.ParseScheduleDays(candScheduleDays);
                    if (targetSlot == null || days.Length == 0)
                    {
                        return issues;
                    }

                    // 1. Kiểm tra trùng GV với các phòng khác
                    if (candTeacherId.HasValue && candTeacherId.Value > 0)
                    {
                        var tName = teacherNameMap.GetValueOrDefault(candTeacherId.Value, $"GV #{candTeacherId.Value}");
                        foreach (var other in otherRooms)
                        {
                            if (!other.DefaultTeacherId.HasValue || other.DefaultTeacherId.Value != candTeacherId.Value)
                            {
                                continue;
                            }
                            if (!(targetSlot.StartTime < other.EndTime && other.StartTime < targetSlot.EndTime))
                            {
                                continue;
                            }
                            var os = (other.OpenDate ?? DateTime.MinValue).Date;
                            var oe = (other.CloseDate ?? DateTime.MaxValue).Date;
                            if (!(candStart <= oe && os <= candEnd))
                            {
                                continue;
                            }

                            var commonDays = days.Intersect(other.Days).OrderBy(d => d).ToList();
                            if (commonDays.Count > 0)
                            {
                                var daySummary = string.Join(", ", commonDays.Select(AdminCoreHelpers.FormatDayShort));
                                issues.Add($"Trùng lịch GV {tName} với {other.ClassroomName} ({daySummary} • {other.SlotName} {other.TimeRange}).");
                            }
                        }
                    }

                    // 2. Kiểm tra trùng phòng hoặc trùng GV với các lớp (ClassSchedules) đang hoạt động
                    foreach (var d in days)
                    {
                        foreach (var cs in systemState.ActiveClassSchedules)
                        {
                            if (cs.DayOfWeek != d) continue;
                            if (!(targetSlot.StartTime < cs.EndTime && cs.StartTime < targetSlot.EndTime)) continue;
                            if (!(candStart <= cs.EndDate.Date && cs.StartDate.Date <= candEnd)) continue;

                            // Bỏ qua nếu chính lớp thuộc phòng đó đang học đúng ca mặc định của phòng
                            if (cs.ClassroomId == dto.ClassroomId && cs.TimeSlotId == targetSlot.Id) continue;

                            var dayLabel = AdminCoreHelpers.FormatDayShort(d);
                            var csRange = AdminCoreHelpers.FormatTimeRange(cs.StartTime, cs.EndTime);

                            if (cs.ClassroomId == dto.ClassroomId)
                            {
                                issues.Add($"Trùng giờ tại chính phòng này với lớp {cs.ClassCode} ({dayLabel} • {cs.SlotName} {csRange}).");
                            }

                            if (candTeacherId.HasValue && candTeacherId.Value > 0
                                && teacherIdsByClass.TryGetValue(cs.ClassId, out var cTids)
                                && cTids.Contains(candTeacherId.Value))
                            {
                                var tName = teacherNameMap.GetValueOrDefault(candTeacherId.Value, $"GV #{candTeacherId.Value}");
                                issues.Add($"Trùng GV {tName} với lớp {cs.ClassCode} ({dayLabel} • {cs.SlotName} {csRange}).");
                            }
                        }
                    }

                    return issues.Distinct().ToList();
                }

                var currentConflicts = EvaluateCandidate(dto.TeacherId, dto.ScheduleDays);

                var teacherStatuses = verifiedTeachers.Select(t =>
                {
                    var errs = EvaluateCandidate(t.Id, dto.ScheduleDays);
                    return (object)new
                    {
                        id = t.Id,
                        fullName = t.FullName,
                        qualification = t.Qualification,
                        isAvailable = errs.Count == 0,
                        conflictReason = errs.FirstOrDefault()
                    };
                }).ToList();

                var dayOptionStatuses = new[] { "t246", "t357", "all" }
                    .Select(preset =>
                    {
                        var errs = EvaluateCandidate(dto.TeacherId, preset);
                        return (object)new
                        {
                            value = preset,
                            label = AdminCoreHelpers.FormatScheduleDaysLabel(preset),
                            isAvailable = errs.Count == 0,
                            conflictReason = errs.FirstOrDefault()
                        };
                    }).ToList();

                return Ok(new
                {
                    success = true,
                    hasConflicts = currentConflicts.Count > 0,
                    conflicts = currentConflicts,
                    teacherStatuses,
                    dayOptionStatuses
                });
            }
            catch (OperationCanceledException)
            {
                return this.ApiCancelled();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể kiểm tra trùng lịch phòng {ClassroomId}.", dto.ClassroomId);
                return this.ApiServerError("Không thể kiểm tra trùng lịch phòng.");
            }
        }

        [HttpPost("/Admin/api/Schedules/Update/{id}")]
        [HttpPut("/Admin/api/Schedules/Update/{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] ScheduleUpdateDto? dto, CancellationToken cancellationToken)
        {
            if (id <= 0 || dto == null)
            {
                return this.ApiBadRequest("Dữ liệu cập nhật lịch học không hợp lệ.");
            }

            if (dto.ClassroomId <= 0 || dto.TimeSlotId <= 0 || dto.DayOfWeek < 1 || dto.DayOfWeek > 7)
            {
                return this.ApiBadRequest("Vui lòng chọn đầy đủ phòng học, ca học và thứ hợp lệ (Thứ 2 - Chủ Nhật).");
            }

            int classId;
            int oldRoomId;
            try
            {
                var existingMeta = await _context.ClassSchedules
                    .AsNoTracking()
                    .Where(s => s.Id == id)
                    .Select(s => new { s.ClassId, s.ClassroomId })
                    .FirstOrDefaultAsync(cancellationToken);
                if (existingMeta == null)
                {
                    return this.ApiNotFound("Không tìm thấy lịch học cần chỉnh sửa.");
                }

                classId = existingMeta.ClassId;
                oldRoomId = existingMeta.ClassroomId;
            }
            catch (OperationCanceledException)
            {
                return this.ApiCancelled();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể tra cứu lịch học {ScheduleId}.", id);
                return this.ApiServerError("Không thể cập nhật lịch học.");
            }

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                // Khóa theo thứ tự chuẩn: Lớp (100M) -> Phòng tăng dần (200M) -> Giảng viên tăng dần (300M) -> TimeSlot shared (700M)
                await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({100_000_000L + classId})", cancellationToken);

                foreach (var rid in new[] { oldRoomId, dto.ClassroomId }.Distinct().OrderBy(x => x))
                {
                    await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({200_000_000L + rid})", cancellationToken);
                }

                var assignedTeachers = await (
                    from ct in _context.ClassTeachers.AsNoTracking()
                    join t in _context.Teachers.AsNoTracking() on ct.TeacherId equals t.Id
                    join u in _context.Users.AsNoTracking() on t.UserId equals u.Id
                    where ct.ClassId == classId
                    select new { TeacherId = t.Id, TeacherName = u.FullName, ct.Role }
                ).ToListAsync(cancellationToken);

                var requestedTeacherId = dto.TeacherId.HasValue && dto.TeacherId.Value > 0 ? dto.TeacherId.Value : (int?)null;
                var lockTeachers = assignedTeachers.Select(x => x.TeacherId).ToList();
                if (requestedTeacherId.HasValue)
                {
                    lockTeachers.Add(requestedTeacherId.Value);
                }
                foreach (var tid in lockTeachers.Distinct().OrderBy(x => x))
                {
                    await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({300_000_000L + tid})", cancellationToken);
                }

                await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock_shared({TimeSlotLockKey})", cancellationToken);

                var schedule = await _context.ClassSchedules.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
                if (schedule == null)
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiNotFound("Không tìm thấy lịch học cần chỉnh sửa.");
                }

                var targetClass = await _context.CourseClasses
                    .AsNoTracking()
                    .Where(c => c.Id == classId)
                    .Select(c => new { c.ClassCode, c.ClassName, c.Status, c.MaxStudents, c.StartDate, c.EndDate })
                    .FirstOrDefaultAsync(cancellationToken);
                if (targetClass == null || (targetClass.Status != "upcoming" && targetClass.Status != "ongoing"))
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiBadRequest("Chỉ chỉnh sửa được lịch cho lớp ở trạng thái upcoming hoặc ongoing.");
                }

                var classroom = await _context.Classrooms
                    .AsNoTracking()
                    .Where(r => r.Id == dto.ClassroomId)
                    .Select(r => new { r.Name, r.Capacity, r.IsActive })
                    .FirstOrDefaultAsync(cancellationToken);
                if (classroom == null || !classroom.IsActive)
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiBadRequest("Phòng học không tồn tại hoặc đang tạm ngưng sử dụng.");
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

                string? reassignedTeacherName = null;
                if (requestedTeacherId.HasValue)
                {
                    var validTeacher = await LoadVerifiedActiveTeacherAsync(requestedTeacherId.Value, cancellationToken);
                    if (validTeacher == null)
                    {
                        await transaction.RollbackAsync(CancellationToken.None);
                        return this.ApiBadRequest("Giảng viên được chọn không tồn tại, chưa xác minh hoặc đang bị khóa.");
                    }
                    reassignedTeacherName = validTeacher.FullName;
                }

                var inspection = await InspectCandidateAndTeachersAsync(
                    classId,
                    dto.ClassroomId,
                    dto.TimeSlotId,
                    new List<int> { dto.DayOfWeek },
                    requestedTeacherId,
                    excludeScheduleId: id,
                    cancellationToken);

                if (inspection.Conflicts.Count > 0)
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiBadRequest(inspection.Conflicts[0].Message);
                }

                var now = DateTime.UtcNow;
                if (requestedTeacherId.HasValue)
                {
                    var newTeacherConflictOnOtherSessions = await CheckTeacherConflictAgainstClassSchedulesAsync(
                        classId,
                        requestedTeacherId.Value,
                        reassignedTeacherName!,
                        targetClass.StartDate,
                        targetClass.EndDate,
                        excludeScheduleId: id,
                        cancellationToken);
                    if (newTeacherConflictOnOtherSessions != null)
                    {
                        await transaction.RollbackAsync(CancellationToken.None);
                        return this.ApiBadRequest(newTeacherConflictOnOtherSessions);
                    }

                    await ApplyMainTeacherAsync(classId, requestedTeacherId.Value, now, cancellationToken);
                }

                await _context.ClassSchedules
                    .Where(s => s.Id == id)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(s => s.ClassroomId, dto.ClassroomId)
                        .SetProperty(s => s.TimeSlotId, dto.TimeSlotId)
                        .SetProperty(s => s.DayOfWeek, dto.DayOfWeek),
                        cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                var msg = reassignedTeacherName != null
                    ? $"Đã cập nhật lịch học lớp {targetClass.ClassCode} ({AdminCoreHelpers.FormatDayFull(dto.DayOfWeek)} - {timeSlot.Name}, phòng {classroom.Name}) và phân công GV {reassignedTeacherName}."
                    : $"Đã cập nhật lịch học lớp {targetClass.ClassCode} ({AdminCoreHelpers.FormatDayFull(dto.DayOfWeek)} - {timeSlot.Name}, phòng {classroom.Name}).";

                return Ok(new { success = true, message = msg });
            }
            catch (OperationCanceledException)
            {
                return this.ApiCancelled();
            }
            catch (PostgresException pgEx)
            {
                _logger.LogError(pgEx, "Lỗi PostgreSQL khi cập nhật lịch học {ScheduleId}: {Detail}", id, pgEx.Detail);
                await transaction.RollbackAsync(CancellationToken.None);
                return this.ApiBadRequest($"Lỗi dữ liệu ({pgEx.SqlState}): {pgEx.MessageText}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể cập nhật lịch học {ScheduleId}.", id);
                await transaction.RollbackAsync(CancellationToken.None);
                return this.ApiServerError($"Không thể cập nhật lịch học: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        [HttpPost("/Admin/api/Schedules/AutoPreview")]
        public async Task<IActionResult> AutoPreview([FromBody] AutoPreviewDto? dto, CancellationToken cancellationToken)
        {
            if (dto == null || dto.ClassId <= 0)
            {
                return this.ApiBadRequest("Vui lòng chọn lớp học cần xếp lịch tự động.");
            }

            var sessionsPerWeek = Math.Clamp(dto.SessionsPerWeek <= 0 ? 2 : dto.SessionsPerWeek, 1, 7);

            try
            {
                await AdminCoreHelpers.SyncExpiredClassesAsync(_context, cancellationToken);

                var targetClass = await _context.CourseClasses
                    .AsNoTracking()
                    .Where(c => c.Id == dto.ClassId)
                    .Select(c => new { c.Id, c.ClassCode, c.ClassName, c.Status, c.MaxStudents, c.StartDate, c.EndDate })
                    .FirstOrDefaultAsync(cancellationToken);

                if (targetClass == null || (targetClass.Status != "upcoming" && targetClass.Status != "ongoing"))
                {
                    return this.ApiBadRequest("Lớp học không tồn tại hoặc không ở trạng thái đang hoạt động.");
                }

                var rooms = await _context.Classrooms
                    .AsNoTracking()
                    .Where(r => r.IsActive && r.Capacity >= targetClass.MaxStudents)
                    .OrderByDescending(r => dto.PreferredClassroomId.HasValue && r.Id == dto.PreferredClassroomId.Value)
                    .ThenBy(r => r.Capacity)
                    .ThenBy(r => r.Name)
                    .Select(r => new { r.Id, r.Name, r.Capacity })
                    .ToListAsync(cancellationToken);

                if (rooms.Count == 0)
                {
                    return this.ApiBadRequest($"Không có phòng học nào đang hoạt động đủ sức chứa (>= {targetClass.MaxStudents} học viên) cho lớp {targetClass.ClassCode}.");
                }

                var slots = await _context.TimeSlots
                    .AsNoTracking()
                    .Where(ts => ts.IsActive)
                    .OrderByDescending(ts => dto.PreferredTimeSlotId.HasValue && ts.Id == dto.PreferredTimeSlotId.Value)
                    .ThenBy(ts => ts.StartTime)
                    .Select(ts => new { ts.Id, ts.Name, ts.StartTime, ts.EndTime })
                    .ToListAsync(cancellationToken);

                if (slots.Count == 0)
                {
                    return this.ApiBadRequest("Chưa có ca học nào đang hoạt động để xếp lịch.");
                }

                var verifiedTeachers = await (
                    from t in _context.Teachers.AsNoTracking()
                    join u in _context.Users.AsNoTracking() on t.UserId equals u.Id
                    where t.IsVerified && u.Role == "teacher" && u.IsActive
                    orderby u.FullName
                    select new SimpleTeacherInfo(t.Id, u.FullName, t.Qualification)
                ).ToListAsync(cancellationToken);

                var currentMainTeacher = await (
                    from ct in _context.ClassTeachers.AsNoTracking()
                    join t in _context.Teachers.AsNoTracking() on ct.TeacherId equals t.Id
                    join u in _context.Users.AsNoTracking() on t.UserId equals u.Id
                    where ct.ClassId == dto.ClassId && ct.Role == "main"
                    select new { TeacherId = t.Id, TeacherName = u.FullName }
                ).FirstOrDefaultAsync(cancellationToken);

                var assistantTeacherIds = await _context.ClassTeachers
                    .AsNoTracking()
                    .Where(ct => ct.ClassId == dto.ClassId && ct.Role != "main")
                    .Select(ct => ct.TeacherId)
                    .ToListAsync(cancellationToken);

                var effectiveTeacherId = dto.TeacherId.HasValue && dto.TeacherId.Value > 0
                    ? dto.TeacherId.Value
                    : currentMainTeacher?.TeacherId;

                var effectiveTeacherName = effectiveTeacherId.HasValue
                    ? (verifiedTeachers.FirstOrDefault(t => t.Id == effectiveTeacherId.Value)?.FullName
                        ?? currentMainTeacher?.TeacherName
                        ?? $"GV #{effectiveTeacherId.Value}")
                    : "Chưa phân công";

                var allActiveSchedules = await (
                    from cs in _context.ClassSchedules.AsNoTracking()
                    join cc in _context.CourseClasses.AsNoTracking() on cs.ClassId equals cc.Id
                    join r in _context.Classrooms.AsNoTracking() on cs.ClassroomId equals r.Id
                    join ts in _context.TimeSlots.AsNoTracking() on cs.TimeSlotId equals ts.Id
                    where cc.Status == "upcoming" || cc.Status == "ongoing"
                    select new ActiveScheduleSnapshot
                    {
                        ScheduleId = cs.Id,
                        ClassId = cc.Id,
                        ClassCode = cc.ClassCode,
                        ClassName = cc.ClassName,
                        StartDate = cc.StartDate,
                        EndDate = cc.EndDate,
                        ClassroomId = r.Id,
                        ClassroomName = r.Name,
                        TimeSlotId = ts.Id,
                        SlotName = ts.Name,
                        StartTime = ts.StartTime,
                        EndTime = ts.EndTime,
                        DayOfWeek = cs.DayOfWeek
                    }
                ).ToListAsync(cancellationToken);

                var allActiveClassTeachers = await (
                    from ct in _context.ClassTeachers.AsNoTracking()
                    join cc in _context.CourseClasses.AsNoTracking() on ct.ClassId equals cc.Id
                    join t in _context.Teachers.AsNoTracking() on ct.TeacherId equals t.Id
                    join u in _context.Users.AsNoTracking() on t.UserId equals u.Id
                    where cc.Status == "upcoming" || cc.Status == "ongoing"
                    select new { ct.ClassId, ct.TeacherId, TeacherName = u.FullName, ct.Role }
                ).ToListAsync(cancellationToken);

                var teachersByClassId = allActiveClassTeachers
                    .GroupBy(x => x.ClassId)
                    .ToDictionary(g => g.Key, g => g.Select(x => x.TeacherId).Distinct().ToList());

                var existingClassSchedules = dto.ReplaceExisting
                    ? new List<ActiveScheduleSnapshot>()
                    : allActiveSchedules.Where(s => s.ClassId == dto.ClassId).ToList();

                var otherSchedules = allActiveSchedules.Where(s => s.ClassId != dto.ClassId).ToList();

                var defaultBalancedDays = new[] { 1, 3, 5, 2, 4, 6, 7 };
                var candidateDays = dto.PreferredDays != null && dto.PreferredDays.Count > 0
                    ? dto.PreferredDays.Where(d => d >= 1 && d <= 7).Distinct().Concat(defaultBalancedDays).Distinct().ToList()
                    : defaultBalancedDays.ToList();

                var proposed = new List<AutoProposedSlot>();
                var usedDays = existingClassSchedules.Select(s => s.DayOfWeek).ToHashSet();

                // Nếu người dùng ép cứng ca / thứ ưu tiên, ưu tiên giữ khung mong muốn trước để dễ nhìn thấy cảnh báo trùng (nếu có) và gợi ý đổi GV
                bool strictPreference = dto.PreferredTimeSlotId.HasValue || (dto.PreferredDays != null && dto.PreferredDays.Count > 0);
                var primarySlots = dto.PreferredTimeSlotId.HasValue
                    ? slots.Where(s => s.Id == dto.PreferredTimeSlotId.Value).ToList()
                    : slots;

                // Pass 1: Tìm tối đa buổi trống hoàn toàn (không trùng lớp, không trùng phòng, không trùng GV)
                foreach (var day in candidateDays)
                {
                    if (proposed.Count >= sessionsPerWeek) break;
                    if (usedDays.Contains(day)) continue;

                    AutoProposedSlot? foundClean = null;
                    foreach (var slot in primarySlots.Count > 0 ? primarySlots : slots)
                    {
                        bool sameClassBusy = existingClassSchedules.Any(s => s.DayOfWeek == day && s.StartTime < slot.EndTime && slot.StartTime < s.EndTime);
                        if (sameClassBusy) continue;

                        foreach (var room in rooms)
                        {
                            // Phòng bận ở ca slot nếu đã có lớp hoạt động khác (giao khoảng ngày) đăng ký cùng ca hoặc giao giờ tại phòng đó; các ca khác vẫn khả dụng.
                            var roomHit = otherSchedules.FirstOrDefault(o =>
                                o.ClassroomId == room.Id
                                && targetClass.StartDate <= o.EndDate && o.StartDate <= targetClass.EndDate
                                && (o.TimeSlotId == slot.Id || (o.StartTime < slot.EndTime && slot.StartTime < o.EndTime)));
                            if (roomHit != null) continue;

                            var sameClassSlotOverlap = existingClassSchedules.Any(s =>
                                s.ClassroomId == room.Id
                                && s.TimeSlotId != slot.Id
                                && s.StartTime < slot.EndTime && slot.StartTime < s.EndTime);
                            if (sameClassSlotOverlap) continue;

                            var teacherHit = FindTeacherConflictInSnapshot(
                                effectiveTeacherId, assistantTeacherIds, targetClass.StartDate, targetClass.EndDate,
                                day, slot.StartTime, slot.EndTime, otherSchedules, teachersByClassId,
                                effectiveTeacherName);

                            if (teacherHit == null)
                            {
                                foundClean = BuildAutoProposedSlot(
                                    day, slot.Id, slot.Name, AdminCoreHelpers.FormatTimeRange(slot.StartTime, slot.EndTime),
                                    slot.StartTime, slot.EndTime,
                                    room.Id, room.Name,
                                    effectiveTeacherId, effectiveTeacherName,
                                    hasConflict: false, conflictType: null, conflictMessage: null,
                                    verifiedTeachers, targetClass.StartDate, targetClass.EndDate,
                                    otherSchedules, teachersByClassId, existingClassSchedules);
                                break;
                            }
                        }

                        if (foundClean != null) break;
                    }

                    // Nếu chưa bật strictPreference mà chưa kiếm đủ với primarySlots, thử tất cả các ca còn lại trong Pass 1b ở cuối
                    if (foundClean != null)
                    {
                        proposed.Add(foundClean);
                        usedDays.Add(day);
                    }
                }

                // Pass 1b: Nếu chọn ca ưu tiên mà một số ngày bị kẹt, kiểm tra tiếp các ca khác không bị trùng
                if (proposed.Count < sessionsPerWeek && !strictPreference)
                {
                    // (Đã phủ hết slots nếu preferredTimeSlotId null; giữ phòng trường hợp mở rộng)
                }

                // Pass 2: Nếu vẫn chưa đủ số buổi (ví dụ GV chính bị trùng hết ở ca đó hoặc lớp đã chọn thứ/ca bị trùng),
                // giữ ca/thứ phù hợp nhất VÀ gắn cờ CẢNH BÁO TRÙNG LỊCH + danh sách giáo viên rảnh để Admin thay thế!
                if (proposed.Count < sessionsPerWeek)
                {
                    var pass2Slots = primarySlots.Count > 0 ? primarySlots : slots;
                    foreach (var day in candidateDays)
                    {
                        if (proposed.Count >= sessionsPerWeek) break;
                        if (usedDays.Contains(day)) continue;

                        foreach (var slot in pass2Slots)
                        {
                            bool sameClassBusy = existingClassSchedules.Any(s => s.DayOfWeek == day && s.StartTime < slot.EndTime && slot.StartTime < s.EndTime);
                            if (sameClassBusy) continue;

                            var freeRoom = rooms.FirstOrDefault(room => !otherSchedules.Any(o =>
                                o.ClassroomId == room.Id
                                && targetClass.StartDate <= o.EndDate && o.StartDate <= targetClass.EndDate
                                && (o.TimeSlotId == slot.Id || (o.StartTime < slot.EndTime && slot.StartTime < o.EndTime))));

                            var chosenRoom = freeRoom ?? rooms[0];
                            string? conflictType = null;
                            string? conflictMsg = null;

                            if (freeRoom == null)
                            {
                                var roomHit = otherSchedules.First(o =>
                                    o.ClassroomId == chosenRoom.Id
                                    && targetClass.StartDate <= o.EndDate && o.StartDate <= targetClass.EndDate
                                    && (o.TimeSlotId == slot.Id || (o.StartTime < slot.EndTime && slot.StartTime < o.EndTime)));
                                conflictType = "room";
                                conflictMsg = $"Trùng ca học trong cùng phòng {chosenRoom.Name}: Đã có lớp {roomHit.ClassCode} đăng ký ca {roomHit.SlotName} ({AdminCoreHelpers.FormatTimeRange(roomHit.StartTime, roomHit.EndTime)}). Mỗi lớp trong cùng phòng bắt buộc phải chọn ca học khác nhau.";
                            }
                            else
                            {
                                var teacherHit = FindTeacherConflictInSnapshot(
                                    effectiveTeacherId, assistantTeacherIds, targetClass.StartDate, targetClass.EndDate,
                                    day, slot.StartTime, slot.EndTime, otherSchedules, teachersByClassId,
                                    effectiveTeacherName);
                                if (teacherHit != null)
                                {
                                    conflictType = "teacher";
                                    conflictMsg = teacherHit;
                                }
                            }

                            var item = BuildAutoProposedSlot(
                                day, slot.Id, slot.Name, AdminCoreHelpers.FormatTimeRange(slot.StartTime, slot.EndTime),
                                slot.StartTime, slot.EndTime,
                                chosenRoom.Id, chosenRoom.Name,
                                effectiveTeacherId, effectiveTeacherName,
                                hasConflict: conflictMsg != null,
                                conflictType: conflictType,
                                conflictMessage: conflictMsg,
                                verifiedTeachers, targetClass.StartDate, targetClass.EndDate,
                                otherSchedules, teachersByClassId, existingClassSchedules);

                            proposed.Add(item);
                            usedDays.Add(day);
                            break;
                        }
                    }
                }

                // Kiểm tra tổng thể xem giảng viên nào có thể dạy TOÀN BỘ lịch đang đề xuất (gồm cả lịch cũ nếu giữ nguyên)
                var allSessionWindows = existingClassSchedules
                    .Select(s => (s.DayOfWeek, s.StartTime, s.EndTime))
                    .Concat(proposed.Select(p => (p.DayOfWeek, p.StartTime, p.EndTime)))
                    .ToList();

                var teacherStatuses = verifiedTeachers.Select(t =>
                {
                    string? busyReason = null;
                    foreach (var w in allSessionWindows)
                    {
                        var hit = otherSchedules.FirstOrDefault(o =>
                            teachersByClassId.TryGetValue(o.ClassId, out var tids) && tids.Contains(t.Id)
                            && o.DayOfWeek == w.DayOfWeek
                            && targetClass.StartDate <= o.EndDate && o.StartDate <= targetClass.EndDate
                            && o.StartTime < w.EndTime && w.StartTime < o.EndTime);
                        if (hit != null)
                        {
                            busyReason = $"Trùng {AdminCoreHelpers.FormatDayFull(w.DayOfWeek)} - lớp {hit.ClassCode} ({hit.SlotName} {AdminCoreHelpers.FormatTimeRange(hit.StartTime, hit.EndTime)})";
                            break;
                        }
                    }
                    return new
                    {
                        id = t.Id,
                        fullName = t.FullName,
                        qualification = t.Qualification,
                        isAvailable = busyReason == null,
                        conflictReason = busyReason
                    };
                }).ToList();

                var warningCount = proposed.Count(p => p.HasConflict);
                var conflictWarnings = proposed.Where(p => p.HasConflict && p.ConflictMessage != null)
                    .Select(p => $"{p.DayLabel} ({p.TimeSlotName}): {p.ConflictMessage}")
                    .ToList();

                return Ok(new
                {
                    success = true,
                    classInfo = new
                    {
                        id = targetClass.Id,
                        classCode = targetClass.ClassCode,
                        className = targetClass.ClassName,
                        teacherId = effectiveTeacherId,
                        teacherName = effectiveTeacherName
                    },
                    hasConflicts = warningCount > 0,
                    warningCount,
                    warnings = conflictWarnings,
                    items = proposed.Select(p => new
                    {
                        dayOfWeek = p.DayOfWeek,
                        dayLabel = p.DayLabel,
                        timeSlotId = p.TimeSlotId,
                        timeSlotName = p.TimeSlotName,
                        timeRange = p.TimeRange,
                        classroomId = p.ClassroomId,
                        classroomName = p.ClassroomName,
                        teacherId = p.TeacherId,
                        teacherName = p.TeacherName,
                        hasConflict = p.HasConflict,
                        conflictType = p.ConflictType,
                        conflictMessage = p.ConflictMessage,
                        alternativeTeachers = p.AlternativeTeachers
                    }).ToList(),
                    teacherStatuses
                });
            }
            catch (OperationCanceledException)
            {
                return this.ApiCancelled();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể phân tích xếp lịch tự động cho lớp {ClassId}.", dto.ClassId);
                return this.ApiServerError("Không thể chạy phân tích xếp lịch tự động.");
            }
        }

        [HttpPost("/Admin/api/Schedules/AutoApply")]
        public async Task<IActionResult> AutoApply([FromBody] AutoApplyDto? dto, CancellationToken cancellationToken)
        {
            if (dto == null || dto.ClassId <= 0 || dto.Items == null || dto.Items.Count == 0)
            {
                return this.ApiBadRequest("Vui lòng chọn lớp học và danh sách buổi học hợp lệ để lưu.");
            }

            if (dto.Items.Any(i => i.ClassroomId <= 0 || i.TimeSlotId <= 0 || i.DayOfWeek < 1 || i.DayOfWeek > 7))
            {
                return this.ApiBadRequest("Thông tin phòng học, ca học hoặc thứ trong tuần không hợp lệ.");
            }

            if (dto.Items.Select(i => i.DayOfWeek).Distinct().Count() != dto.Items.Count)
            {
                return this.ApiBadRequest("Danh sách xếp lịch tự động đang có hai buổi trùng cùng một thứ.");
            }

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({100_000_000L + dto.ClassId})", cancellationToken);

                foreach (var rid in dto.Items.Select(i => i.ClassroomId).Distinct().OrderBy(x => x))
                {
                    await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({200_000_000L + rid})", cancellationToken);
                }

                var assignedTeachers = await (
                    from ct in _context.ClassTeachers.AsNoTracking()
                    join t in _context.Teachers.AsNoTracking() on ct.TeacherId equals t.Id
                    join u in _context.Users.AsNoTracking() on t.UserId equals u.Id
                    where ct.ClassId == dto.ClassId
                    select new { TeacherId = t.Id, TeacherName = u.FullName, ct.Role }
                ).ToListAsync(cancellationToken);

                var requestedTeacherId = dto.TeacherId.HasValue && dto.TeacherId.Value > 0 ? dto.TeacherId.Value : (int?)null;
                var lockTeachers = assignedTeachers.Select(t => t.TeacherId).ToList();
                if (requestedTeacherId.HasValue)
                {
                    lockTeachers.Add(requestedTeacherId.Value);
                }
                foreach (var tid in lockTeachers.Distinct().OrderBy(x => x))
                {
                    await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({300_000_000L + tid})", cancellationToken);
                }

                await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock_shared({TimeSlotLockKey})", cancellationToken);

                var targetClass = await _context.CourseClasses
                    .AsNoTracking()
                    .Where(c => c.Id == dto.ClassId)
                    .Select(c => new { c.ClassCode, c.Status, c.MaxStudents, c.StartDate, c.EndDate })
                    .FirstOrDefaultAsync(cancellationToken);
                if (targetClass == null || (targetClass.Status != "upcoming" && targetClass.Status != "ongoing"))
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiBadRequest("Lớp học không tồn tại hoặc không ở trạng thái đang hoạt động.");
                }

                string? assignedTeacherName = null;
                if (requestedTeacherId.HasValue)
                {
                    var validTeacher = await LoadVerifiedActiveTeacherAsync(requestedTeacherId.Value, cancellationToken);
                    if (validTeacher == null)
                    {
                        await transaction.RollbackAsync(CancellationToken.None);
                        return this.ApiBadRequest("Giảng viên thay thế không tồn tại, chưa xác minh hoặc đang bị khóa.");
                    }
                    assignedTeacherName = validTeacher.FullName;
                }

                if (dto.ReplaceExisting)
                {
                    var existingIds = await _context.ClassSchedules
                        .AsNoTracking()
                        .Where(cs => cs.ClassId == dto.ClassId)
                        .Select(cs => cs.Id)
                        .ToListAsync(cancellationToken);

                    bool hasLessonsLinked = existingIds.Count > 0 && await _context.ClassLessons
                        .AsNoTracking()
                        .AnyAsync(cl => cl.ScheduleId.HasValue && existingIds.Contains(cl.ScheduleId.Value), cancellationToken);
                    if (hasLessonsLinked)
                    {
                        await transaction.RollbackAsync(CancellationToken.None);
                        return this.ApiBadRequest("Không thể thay thế toàn bộ lịch cũ vì lớp đã có nhật ký buổi dạy gắn với lịch hiện tại. Hãy bỏ chọn 'Thay thế lịch cũ' hoặc xóa từng buổi chưa dạy.");
                    }

                    var oldRows = await _context.ClassSchedules
                        .Where(cs => cs.ClassId == dto.ClassId)
                        .ToListAsync(cancellationToken);
                    if (oldRows.Count > 0)
                    {
                        _context.ClassSchedules.RemoveRange(oldRows);
                        await _context.SaveChangesAsync(cancellationToken);
                    }
                }

                foreach (var item in dto.Items)
                {
                    var inspection = await InspectCandidateAndTeachersAsync(
                        dto.ClassId,
                        item.ClassroomId,
                        item.TimeSlotId,
                        new List<int> { item.DayOfWeek },
                        requestedTeacherId,
                        excludeScheduleId: null,
                        cancellationToken);

                    if (inspection.Conflicts.Count > 0)
                    {
                        await transaction.RollbackAsync(CancellationToken.None);
                        return this.ApiBadRequest(inspection.Conflicts[0].Message);
                    }
                }

                var now = DateTime.UtcNow;
                if (requestedTeacherId.HasValue)
                {
                    if (!dto.ReplaceExisting)
                    {
                        var existingConflict = await CheckTeacherConflictAgainstClassSchedulesAsync(
                            dto.ClassId,
                            requestedTeacherId.Value,
                            assignedTeacherName!,
                            targetClass.StartDate,
                            targetClass.EndDate,
                            excludeScheduleId: null,
                            cancellationToken);
                        if (existingConflict != null)
                        {
                            await transaction.RollbackAsync(CancellationToken.None);
                            return this.ApiBadRequest(existingConflict);
                        }
                    }

                    await ApplyMainTeacherAsync(dto.ClassId, requestedTeacherId.Value, now, cancellationToken);
                }

                var newSchedules = dto.Items.Select(item => new ClassSchedule
                {
                    ClassId = dto.ClassId,
                    ClassroomId = item.ClassroomId,
                    TimeSlotId = item.TimeSlotId,
                    DayOfWeek = item.DayOfWeek,
                    CreatedAt = now
                }).ToList();

                _context.ClassSchedules.AddRange(newSchedules);
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                var successMsg = assignedTeacherName != null
                    ? $"Đã xếp tự động {newSchedules.Count} buổi/tuần cho lớp {targetClass.ClassCode} (GV: {assignedTeacherName})."
                    : $"Đã xếp tự động {newSchedules.Count} buổi/tuần cho lớp {targetClass.ClassCode}.";

                return Ok(new { success = true, message = successMsg, count = newSchedules.Count });
            }
            catch (OperationCanceledException)
            {
                return this.ApiCancelled();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể áp dụng xếp lịch tự động cho lớp {ClassId}.", dto.ClassId);
                await transaction.RollbackAsync(CancellationToken.None);
                return this.ApiServerError("Không thể lưu thời khóa biểu tự động.");
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
                await AdminCoreHelpers.SyncExpiredClassesAsync(_context, cancellationToken);

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
                    startTime = AdminCoreHelpers.FormatTime24(ts.StartTime),
                    endTime = AdminCoreHelpers.FormatTime24(ts.EndTime),
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
                        var overlappingOthers = await (
                            from cs in _context.ClassSchedules.AsNoTracking()
                            join cc in _context.CourseClasses.AsNoTracking() on cs.ClassId equals cc.Id
                            join r in _context.Classrooms.AsNoTracking() on cs.ClassroomId equals r.Id
                            join ts in _context.TimeSlots.AsNoTracking() on cs.TimeSlotId equals ts.Id
                            where cs.TimeSlotId != id
                                  && (cc.Status == "upcoming" || cc.Status == "ongoing")
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

                                foreach (var other in overlappingOthers)
                                {
                                    var otherRange = AdminCoreHelpers.FormatTimeRange(other.StartTime, other.EndTime);

                                    if (other.ClassId == mine.ClassId)
                                    {
                                        if (other.DayOfWeek == mine.DayOfWeek)
                                        {
                                            await transaction.RollbackAsync(CancellationToken.None);
                                            return this.ApiBadRequest(
                                                $"Không thể đổi giờ ca {slotName} ({newRange}): Lớp {mine.ClassCode} bị trùng/giao giờ vào {AdminCoreHelpers.FormatDayFull(mine.DayOfWeek)} với ca {other.SlotName} ({otherRange}).");
                                        }
                                        if (other.ClassroomId == mine.ClassroomId)
                                        {
                                            await transaction.RollbackAsync(CancellationToken.None);
                                            return this.ApiBadRequest(
                                                $"Không thể đổi giờ ca {slotName} ({newRange}): Lớp {mine.ClassCode} bị giao giờ tại phòng {mine.ClassroomName} với ca {other.SlotName} ({otherRange}).");
                                        }
                                        continue;
                                    }

                                    // Chỉ coi là xung đột phòng/giảng viên nếu khoảng ngày [StartDate, EndDate] của 2 lớp có giao nhau
                                    var dateOverlap = mine.StartDate <= other.EndDate && other.StartDate <= mine.EndDate;
                                    if (!dateOverlap)
                                    {
                                        continue;
                                    }

                                    // Quy tắc phòng-ca học: Trong cùng 1 phòng, các lớp hoạt động giao ngày bắt buộc có khung giờ ca học không giao nhau (bất kể thứ)
                                    if (other.ClassroomId == mine.ClassroomId)
                                    {
                                        await transaction.RollbackAsync(CancellationToken.None);
                                        return this.ApiBadRequest(
                                            $"Không thể đổi giờ ca {slotName} ({newRange}): Phòng {mine.ClassroomName} bị trùng/giao ca học giữa lớp {mine.ClassCode} và lớp {other.ClassCode} (ca {other.SlotName} {otherRange}). Mỗi lớp trong cùng phòng bắt buộc phải chọn ca học khác nhau.");
                                    }

                                    if (other.DayOfWeek == mine.DayOfWeek && mineTeachers != null && teachersByClass.TryGetValue(other.ClassId, out var otherTeachers))
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

        [HttpPost("/Admin/api/Schedules/TimeSlots/Delete/{id}")]
        [HttpDelete("/Admin/api/Schedules/TimeSlots/Delete/{id}")]
        public async Task<IActionResult> DeleteTimeSlot(int id, CancellationToken cancellationToken)
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
                    return this.ApiNotFound("Không tìm thấy ca học cần xóa.");
                }

                var linkedClasses = await (
                    from cs in _context.ClassSchedules.AsNoTracking()
                    join cc in _context.CourseClasses.AsNoTracking() on cs.ClassId equals cc.Id
                    where cs.TimeSlotId == id
                    select cc.ClassCode
                ).Distinct().OrderBy(code => code).ToListAsync(cancellationToken);

                if (linkedClasses.Count > 0)
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiBadRequest(
                        $"Không thể xóa ca học '{slot.Name}' vì đang có lịch học gắn với lớp: {string.Join(", ", linkedClasses)}. Hãy xóa lịch lớp hoặc dùng nút Tạm ngưng.");
                }

                var slotName = slot.Name;
                _context.TimeSlots.Remove(slot);
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return Ok(new
                {
                    success = true,
                    message = $"Đã xóa ca học '{slotName}' khỏi bảng time_slots.",
                    id
                });
            }
            catch (OperationCanceledException)
            {
                return this.ApiCancelled();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể xóa ca học {TimeSlotId}.", id);
                await transaction.RollbackAsync(CancellationToken.None);
                return this.ApiServerError("Không thể xóa ca học.");
            }
        }

        [HttpPost("/Admin/api/Schedules/TimeSlots/SeedDefaults")]
        public async Task<IActionResult> SeedDefaultTimeSlots(CancellationToken cancellationToken)
        {
            var defaults = new (string Name, TimeSpan Start, TimeSpan End, string Description)[]
            {
                ("Ca 1", new TimeSpan(8, 0, 0), new TimeSpan(9, 30, 0), "Ca sáng 1 (08:00 - 09:30)"),
                ("Ca 2", new TimeSpan(10, 0, 0), new TimeSpan(11, 30, 0), "Ca sáng 2 (10:00 - 11:30)"),
                ("Ca 3", new TimeSpan(14, 0, 0), new TimeSpan(15, 30, 0), "Ca chiều 1 (14:00 - 15:30)"),
                ("Ca 4", new TimeSpan(16, 0, 0), new TimeSpan(17, 30, 0), "Ca chiều 2 (16:00 - 17:30)"),
                ("Ca 5", new TimeSpan(18, 0, 0), new TimeSpan(19, 30, 0), "Ca tối 1 (18:00 - 19:30)"),
                ("Ca 6", new TimeSpan(20, 0, 0), new TimeSpan(21, 30, 0), "Ca tối 2 (20:00 - 21:30)")
            };

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({TimeSlotLockKey})", cancellationToken);

                var existing = await _context.TimeSlots.ToListAsync(cancellationToken);
                var addedCount = 0;

                foreach (var def in defaults)
                {
                    var match = existing.FirstOrDefault(ts =>
                        ts.Name.Equals(def.Name, StringComparison.OrdinalIgnoreCase)
                        || (ts.StartTime == def.Start && ts.EndTime == def.End));

                    if (match == null)
                    {
                        _context.TimeSlots.Add(new TimeSlot
                        {
                            Name = def.Name,
                            StartTime = def.Start,
                            EndTime = def.End,
                            Description = def.Description,
                            IsActive = true
                        });
                        addedCount++;
                    }
                    else if (!match.IsActive)
                    {
                        match.IsActive = true;
                        addedCount++;
                    }
                }

                if (addedCount > 0)
                {
                    await _context.SaveChangesAsync(cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);

                return Ok(new
                {
                    success = true,
                    addedCount,
                    message = addedCount > 0
                        ? $"Đã khởi tạo / kích hoạt nhanh {addedCount} ca học mẫu chuẩn 24h (Ca 1 -> Ca 6: 08:00 - 21:30) vào bảng time_slots."
                        : "Các khung giờ chuẩn 24h (Ca 1 -> Ca 6: 08:00 - 21:30) đã có đủ trong bảng time_slots."
                });
            }
            catch (OperationCanceledException)
            {
                return this.ApiCancelled();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể tạo nhanh ca học mẫu.");
                await transaction.RollbackAsync(CancellationToken.None);
                return this.ApiServerError("Không thể tạo nhanh ca học mẫu.");
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
                return "Định dạng giờ bắt đầu hoặc giờ kết thúc không hợp lệ (sử dụng chuẩn 24h HH:mm trong khoảng 00:00 - 23:59, ví dụ 08:00, 14:30, 20:00).";
            }

            if (startTime >= endTime)
            {
                return "Giờ kết thúc phải lớn hơn giờ bắt đầu (chuẩn 24h 00:00 - 23:59).";
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

            if (parsed.Days != 0 || parsed < TimeSpan.Zero || parsed >= TimeSpan.FromHours(24))
            {
                return false;
            }

            result = parsed;
            return true;
        }

        private async Task<SimpleTeacherInfo?> LoadVerifiedActiveTeacherAsync(int teacherId, CancellationToken cancellationToken)
        {
            return await (
                from t in _context.Teachers.AsNoTracking()
                join u in _context.Users.AsNoTracking() on t.UserId equals u.Id
                where t.Id == teacherId && t.IsVerified && u.Role == "teacher" && u.IsActive
                select new SimpleTeacherInfo(t.Id, u.FullName, t.Qualification)
            ).FirstOrDefaultAsync(cancellationToken);
        }

        private async Task ApplyMainTeacherAsync(int classId, int teacherId, DateTime now, CancellationToken cancellationToken)
        {
            var sampleAssigned = await _context.ClassTeachers
                .AsNoTracking()
                .Select(ct => (DateTime?)ct.AssignedAt)
                .FirstOrDefaultAsync(cancellationToken);

            var dbNow = sampleAssigned.HasValue && sampleAssigned.Value.Kind == DateTimeKind.Utc
                ? now
                : DateTime.SpecifyKind(now, DateTimeKind.Unspecified);

            // Xóa giảng viên chính cũ (trừ giảng viên mới nếu đang là trợ giảng)
            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"DELETE FROM class_teachers WHERE class_id = {classId} AND role = 'main' AND teacher_id <> {teacherId}",
                cancellationToken);

            var updated = await _context.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE class_teachers SET role = 'main', assigned_at = {dbNow} WHERE class_id = {classId} AND teacher_id = {teacherId}",
                cancellationToken);

            if (updated == 0)
            {
                await _context.Database.ExecuteSqlInterpolatedAsync(
                    $"INSERT INTO class_teachers (class_id, teacher_id, role, assigned_at) VALUES ({classId}, {teacherId}, 'main', {dbNow})",
                    cancellationToken);
            }
        }

        private async Task<string?> CheckTeacherConflictAgainstClassSchedulesAsync(
            int classId,
            int teacherId,
            string teacherName,
            DateTime targetStart,
            DateTime targetEnd,
            int? excludeScheduleId,
            CancellationToken cancellationToken)
        {
            var classSchedules = await (
                from cs in _context.ClassSchedules.AsNoTracking()
                join ts in _context.TimeSlots.AsNoTracking() on cs.TimeSlotId equals ts.Id
                where cs.ClassId == classId && (!excludeScheduleId.HasValue || cs.Id != excludeScheduleId.Value)
                select new { cs.DayOfWeek, ts.Name, ts.StartTime, ts.EndTime }
            ).ToListAsync(cancellationToken);

            if (classSchedules.Count == 0) return null;

            var days = classSchedules.Select(s => s.DayOfWeek).Distinct().ToList();
            var otherTeacherSchedules = await (
                from cs in _context.ClassSchedules.AsNoTracking()
                join cc in _context.CourseClasses.AsNoTracking() on cs.ClassId equals cc.Id
                join ct in _context.ClassTeachers.AsNoTracking() on cc.Id equals ct.ClassId
                join ts in _context.TimeSlots.AsNoTracking() on cs.TimeSlotId equals ts.Id
                where cs.ClassId != classId
                      && ct.TeacherId == teacherId
                      && (cc.Status == "upcoming" || cc.Status == "ongoing")
                      && targetStart <= cc.EndDate
                      && cc.StartDate <= targetEnd
                      && days.Contains(cs.DayOfWeek)
                select new { cs.DayOfWeek, cc.ClassCode, cc.ClassName, ts.Name, ts.StartTime, ts.EndTime }
            ).ToListAsync(cancellationToken);

            foreach (var mine in classSchedules)
            {
                var hit = otherTeacherSchedules.FirstOrDefault(o =>
                    o.DayOfWeek == mine.DayOfWeek
                    && o.StartTime < mine.EndTime
                    && mine.StartTime < o.EndTime);
                if (hit != null)
                {
                    var myRange = AdminCoreHelpers.FormatTimeRange(mine.StartTime, mine.EndTime);
                    var hitRange = AdminCoreHelpers.FormatTimeRange(hit.StartTime, hit.EndTime);
                    return $"Trùng lịch giảng viên: Giảng viên {teacherName} có lịch dạy lớp {hit.ClassCode} ({hit.ClassName}) vào {AdminCoreHelpers.FormatDayFull(mine.DayOfWeek)}, ca {hit.Name} ({hitRange}), bị trùng với buổi khác của lớp này ({mine.Name} {myRange}).";
                }
            }

            return null;
        }

        private async Task<CandidateInspectionResult> InspectCandidateAndTeachersAsync(
            int classId,
            int classroomId,
            int timeSlotId,
            List<int> days,
            int? requestedTeacherId,
            int? excludeScheduleId,
            CancellationToken cancellationToken)
        {
            var conflicts = new List<CandidateConflictItem>();

            var targetClass = await _context.CourseClasses
                .AsNoTracking()
                .Where(c => c.Id == classId)
                .Select(c => new { c.ClassCode, c.ClassName, c.MaxStudents, c.StartDate, c.EndDate })
                .FirstOrDefaultAsync(cancellationToken);

            var classroom = await _context.Classrooms
                .AsNoTracking()
                .Where(r => r.Id == classroomId)
                .Select(r => new { r.Name, r.Capacity, r.IsActive })
                .FirstOrDefaultAsync(cancellationToken);

            var timeSlot = await _context.TimeSlots
                .AsNoTracking()
                .Where(ts => ts.Id == timeSlotId)
                .Select(ts => new { ts.Name, ts.StartTime, ts.EndTime, ts.IsActive })
                .FirstOrDefaultAsync(cancellationToken);

            if (targetClass == null || classroom == null || timeSlot == null)
            {
                return new CandidateInspectionResult(conflicts, new List<object>());
            }

            if (classroom.Capacity < targetClass.MaxStudents)
            {
                conflicts.Add(new CandidateConflictItem(
                    "capacity", 0, "Chung", null, null, null, null,
                    $"Sức chứa phòng {classroom.Name} ({classroom.Capacity} chỗ) nhỏ hơn sĩ số tối đa của lớp {targetClass.ClassCode} ({targetClass.MaxStudents} học viên)."));
            }

            var verifiedTeachers = await (
                from t in _context.Teachers.AsNoTracking()
                join u in _context.Users.AsNoTracking() on t.UserId equals u.Id
                where t.IsVerified && u.Role == "teacher" && u.IsActive
                orderby u.FullName
                select new SimpleTeacherInfo(t.Id, u.FullName, t.Qualification)
            ).ToListAsync(cancellationToken);

            var assignedTeachers = await (
                from ct in _context.ClassTeachers.AsNoTracking()
                join t in _context.Teachers.AsNoTracking() on ct.TeacherId equals t.Id
                join u in _context.Users.AsNoTracking() on t.UserId equals u.Id
                where ct.ClassId == classId
                select new { TeacherId = t.Id, TeacherName = u.FullName, ct.Role }
            ).ToListAsync(cancellationToken);

            var checkTeacherIds = requestedTeacherId.HasValue && requestedTeacherId.Value > 0
                ? assignedTeachers.Where(t => t.Role != "main").Select(t => t.TeacherId).Append(requestedTeacherId.Value).Distinct().ToList()
                : assignedTeachers.Select(t => t.TeacherId).Distinct().ToList();

            var teacherNameById = assignedTeachers
                .GroupBy(t => t.TeacherId)
                .ToDictionary(g => g.Key, g => g.First().TeacherName);
            if (requestedTeacherId.HasValue && requestedTeacherId.Value > 0)
            {
                var vt = verifiedTeachers.FirstOrDefault(x => x.Id == requestedTeacherId.Value);
                if (vt != null) teacherNameById[vt.Id] = vt.FullName;
            }

            var slotStart = timeSlot.StartTime;
            var slotEnd = timeSlot.EndTime;
            var timeLabel = $"{timeSlot.Name} ({AdminCoreHelpers.FormatTimeRange(slotStart, slotEnd)})";

            // Kiểm tra xung đột trên các ngày được chọn
            var sameClassHits = await (
                from cs in _context.ClassSchedules.AsNoTracking()
                join ts in _context.TimeSlots.AsNoTracking() on cs.TimeSlotId equals ts.Id
                where cs.ClassId == classId
                      && (!excludeScheduleId.HasValue || cs.Id != excludeScheduleId.Value)
                      && days.Contains(cs.DayOfWeek)
                      && ts.StartTime < slotEnd && slotStart < ts.EndTime
                select new { cs.DayOfWeek, ts.Name, ts.StartTime, ts.EndTime }
            ).ToListAsync(cancellationToken);

            // Kiểm tra xung đột phòng - ca học: Một phòng được mở nhiều lớp,
            // nhưng trong cùng khoảng ngày [StartDate, EndDate] giao nhau, mỗi lớp bắt buộc phải học ở ca học khác nhau (không trùng TimeSlotId hoặc giao giờ).
            var roomSlotHits = await (
                from cs in _context.ClassSchedules.AsNoTracking()
                join cc in _context.CourseClasses.AsNoTracking() on cs.ClassId equals cc.Id
                join ts in _context.TimeSlots.AsNoTracking() on cs.TimeSlotId equals ts.Id
                where cs.ClassroomId == classroomId
                      && cs.ClassId != classId
                      && (cc.Status == "upcoming" || cc.Status == "ongoing")
                      && targetClass.StartDate <= cc.EndDate && cc.StartDate <= targetClass.EndDate
                      && (cs.TimeSlotId == timeSlotId
                          || (ts.StartTime < slotEnd && slotStart < ts.EndTime))
                select new { cs.DayOfWeek, ClassId = cc.Id, cc.ClassCode, cc.ClassName, SlotId = ts.Id, SlotName = ts.Name, ts.StartTime, ts.EndTime }
            ).ToListAsync(cancellationToken);

            foreach (var grp in roomSlotHits.GroupBy(x => (x.ClassId, x.SlotId)))
            {
                var hit = grp.First();
                var hitRange = AdminCoreHelpers.FormatTimeRange(hit.StartTime, hit.EndTime);
                var hitDays = string.Join(", ", grp.Select(x => x.DayOfWeek).Distinct().OrderBy(d => d).Select(AdminCoreHelpers.FormatDayShort));
                conflicts.Add(new CandidateConflictItem(
                    "room", 0, hitDays, null, null, hit.ClassCode, hit.ClassName,
                    $"Trùng ca học trong cùng phòng: Phòng {classroom.Name} đã có lớp {hit.ClassCode} ({hit.ClassName}, {hitDays}) đăng ký ở ca {hit.SlotName} ({hitRange}). Mỗi lớp trong cùng một phòng bắt buộc phải chọn ca học khác nhau."));
            }

            // Không cho cùng 1 lớp đăng ký 2 ca khác nhau giao giờ trong cùng 1 phòng
            var sameClassRoomSlotOverlap = await (
                from cs in _context.ClassSchedules.AsNoTracking()
                join ts in _context.TimeSlots.AsNoTracking() on cs.TimeSlotId equals ts.Id
                where cs.ClassroomId == classroomId
                      && cs.ClassId == classId
                      && (!excludeScheduleId.HasValue || cs.Id != excludeScheduleId.Value)
                      && cs.TimeSlotId != timeSlotId
                      && ts.StartTime < slotEnd && slotStart < ts.EndTime
                select new { SlotName = ts.Name, ts.StartTime, ts.EndTime }
            ).FirstOrDefaultAsync(cancellationToken);
            if (sameClassRoomSlotOverlap != null)
            {
                var hitRange = AdminCoreHelpers.FormatTimeRange(sameClassRoomSlotOverlap.StartTime, sameClassRoomSlotOverlap.EndTime);
                conflicts.Add(new CandidateConflictItem(
                    "room", 0, "Chung", null, null, targetClass.ClassCode, targetClass.ClassName,
                    $"Chồng chéo ca học trong cùng phòng: Lớp {targetClass.ClassCode} đã đăng ký ca {sameClassRoomSlotOverlap.SlotName} ({hitRange}) tại phòng {classroom.Name}, giao giờ với ca {timeLabel}."));
            }

            // Tải toàn bộ lịch bận của TẤT CẢ giảng viên trong khung ngày/giờ đang kiểm tra (và các lịch khác của lớp nếu đổi GV)
            var classOtherSchedules = await (
                from cs in _context.ClassSchedules.AsNoTracking()
                join ts in _context.TimeSlots.AsNoTracking() on cs.TimeSlotId equals ts.Id
                where cs.ClassId == classId && (!excludeScheduleId.HasValue || cs.Id != excludeScheduleId.Value)
                select new { cs.DayOfWeek, ts.Name, ts.StartTime, ts.EndTime }
            ).ToListAsync(cancellationToken);

            var classBusyTeacherSchedules = await (
                from cs in _context.ClassSchedules.AsNoTracking()
                join cc in _context.CourseClasses.AsNoTracking() on cs.ClassId equals cc.Id
                join ct in _context.ClassTeachers.AsNoTracking() on cc.Id equals ct.ClassId
                join ts in _context.TimeSlots.AsNoTracking() on cs.TimeSlotId equals ts.Id
                where cs.ClassId != classId
                      && (cc.Status == "upcoming" || cc.Status == "ongoing")
                      && targetClass.StartDate <= cc.EndDate && cc.StartDate <= targetClass.EndDate
                select new BusyTeacherScheduleRow(
                    ct.TeacherId,
                    cs.DayOfWeek,
                    cc.ClassCode,
                    cc.ClassName,
                    ts.Name,
                    ts.StartTime,
                    ts.EndTime)
            ).ToListAsync(cancellationToken);

            var allBusyTeacherSchedules = new List<BusyTeacherScheduleRow>(classBusyTeacherSchedules);

            foreach (var day in days)
            {
                var dayLabel = AdminCoreHelpers.FormatDayFull(day);
                var sameHit = sameClassHits.FirstOrDefault(x => x.DayOfWeek == day);
                if (sameHit != null)
                {
                    var range = $"{sameHit.Name} ({AdminCoreHelpers.FormatTimeRange(sameHit.StartTime, sameHit.EndTime)})";
                    conflicts.Add(new CandidateConflictItem(
                        "sameClass", day, dayLabel, null, null, targetClass.ClassCode, targetClass.ClassName,
                        $"Lớp {targetClass.ClassCode} đã có lịch học trùng/giao giờ vào {dayLabel}, {range}."));
                }

                foreach (var tid in checkTeacherIds)
                {
                    var tHit = allBusyTeacherSchedules.FirstOrDefault(x =>
                        x.TeacherId == tid
                        && x.DayOfWeek == day
                        && x.StartTime < slotEnd && slotStart < x.EndTime);
                    if (tHit != null)
                    {
                        var tName = teacherNameById.GetValueOrDefault(tid, $"GV #{tid}");
                        var range = $"{tHit.SlotName} ({AdminCoreHelpers.FormatTimeRange(tHit.StartTime, tHit.EndTime)})";
                        conflicts.Add(new CandidateConflictItem(
                            "teacher", day, dayLabel, tid, tName, tHit.ClassCode, tHit.ClassName,
                            $"Trùng giảng viên: Giảng viên {tName} đã được phân công lớp {tHit.ClassCode} ({tHit.ClassName}) vào {dayLabel}, ca {range} (giao với {timeLabel})."));
                    }
                }
            }

            // Đánh giá trạng thái rảnh/bận cho từng giảng viên để gợi ý "thay giáo viên khác"
            var checkWindows = classOtherSchedules
                .Select(s => (s.DayOfWeek, s.Name, s.StartTime, s.EndTime))
                .Concat(days.Select(d => (DayOfWeek: d, Name: timeSlot.Name, StartTime: slotStart, EndTime: slotEnd)))
                .ToList();

            var teacherStatuses = verifiedTeachers.Select(t =>
            {
                string? busyReason = null;
                foreach (var w in checkWindows)
                {
                    var hit = allBusyTeacherSchedules.FirstOrDefault(x =>
                        x.TeacherId == t.Id
                        && x.DayOfWeek == w.DayOfWeek
                        && x.StartTime < w.EndTime && w.StartTime < x.EndTime);
                    if (hit != null)
                    {
                        var hitRange = AdminCoreHelpers.FormatTimeRange(hit.StartTime, hit.EndTime);
                        busyReason = $"Trùng {AdminCoreHelpers.FormatDayFull(w.DayOfWeek)} - lớp {hit.ClassCode} ({hit.SlotName} {hitRange})";
                        break;
                    }
                }
                return (object)new
                {
                    id = t.Id,
                    fullName = t.FullName,
                    qualification = t.Qualification,
                    isAvailable = busyReason == null,
                    conflictReason = busyReason
                };
            }).ToList();

            return new CandidateInspectionResult(conflicts, teacherStatuses);
        }

        private static string? FindTeacherConflictInSnapshot(
            int? mainTeacherId,
            List<int> assistantTeacherIds,
            DateTime targetStart,
            DateTime targetEnd,
            int day,
            TimeSpan slotStart,
            TimeSpan slotEnd,
            List<ActiveScheduleSnapshot> otherSchedules,
            Dictionary<int, List<int>> teachersByClassId,
            string mainTeacherName)
        {
            var idsToCheck = new List<int>();
            if (mainTeacherId.HasValue && mainTeacherId.Value > 0) idsToCheck.Add(mainTeacherId.Value);
            idsToCheck.AddRange(assistantTeacherIds);

            foreach (var tid in idsToCheck.Distinct())
            {
                var hit = otherSchedules.FirstOrDefault(o =>
                    teachersByClassId.TryGetValue(o.ClassId, out var tids) && tids.Contains(tid)
                    && o.DayOfWeek == day
                    && targetStart <= o.EndDate && o.StartDate <= targetEnd
                    && o.StartTime < slotEnd && slotStart < o.EndTime);
                if (hit != null)
                {
                    var label = tid == mainTeacherId ? mainTeacherName : $"Trợ giảng #{tid}";
                    var hitRange = AdminCoreHelpers.FormatTimeRange(hit.StartTime, hit.EndTime);
                    return $"Cảnh báo trùng lịch: GV {label} đang dạy lớp {hit.ClassCode} ({hit.ClassName}) vào {hit.SlotName} ({hitRange}).";
                }
            }

            return null;
        }

        private static AutoProposedSlot BuildAutoProposedSlot(
            int day,
            int slotId,
            string slotName,
            string timeRange,
            TimeSpan slotStart,
            TimeSpan slotEnd,
            int roomId,
            string roomName,
            int? teacherId,
            string teacherName,
            bool hasConflict,
            string? conflictType,
            string? conflictMessage,
            List<SimpleTeacherInfo> verifiedTeachers,
            DateTime targetStart,
            DateTime targetEnd,
            List<ActiveScheduleSnapshot> otherSchedules,
            Dictionary<int, List<int>> teachersByClassId,
            List<ActiveScheduleSnapshot> existingClassSchedules)
        {
            var alternatives = verifiedTeachers
                .Select(t =>
                {
                    var busyThisSlot = otherSchedules.Any(o =>
                        teachersByClassId.TryGetValue(o.ClassId, out var tids) && tids.Contains(t.Id)
                        && o.DayOfWeek == day
                        && targetStart <= o.EndDate && o.StartDate <= targetEnd
                        && o.StartTime < slotEnd && slotStart < o.EndTime);

                    var busyOtherClassSessions = existingClassSchedules.Any(cs => otherSchedules.Any(o =>
                        teachersByClassId.TryGetValue(o.ClassId, out var tids) && tids.Contains(t.Id)
                        && o.DayOfWeek == cs.DayOfWeek
                        && targetStart <= o.EndDate && o.StartDate <= targetEnd
                        && o.StartTime < cs.EndTime && cs.StartTime < o.EndTime));

                    return new
                    {
                        id = t.Id,
                        fullName = t.FullName,
                        qualification = t.Qualification,
                        isAvailable = !busyThisSlot && !busyOtherClassSessions
                    };
                })
                .Where(t => t.isAvailable)
                .Select(t => new AlternativeTeacherItem(t.id, t.fullName, t.qualification))
                .ToList();

            return new AutoProposedSlot(
                day,
                AdminCoreHelpers.FormatDayFull(day),
                slotId,
                slotName,
                timeRange,
                slotStart,
                slotEnd,
                roomId,
                roomName,
                teacherId,
                teacherName,
                hasConflict,
                conflictType,
                conflictMessage,
                alternatives);
        }

        private sealed record SimpleTeacherInfo(int Id, string FullName, string? Qualification);
        private sealed record AlternativeTeacherItem(int Id, string FullName, string? Qualification);
        private sealed record BusyTeacherScheduleRow(
            int TeacherId,
            int DayOfWeek,
            string ClassCode,
            string ClassName,
            string SlotName,
            TimeSpan StartTime,
            TimeSpan EndTime);

        private sealed class ActiveScheduleSnapshot
        {
            public int ScheduleId { get; set; }
            public int ClassId { get; set; }
            public string ClassCode { get; set; } = string.Empty;
            public string ClassName { get; set; } = string.Empty;
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public int ClassroomId { get; set; }
            public string ClassroomName { get; set; } = string.Empty;
            public int TimeSlotId { get; set; }
            public string SlotName { get; set; } = string.Empty;
            public TimeSpan StartTime { get; set; }
            public TimeSpan EndTime { get; set; }
            public int DayOfWeek { get; set; }
        }

        private sealed record CandidateConflictItem(
            string Type,
            int DayOfWeek,
            string DayLabel,
            int? ConflictTeacherId,
            string? ConflictTeacherName,
            string? ConflictClassCode,
            string? ConflictClassName,
            string Message);

        private sealed record CandidateInspectionResult(
            List<CandidateConflictItem> Conflicts,
            List<object> TeacherStatuses);

        private sealed record AutoProposedSlot(
            int DayOfWeek,
            string DayLabel,
            int TimeSlotId,
            string TimeSlotName,
            string TimeRange,
            TimeSpan StartTime,
            TimeSpan EndTime,
            int ClassroomId,
            string ClassroomName,
            int? TeacherId,
            string TeacherName,
            bool HasConflict,
            string? ConflictType,
            string? ConflictMessage,
            List<AlternativeTeacherItem> AlternativeTeachers);

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

        public class RoomConflictCheckDto
        {
            public int ClassroomId { get; set; }
            public int? TeacherId { get; set; }
            public string? ScheduleDays { get; set; }
            public int? TimeSlotId { get; set; }
            public DateTime? OpenDate { get; set; }
            public DateTime? CloseDate { get; set; }
        }

        public class ScheduleCreateDto
        {
            public int ClassId { get; set; }
            public int ClassroomId { get; set; }
            public int TimeSlotId { get; set; }
            public int? TeacherId { get; set; }
            public int? ExcludeScheduleId { get; set; }
            public List<int> DaysOfWeek { get; set; } = new();
        }

        public class ScheduleUpdateDto
        {
            public int ClassroomId { get; set; }
            public int TimeSlotId { get; set; }
            public int DayOfWeek { get; set; }
            public int? TeacherId { get; set; }
        }

        public class AutoPreviewDto
        {
            public int ClassId { get; set; }
            public int SessionsPerWeek { get; set; } = 2;
            public int? TeacherId { get; set; }
            public int? PreferredTimeSlotId { get; set; }
            public int? PreferredClassroomId { get; set; }
            public List<int>? PreferredDays { get; set; }
            public bool ReplaceExisting { get; set; }
        }

        public class AutoApplyDto
        {
            public int ClassId { get; set; }
            public int? TeacherId { get; set; }
            public bool ReplaceExisting { get; set; }
            public List<AutoApplySlotDto> Items { get; set; } = new();
        }

        public class AutoApplySlotDto
        {
            public int DayOfWeek { get; set; }
            public int TimeSlotId { get; set; }
            public int ClassroomId { get; set; }
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