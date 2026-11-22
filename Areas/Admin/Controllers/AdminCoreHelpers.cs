using CNPM_EnglishLearning.Data;
using Microsoft.EntityFrameworkCore;

namespace CNPM_EnglishLearning.Areas.Admin.Controllers
{
    /// <summary>
    /// Helper nội bộ dùng chung cho cụm quản trị cốt lõi (Classes, Schedules, Enrollments, Facilities),
    /// giúp chuẩn hóa định dạng lịch, chống CSV Formula Injection, escape ILike và tránh trùng lặp kiểm tra lịch học viên.
    /// </summary>
    internal static class AdminCoreHelpers
    {
        public static string FormatDayShort(int day) => day switch
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

        public static string FormatDayFull(int day) => day switch
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

        public static string FormatTime24(TimeSpan time)
        {
            return $"{time.Hours:D2}:{time.Minutes:D2}";
        }

        public static string FormatTimeRange(TimeSpan start, TimeSpan end)
        {
            return $"{FormatTime24(start)} - {FormatTime24(end)}";
        }

        public static string FormatEnrollmentStatusVi(string? status) => (status ?? string.Empty).ToLowerInvariant() switch
        {
            "active" => "Đang học (active)",
            "completed" => "Hoàn thành (completed)",
            "cancelled" => "Đã hủy (cancelled)",
            _ => status ?? "--"
        };

        /// <summary>
        /// Escape chuỗi xuất CSV theo chuẩn RFC 4180 đồng thời chặn CSV Formula Injection
        /// (kể cả ký tự xuống dòng \n ở đầu hoặc khoảng trắng đứng trước =, +, -, @, \t, \r, \n).
        /// </summary>
        public static string EscapeCsv(string? value)
        {
            var raw = value ?? string.Empty;
            if (raw.Length > 0)
            {
                var trimmedStart = raw.TrimStart();
                if (raw[0] is '=' or '+' or '-' or '@' or '\t' or '\r' or '\n'
                    || (trimmedStart.Length > 0 && trimmedStart[0] is '=' or '+' or '-' or '@' or '\t' or '\r' or '\n'))
                {
                    raw = "'" + raw;
                }
            }

            return $"\"{raw.Replace("\"", "\"\"")}\"";
        }

        /// <summary>
        /// Escape các ký tự wildcard của PostgreSQL LIKE/ILIKE (\, %, _) với escape char là '\'.
        /// </summary>
        public static string EscapeLikePattern(string term)
        {
            return term
                .Replace("\\", "\\\\")
                .Replace("%", "\\%")
                .Replace("_", "\\_");
        }

        public static string BuildContainsLikePattern(string term)
        {
            return $"%{EscapeLikePattern(term)}%";
        }

        /// <summary>
        /// Kiểm tra học viên có lịch học đang hoạt động (active enrollment + lớp upcoming/ongoing)
        /// trùng thứ, giao giờ và giao khoảng ngày [StartDate, EndDate] với lớp đích hay không.
        /// </summary>
        public static async Task<string?> CheckStudentScheduleConflictAsync(
            ApplicationDbContext context,
            int userId,
            int targetClassId,
            string targetClassCode,
            DateTime targetStart,
            DateTime targetEnd,
            CancellationToken cancellationToken)
        {
            var targetSchedules = await (
                from cs in context.ClassSchedules.AsNoTracking()
                join ts in context.TimeSlots.AsNoTracking() on cs.TimeSlotId equals ts.Id
                where cs.ClassId == targetClassId
                select new { cs.DayOfWeek, SlotName = ts.Name, ts.StartTime, ts.EndTime }
            ).ToListAsync(cancellationToken);

            if (targetSchedules.Count == 0)
            {
                return null;
            }

            var targetDays = targetSchedules.Select(s => s.DayOfWeek).Distinct().ToList();
            var normalizedStart = DateTime.SpecifyKind(targetStart.Date, DateTimeKind.Unspecified);
            var normalizedEnd = DateTime.SpecifyKind(targetEnd.Date, DateTimeKind.Unspecified);

            var otherStudentSchedules = await (
                from e in context.Enrollments.AsNoTracking()
                join cc in context.CourseClasses.AsNoTracking() on e.ClassId equals cc.Id
                join cs in context.ClassSchedules.AsNoTracking() on cc.Id equals cs.ClassId
                join ts in context.TimeSlots.AsNoTracking() on cs.TimeSlotId equals ts.Id
                where e.UserId == userId
                      && e.Status == "active"
                      && e.ClassId != targetClassId
                      && (cc.Status == "upcoming" || cc.Status == "ongoing")
                      && normalizedStart <= cc.EndDate
                      && cc.StartDate <= normalizedEnd
                      && targetDays.Contains(cs.DayOfWeek)
                select new
                {
                    cc.ClassCode,
                    cc.ClassName,
                    cs.DayOfWeek,
                    SlotName = ts.Name,
                    ts.StartTime,
                    ts.EndTime
                }
            ).ToListAsync(cancellationToken);

            foreach (var mine in targetSchedules)
            {
                var conflict = otherStudentSchedules.FirstOrDefault(other =>
                    other.DayOfWeek == mine.DayOfWeek
                    && other.StartTime < mine.EndTime
                    && mine.StartTime < other.EndTime);

                if (conflict != null)
                {
                    var myRange = $"{mine.SlotName} ({FormatTimeRange(mine.StartTime, mine.EndTime)})";
                    var conflictRange = $"{conflict.SlotName} ({FormatTimeRange(conflict.StartTime, conflict.EndTime)})";
                    return $"Trùng lịch học viên: Học viên đang theo học lớp {conflict.ClassCode} ({conflict.ClassName}) vào {FormatDayFull(mine.DayOfWeek)}, ca {conflictRange}, giao với lịch lớp {targetClassCode} ({myRange}).";
                }
            }

            return null;
        }

        public static int[] ParseScheduleDays(string? scheduleDays) => (scheduleDays ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "t246" => [1, 3, 5],
            "t357" => [2, 4, 6],
            "all" => [1, 2, 3, 4, 5, 6],
            _ => []
        };

        public static string? FormatScheduleDaysLabel(string? scheduleDays) => (scheduleDays ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "t246" => "Thứ 2 - Thứ 4 - Thứ 6",
            "t357" => "Thứ 3 - Thứ 5 - Thứ 7",
            "all" => "Cả tuần (Thứ 2 - Thứ 7)",
            _ => null
        };

        /// <summary>
        /// Tính số ca/tuần theo danh sách thứ (ví dụ T2-4-6 = 3 ca/tuần), tổng số tuần phòng/lớp mở
        /// và tổng số ca (buổi học) trong khoảng [startDate, endDate].
        /// </summary>
        public static SessionStatsInfo CalculateSessionStats(IEnumerable<int> daysOfWeek, DateTime? startDate, DateTime? endDate)
        {
            var distinctDays = daysOfWeek
                .Where(d => d >= 1 && d <= 7)
                .Distinct()
                .ToHashSet();

            var sessionsPerWeek = distinctDays.Count;
            if (sessionsPerWeek == 0 || !startDate.HasValue || !endDate.HasValue)
            {
                return new SessionStatsInfo(sessionsPerWeek, 0, 0);
            }

            var start = startDate.Value.Date;
            var end = endDate.Value.Date;
            if (end < start)
            {
                return new SessionStatsInfo(sessionsPerWeek, 0, 0);
            }

            var totalDays = (end - start).Days + 1;
            var totalWeeks = (int)Math.Ceiling(totalDays / 7.0);
            var fullWeeks = totalDays / 7;
            var remainder = totalDays % 7;

            var totalSessions = fullWeeks * sessionsPerWeek;
            for (var i = 0; i < remainder; i++)
            {
                var dow = start.AddDays(i).DayOfWeek;
                var isoDay = dow == DayOfWeek.Sunday ? 7 : (int)dow;
                if (distinctDays.Contains(isoDay))
                {
                    totalSessions++;
                }
            }

            return new SessionStatsInfo(sessionsPerWeek, totalWeeks, totalSessions);
        }

        public static DateTime GetTodayVnDate() =>
            DateTime.SpecifyKind(DateTime.UtcNow.AddHours(7).Date, DateTimeKind.Unspecified);

        private static int _legacyColumnsCleaned;

        /// <summary>
        /// Xóa các cột dư từng thêm vào bảng classrooms và course_classes để database gọn đúng model chuẩn.
        /// Chỉ chạy 1 lần khi ứng dụng khởi động / truy vấn đầu tiên.
        /// </summary>
        public static async Task EnsureCleanDatabaseSchemaAsync(
            ApplicationDbContext context,
            CancellationToken cancellationToken)
        {
            if (System.Threading.Interlocked.Exchange(ref _legacyColumnsCleaned, 1) == 1)
            {
                return;
            }

            try
            {
                await context.Database.ExecuteSqlRawAsync(@"
                    ALTER TABLE public.classrooms
                        DROP COLUMN IF EXISTS default_teacher_id,
                        DROP COLUMN IF EXISTS default_course_id,
                        DROP COLUMN IF EXISTS schedule_days,
                        DROP COLUMN IF EXISTS default_time_slot_id,
                        DROP COLUMN IF EXISTS open_date,
                        DROP COLUMN IF EXISTS close_date;
                    ALTER TABLE public.course_classes
                        DROP COLUMN IF EXISTS classroom_id;
                ", cancellationToken);
            }
            catch
            {
                // Bỏ qua nếu quyền DDL hạn chế hoặc cột đã được xóa
            }
        }

        /// <summary>
        /// Tự động chuyển trạng thái các lớp sắp diễn ra / đang diễn ra sang đã kết thúc (completed)
        /// khi đã đến hoặc qua ngày kết thúc (EndDate <= hôm nay theo giờ Việt Nam UTC+7),
        /// tự động chuyển sang đang diễn ra (ongoing) khi đủ điều kiện,
        /// đồng thời dọn sạch lịch học (ClassSchedules) còn sót của các lớp đã hủy (cancelled).
        /// Không hạ ngược lớp đang diễn ra (ongoing) về sắp khai giảng (upcoming).
        /// </summary>
        public static async Task<int> SyncExpiredClassesAsync(
            ApplicationDbContext context,
            CancellationToken cancellationToken)
        {
            await EnsureCleanDatabaseSchemaAsync(context, cancellationToken);

            var tomorrowVn = DateTime.SpecifyKind(GetTodayVnDate().AddDays(1), DateTimeKind.Unspecified);
            var nowUtc = DateTime.UtcNow;
            var expiredCount = await context.CourseClasses
                .Where(c => (c.Status == "upcoming" || c.Status == "ongoing") && c.EndDate < tomorrowVn)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(c => c.Status, "completed")
                    .SetProperty(c => c.UpdatedAt, nowUtc),
                    cancellationToken);

            // Tự động chuyển các ghi danh đang active thuộc các lớp đã kết thúc (completed) sang completed
            await context.Enrollments
                .Where(e => e.Status == "active"
                            && context.CourseClasses.Any(cc => cc.Id == e.ClassId && cc.Status == "completed"))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(e => e.Status, "completed"),
                    cancellationToken);

            // Tự động chuyển lớp Sắp khai giảng (upcoming) sang Đang diễn ra (ongoing) khi đủ điều kiện
            await context.CourseClasses
                .Where(c => c.Status == "upcoming"
                            && c.EndDate >= tomorrowVn
                            && c.StartDate < tomorrowVn
                            && context.ClassTeachers.Any(ct => ct.ClassId == c.Id)
                            && context.ClassSchedules.Any(cs => cs.ClassId == c.Id)
                            && context.Enrollments.Any(e => e.ClassId == c.Id && e.Status == "active"))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(c => c.Status, "ongoing")
                    .SetProperty(c => c.UpdatedAt, nowUtc),
                    cancellationToken);

            var cancelledScheduleIds = await (
                from cs in context.ClassSchedules.AsNoTracking()
                join cc in context.CourseClasses.AsNoTracking() on cs.ClassId equals cc.Id
                where cc.Status == "cancelled"
                select cs.Id
            ).ToListAsync(cancellationToken);

            if (cancelledScheduleIds.Count > 0)
            {
                await context.ClassLessons
                    .Where(cl => cl.ScheduleId.HasValue && cancelledScheduleIds.Contains(cl.ScheduleId.Value))
                    .ExecuteUpdateAsync(s => s.SetProperty(cl => cl.ScheduleId, (int?)null), cancellationToken);

                await context.ClassSchedules
                    .Where(cs => cancelledScheduleIds.Contains(cs.Id))
                    .ExecuteDeleteAsync(cancellationToken);
            }

            return expiredCount;
        }

        /// <summary>
        /// Đồng bộ trạng thái hết hạn và quét lịch lớp đang hoạt động (upcoming / ongoing).
        /// Thời khóa biểu chỉ hiển thị từ lớp học phần đang mở trong bảng class_schedules.
        /// </summary>
        public static async Task<SystemScheduleState> DetectSystemScheduleStateAsync(
            ApplicationDbContext context,
            CancellationToken cancellationToken)
        {
            await SyncExpiredClassesAsync(context, cancellationToken);

            var activeClassSchedules = await (
                from cs in context.ClassSchedules.AsNoTracking()
                join cc in context.CourseClasses.AsNoTracking() on cs.ClassId equals cc.Id
                join r in context.Classrooms.AsNoTracking() on cs.ClassroomId equals r.Id
                join ts in context.TimeSlots.AsNoTracking() on cs.TimeSlotId equals ts.Id
                where cc.Status == "upcoming" || cc.Status == "ongoing"
                select new ActiveClassScheduleSnapshot
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

            var classIds = activeClassSchedules.Select(s => s.ClassId).Distinct().ToList();
            var classTeachers = classIds.Count == 0
                ? []
                : await (
                    from ct in context.ClassTeachers.AsNoTracking()
                    join t in context.Teachers.AsNoTracking() on ct.TeacherId equals t.Id
                    join u in context.Users.AsNoTracking() on t.UserId equals u.Id
                    where classIds.Contains(ct.ClassId)
                    select new ClassTeacherLink(ct.ClassId, t.Id, u.FullName, ct.Role)
                ).ToListAsync(cancellationToken);

            var teachersByClass = classTeachers
                .GroupBy(x => x.ClassId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var configuredRooms = new List<ConfiguredRoomSchedule>();
            var conflicts = new List<SystemConflictDetail>();

            // 1. Xung đột giữa các ClassSchedule (lớp vs lớp)
            var reportedCrossDayRoomSlotPairs = new HashSet<(int, int, int, int, int)>();
            for (int i = 0; i < activeClassSchedules.Count; i++)
            {
                var a = activeClassSchedules[i];
                var aRange = FormatTimeRange(a.StartTime, a.EndTime);
                var aKey = $"cs-{a.ScheduleId}";
                teachersByClass.TryGetValue(a.ClassId, out var aTeachers);

                for (int j = i + 1; j < activeClassSchedules.Count; j++)
                {
                    var b = activeClassSchedules[j];
                    var timeOverlap = a.TimeSlotId == b.TimeSlotId || (a.StartTime < b.EndTime && b.StartTime < a.EndTime);
                    if (!timeOverlap) continue;

                    var bKey = $"cs-{b.ScheduleId}";
                    var bRange = FormatTimeRange(b.StartTime, b.EndTime);

                    if (a.DayOfWeek != b.DayOfWeek)
                    {
                        if (a.ClassId != b.ClassId
                            && a.ClassroomId == b.ClassroomId
                            && a.StartDate <= b.EndDate
                            && b.StartDate <= a.EndDate)
                        {
                            var pairKey = (
                                Math.Min(a.ClassId, b.ClassId),
                                Math.Max(a.ClassId, b.ClassId),
                                a.ClassroomId,
                                Math.Min(a.TimeSlotId, b.TimeSlotId),
                                Math.Max(a.TimeSlotId, b.TimeSlotId));
                            if (reportedCrossDayRoomSlotPairs.Add(pairKey))
                            {
                                conflicts.Add(new SystemConflictDetail(
                                    aKey, bKey, "class", "class",
                                    a.ScheduleId, b.ScheduleId, "room",
                                    a.ClassId, a.ClassCode, a.ClassName,
                                    b.ClassId, b.ClassCode, b.ClassName,
                                    null, null, a.ClassroomId, a.ClassroomName, b.ClassroomId,
                                    a.DayOfWeek, a.TimeSlotId, a.SlotName, aRange,
                                    $"Trùng ca học trong cùng phòng {a.ClassroomName}: Lớp {a.ClassCode} ({a.SlotName} {aRange}) trùng ca với lớp {b.ClassCode} ({b.SlotName} {bRange})."));
                            }
                        }
                        continue;
                    }

                    var dayLabel = FormatDayFull(a.DayOfWeek);

                    if (a.ClassId == b.ClassId)
                    {
                        conflicts.Add(new SystemConflictDetail(
                            aKey, bKey, "class", "class",
                            a.ScheduleId, b.ScheduleId, "class",
                            a.ClassId, a.ClassCode, a.ClassName,
                            b.ClassId, b.ClassCode, b.ClassName,
                            null, null, a.ClassroomId, a.ClassroomName, b.ClassroomId,
                            a.DayOfWeek, a.TimeSlotId, a.SlotName, aRange,
                            $"Lớp {a.ClassCode} có 2 ca giao giờ vào {dayLabel}: {a.SlotName} ({aRange}) và {b.SlotName} ({bRange})."));
                        continue;
                    }

                    if (!(a.StartDate <= b.EndDate && b.StartDate <= a.EndDate)) continue;

                    if (a.ClassroomId == b.ClassroomId)
                    {
                        conflicts.Add(new SystemConflictDetail(
                            aKey, bKey, "class", "class",
                            a.ScheduleId, b.ScheduleId, "room",
                            a.ClassId, a.ClassCode, a.ClassName,
                            b.ClassId, b.ClassCode, b.ClassName,
                            null, null, a.ClassroomId, a.ClassroomName, b.ClassroomId,
                            a.DayOfWeek, a.TimeSlotId, a.SlotName, aRange,
                            $"Trùng ca học trong cùng phòng {a.ClassroomName} ({dayLabel}, {a.SlotName} {aRange}): Lớp {a.ClassCode} trùng với {b.ClassCode}."));
                    }

                    if (aTeachers != null && teachersByClass.TryGetValue(b.ClassId, out var bTeachers))
                    {
                        var shared = aTeachers.FirstOrDefault(at => bTeachers.Any(bt => bt.TeacherId == at.TeacherId));
                        if (shared != null)
                        {
                            conflicts.Add(new SystemConflictDetail(
                                aKey, bKey, "class", "class",
                                a.ScheduleId, b.ScheduleId, "teacher",
                                a.ClassId, a.ClassCode, a.ClassName,
                                b.ClassId, b.ClassCode, b.ClassName,
                                shared.TeacherId, shared.TeacherName, a.ClassroomId, a.ClassroomName, b.ClassroomId,
                                a.DayOfWeek, a.TimeSlotId, a.SlotName, aRange,
                                $"Trùng lịch GV {shared.TeacherName} ({dayLabel}, {a.SlotName} {aRange}): Lớp {a.ClassCode} trùng với {b.ClassCode} ({b.SlotName} {bRange})."));
                        }
                    }
                }
            }

            return new SystemScheduleState(activeClassSchedules, configuredRooms, conflicts);
        }

        internal sealed record SessionStatsInfo(int SessionsPerWeek, int TotalWeeks, int TotalSessions);

        internal sealed record ClassTeacherLink(int ClassId, int TeacherId, string TeacherName, string Role);

        internal sealed class ActiveClassScheduleSnapshot
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

        internal sealed class ConfiguredRoomSchedule
        {
            public int ClassroomId { get; set; }
            public string ClassroomName { get; set; } = string.Empty;
            public string? Location { get; set; }
            public int Capacity { get; set; }
            public string? Description { get; set; }
            public bool IsActive { get; set; }
            public int? DefaultTeacherId { get; set; }
            public string? DefaultTeacherName { get; set; }
            public int? DefaultCourseId { get; set; }
            public string? DefaultCourseTitle { get; set; }
            public string ScheduleDays { get; set; } = string.Empty;
            public string ScheduleDaysLabel { get; set; } = string.Empty;
            public int[] Days { get; set; } = [];
            public int TimeSlotId { get; set; }
            public string SlotName { get; set; } = string.Empty;
            public TimeSpan StartTime { get; set; }
            public TimeSpan EndTime { get; set; }
            public string TimeRange { get; set; } = string.Empty;
            public DateTime? OpenDate { get; set; }
            public DateTime? CloseDate { get; set; }
            public SessionStatsInfo Stats { get; set; } = new(0, 0, 0);
        }

        internal sealed record SystemConflictDetail(
            string EntryKey,
            string OtherEntryKey,
            string SourceType,
            string OtherSourceType,
            int? ScheduleId,
            int? OtherScheduleId,
            string ConflictType,
            int? ClassId,
            string CodeLabel,
            string TitleLabel,
            int? OtherClassId,
            string OtherCodeLabel,
            string OtherTitleLabel,
            int? TeacherId,
            string? TeacherName,
            int ClassroomId,
            string ClassroomName,
            int OtherClassroomId,
            int DayOfWeek,
            int TimeSlotId,
            string TimeSlotName,
            string TimeRange,
            string Message);

        internal sealed record SystemScheduleState(
            List<ActiveClassScheduleSnapshot> ActiveClassSchedules,
            List<ConfiguredRoomSchedule> ConfiguredRooms,
            List<SystemConflictDetail> Conflicts);
    }
}

