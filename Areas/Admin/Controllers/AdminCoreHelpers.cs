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

        public static string FormatTimeRange(TimeSpan start, TimeSpan end)
        {
            return $"{start:hh\\:mm} - {end:hh\\:mm}";
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
    }
}

