using System.Linq.Expressions;
using CNPM_EnglishLearning.Areas.Student.DTOs;
using CNPM_EnglishLearning.Data;
using Microsoft.EntityFrameworkCore;

namespace CNPM_EnglishLearning.Areas.Student.Services
{
    /// <summary>
    /// Lớp cơ sở trừu tượng (Abstract OOP Base Class) cho toàn bộ các Service thuộc phân hệ Học sinh.
    /// Đóng gói ApplicationDbContext, các hằng số trạng thái dùng chung và các phương thức truy vấn batch tối ưu.
    /// </summary>
    public abstract class BaseStudentService
    {
        protected readonly ApplicationDbContext Db;

        // Hằng số trạng thái dùng chung (Tái sử dụng biến / tránh hardcode chuỗi lặp lại)
        protected const string EnrollmentActive = "active";
        protected const string EnrollmentCompleted = "completed";
        protected const string EnrollmentCancelled = "cancelled";

        protected const string ClassUpcoming = "upcoming";
        protected const string ClassOngoing = "ongoing";
        protected const string ClassCompleted = "completed";

        protected const string SessionScheduled = "scheduled";
        protected const string SessionCompleted = "completed";

        protected const string ExamInProgress = "in_progress";
        protected const string ExamSubmitted = "submitted";
        protected const string ExamPassed = "passed";
        protected const string ExamFailed = "failed";

        protected const string GradingAutoGraded = "auto_graded";
        protected const string GradingPendingReview = "pending_review";
        protected const string GradingGraded = "graded";

        protected const string QuestionTypeMultipleChoice = "multiple_choice";
        protected const string QuestionTypeFillBlank = "fill_blank";
        protected const string QuestionTypeEssay = "essay";

        protected BaseStudentService(ApplicationDbContext db)
        {
            Db = db ?? throw new ArgumentNullException(nameof(db));
        }

        /// <summary>
        /// Chuẩn hóa tham số phân trang (page >= 1, 1 <= limit <= 100).
        /// </summary>
        protected static (int Page, int Limit, int Skip) NormalizePagination(int page, int limit, int defaultLimit = 10)
        {
            var safePage = page < 1 ? 1 : page;
            var safeLimit = limit < 1 ? defaultLimit : (limit > 100 ? 100 : limit);
            var skip = (safePage - 1) * safeLimit;
            return (safePage, safeLimit, skip);
        }

        /// <summary>
        /// Tính phần trăm tiến độ học tập an toàn (0 - 100%).
        /// </summary>
        protected static int CalculatePercentage(int completed, int total)
        {
            if (total <= 0 || completed <= 0) return 0;
            var percentage = (int)Math.Round((double)completed * 100.0 / total, MidpointRounding.AwayFromZero);
            return Math.Clamp(percentage, 0, 100);
        }

        /// <summary>
        /// Định dạng khung giờ học từ TimeSpan (VD: "18:00 - 20:00").
        /// </summary>
        protected static string FormatTimeRange(TimeSpan startTime, TimeSpan endTime)
        {
            return $"{startTime:hh\\:mm} - {endTime:hh\\:mm}";
        }

        /// <summary>
        /// Chuyển đổi số thứ tự ngày trong tuần (1-7) sang tên tiếng Việt.
        /// Quy ước: 1 = Chủ nhật hoặc Thứ 2 tùy dữ liệu; hỗ trợ chuẩn 2..7 = Thứ 2..Thứ 7, 1/7/8 = Chủ nhật.
        /// </summary>
        protected static string GetDayOfWeekName(int dayOfWeek)
        {
            return dayOfWeek switch
            {
                2 => "Thứ 2",
                3 => "Thứ 3",
                4 => "Thứ 4",
                5 => "Thứ 5",
                6 => "Thứ 6",
                7 => "Thứ 7",
                1 or 0 or 8 => "Chủ nhật",
                _ => $"Ngày {dayOfWeek}"
            };
        }

        /// <summary>
        /// Quy đổi DateTime.DayOfWeek của .NET sang giá trị day_of_week chuẩn DB/Frontend (Thứ 2 = 2 ... Thứ 7 = 7, CN = 1).
        /// </summary>
        protected static int ToScheduleDayOfWeek(DayOfWeek dayOfWeek)
        {
            return dayOfWeek switch
            {
                DayOfWeek.Monday => 2,
                DayOfWeek.Tuesday => 3,
                DayOfWeek.Wednesday => 4,
                DayOfWeek.Thursday => 5,
                DayOfWeek.Friday => 6,
                DayOfWeek.Saturday => 7,
                DayOfWeek.Sunday => 1,
                _ => 1
            };
        }

        /// <summary>
        /// Quy đổi DateTime.DayOfWeek của .NET sang chuẩn ISO (Thứ 2 = 1 ... Thứ 7 = 6, CN = 7) khớp giá trị EF Core đọc từ ClassSchedule.DayOfWeek.
        /// </summary>
        protected static int ToIsoDayOfWeek(DayOfWeek dayOfWeek)
        {
            return dayOfWeek == DayOfWeek.Sunday ? 7 : (int)dayOfWeek;
        }

        /// <summary>
        /// Chuyển giá trị ISO (1=Thứ 2..6=Thứ 7, 7=CN) từ EF Core ClassSchedule.DayOfWeek sang chuẩn DB/Frontend (2=Thứ 2..7=Thứ 7, 1=CN).
        /// </summary>
        protected static int IsoToScheduleDayOfWeek(int isoDayOfWeek)
        {
            return isoDayOfWeek == 7 ? 1 : (isoDayOfWeek >= 1 && isoDayOfWeek <= 6 ? isoDayOfWeek + 1 : isoDayOfWeek);
        }

        /// <summary>
        /// Sinh ID tiếp theo an toàn cho bảng khi Insert (tránh lỗi trùng khóa chính nếu sequence PostgreSQL chưa đồng bộ sau khi seed).
        /// </summary>
        protected static async Task<int> GetNextIdAsync<TEntity>(
            IQueryable<TEntity> dbSet,
            Expression<Func<TEntity, int>> idSelector) where TEntity : class
        {
            var maxId = await dbSet.AsNoTracking()
                .Select(idSelector)
                .OrderByDescending(x => x)
                .FirstOrDefaultAsync();
            return maxId + 1;
        }

        /// <summary>
        /// Phân giải và lọc trường Text, tài liệu PDF và Video bài giảng từ cột content (hỗ trợ định dạng JSON/EL_LESSON_MEDIA_V1 của Supabase Storage).
        /// Đảm bảo nội dung bài học (Text), tệp PDF và Video luôn được tách bạch, sạch sẽ.
        /// </summary>
        protected static (string? TextContent, string? PdfUrl, string? PdfName, string? VideoUrl, string? VideoName) ParseLessonContent(string? rawContent)
        {
            if (string.IsNullOrWhiteSpace(rawContent))
            {
                return (null, null, null, null, null);
            }

            var trimmed = rawContent.Trim();
            var jsonStr = trimmed;
            if (trimmed.StartsWith("EL_LESSON_MEDIA_V1", StringComparison.OrdinalIgnoreCase))
            {
                jsonStr = trimmed.Substring("EL_LESSON_MEDIA_V1".Length).Trim();
            }

            if (jsonStr.StartsWith("{") && jsonStr.EndsWith("}"))
            {
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(jsonStr);
                    var root = doc.RootElement;

                    string? parsedText = null;
                    if (root.TryGetProperty("Text", out var textEl) && textEl.ValueKind == System.Text.Json.JsonValueKind.String)
                    {
                        parsedText = textEl.GetString();
                    }

                    string? pdfUrl = null;
                    string? pdfName = null;
                    if (root.TryGetProperty("Pdf", out var pdfEl) && pdfEl.ValueKind == System.Text.Json.JsonValueKind.Object)
                    {
                        var key = pdfEl.TryGetProperty("Key", out var k) ? k.GetString() : null;
                        var name = pdfEl.TryGetProperty("Name", out var n) ? n.GetString() : null;
                        var bucket = pdfEl.TryGetProperty("Bucket", out var b) ? b.GetString() : "Lesson_media";
                        if (string.IsNullOrWhiteSpace(bucket)) bucket = "Lesson_media";

                        if (!string.IsNullOrWhiteSpace(key))
                        {
                            pdfUrl = $"https://wymdewnklhrnmirbvgxi.supabase.co/storage/v1/object/public/{bucket}/{key}";
                            pdfName = name ?? key;
                        }
                    }

                    string? videoUrl = null;
                    string? videoName = null;
                    if (root.TryGetProperty("Video", out var videoEl) && videoEl.ValueKind == System.Text.Json.JsonValueKind.Object)
                    {
                        var vKey = videoEl.TryGetProperty("Key", out var vk) ? vk.GetString() : null;
                        var vName = videoEl.TryGetProperty("Name", out var vn) ? vn.GetString() : null;
                        var vBucket = videoEl.TryGetProperty("Bucket", out var vb) ? vb.GetString() : "Lesson_media";
                        if (string.IsNullOrWhiteSpace(vBucket)) vBucket = "Lesson_media";

                        if (!string.IsNullOrWhiteSpace(vKey))
                        {
                            videoUrl = $"https://wymdewnklhrnmirbvgxi.supabase.co/storage/v1/object/public/{vBucket}/{vKey}";
                            videoName = vName ?? vKey;
                        }
                    }

                    return (parsedText, pdfUrl, pdfName, videoUrl, videoName);
                }
                catch
                {
                    // Fallback trả về nguyên bản chuỗi trimmed nếu parse thất bại
                    return (trimmed, null, null, null, null);
                }
            }

            // Nếu không phải định dạng JSON (nội dung văn bản thông thường)
            return (trimmed, null, null, null, null);
        }

        /// <summary>
        /// Truy vấn gộp (Batch Query) tổng số bài học và số bài học đã hoàn thành theo danh sách CourseId cho 1 học sinh.
        /// Giúp loại bỏ hoàn toàn lỗi N+1 Query tại Dashboard, MyClasses và Syllabus.
        /// </summary>
        protected async Task<(Dictionary<int, int> TotalByCourse, Dictionary<int, int> CompletedByCourse)> GetCourseProgressDictionariesAsync(
            IReadOnlyCollection<int> courseIds,
            int studentId)
        {
            if (courseIds.Count == 0)
            {
                return (new Dictionary<int, int>(), new Dictionary<int, int>());
            }

            // 1. Tổng số bài học của từng khóa trong 1 câu truy vấn GroupBy
            var totalLessonsList = await Db.Lessons.AsNoTracking()
                .Where(l => courseIds.Contains(l.CourseId))
                .GroupBy(l => l.CourseId)
                .Select(g => new { CourseId = g.Key, TotalCount = g.Count() })
                .ToListAsync();

            var totalByCourse = totalLessonsList.ToDictionary(x => x.CourseId, x => x.TotalCount);

            // 2. Số bài học học sinh đã hoàn thành trong từng khóa (Join Lessons & UserProgresses)
            var completedLessonsList = await (
                from p in Db.UserProgresses.AsNoTracking()
                join l in Db.Lessons.AsNoTracking() on p.LessonId equals l.Id
                where p.UserId == studentId && p.IsCompleted && courseIds.Contains(l.CourseId)
                group l by l.CourseId into g
                select new { CourseId = g.Key, CompletedCount = g.Select(x => x.Id).Distinct().Count() }
            ).ToListAsync();

            var completedByCourse = completedLessonsList.ToDictionary(x => x.CourseId, x => x.CompletedCount);

            return (totalByCourse, completedByCourse);
        }

        /// <summary>
        /// Truy vấn gộp (Batch Query) tổng số bài học và số bài học đã hoàn thành theo danh sách ClassId cho 1 học sinh.
        /// Ưu tiên đếm theo class_lessons nếu lớp đã có cấu hình bài học cụ thể, fallback theo course nếu chưa cấu hình.
        /// Giúp khắc phục tình trạng lớp có số bài học thực tế khác với tổng bài học trong bảng lessons của khóa học.
        /// </summary>
        protected async Task<(Dictionary<int, int> TotalByClass, Dictionary<int, int> CompletedByClass)> GetClassProgressDictionariesAsync(
            IReadOnlyCollection<int> classIds,
            IReadOnlyDictionary<int, int> classToCourseMap,
            int studentId)
        {
            if (classIds.Count == 0)
            {
                return (new Dictionary<int, int>(), new Dictionary<int, int>());
            }

            // 1. Lấy danh sách lesson_id của từng lớp từ class_lessons
            var classLessons = await Db.ClassLessons.AsNoTracking()
                .Where(cl => classIds.Contains(cl.ClassId))
                .Select(cl => new { cl.ClassId, cl.LessonId })
                .Distinct()
                .ToListAsync();

            var lessonsByClass = classLessons
                .GroupBy(cl => cl.ClassId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.LessonId).ToHashSet());

            // 2. Lấy danh sách lesson_id mà học sinh đã hoàn thành
            var completedLessonIds = (await Db.UserProgresses.AsNoTracking()
                .Where(p => p.UserId == studentId && p.IsCompleted)
                .Select(p => p.LessonId)
                .Distinct()
                .ToListAsync())
                .ToHashSet();

            // 3. Chuẩn bị fallback theo course nếu lớp không có class_lessons
            var courseIds = classToCourseMap.Values.Distinct().ToList();
            var (totalByCourse, completedByCourse) = await GetCourseProgressDictionariesAsync(courseIds, studentId);

            var totalByClass = new Dictionary<int, int>();
            var completedByClass = new Dictionary<int, int>();

            foreach (var classId in classIds)
            {
                if (lessonsByClass.TryGetValue(classId, out var lessonSet) && lessonSet.Count > 0)
                {
                    totalByClass[classId] = lessonSet.Count;
                    completedByClass[classId] = lessonSet.Count(lid => completedLessonIds.Contains(lid));
                }
                else
                {
                    var courseId = classToCourseMap.GetValueOrDefault(classId, 0);
                    totalByClass[classId] = totalByCourse.GetValueOrDefault(courseId, 0);
                    completedByClass[classId] = completedByCourse.GetValueOrDefault(courseId, 0);
                }
            }

            return (totalByClass, completedByClass);
        }

        /// <summary>
        /// Truy vấn gộp (Batch Query) danh sách lịch học cố định hàng tuần theo danh sách ClassId.
        /// Tái sử dụng giữa CourseDetail, MyClassDetail và Enrollment Conflict Check.
        /// </summary>
        protected async Task<Dictionary<int, List<ClassScheduleItemDto>>> GetSchedulesByClassIdsAsync(IReadOnlyCollection<int> classIds)
        {
            if (classIds.Count == 0)
            {
                return new Dictionary<int, List<ClassScheduleItemDto>>();
            }

            var rawSchedules = await (
                from cs in Db.ClassSchedules.AsNoTracking()
                join ts in Db.TimeSlots.AsNoTracking() on cs.TimeSlotId equals ts.Id into tsGroup
                from ts in tsGroup.DefaultIfEmpty()
                join cr in Db.Classrooms.AsNoTracking() on cs.ClassroomId equals cr.Id into crGroup
                from cr in crGroup.DefaultIfEmpty()
                where classIds.Contains(cs.ClassId)
                orderby cs.DayOfWeek, cs.TimeSlotId
                select new
                {
                    cs.ClassId,
                    cs.Id,
                    cs.DayOfWeek,
                    cs.TimeSlotId,
                    TimeSlotName = ts != null ? ts.Name : "Ca học tiêu chuẩn",
                    RawStartTime = (TimeSpan?)ts.StartTime,
                    RawEndTime = (TimeSpan?)ts.EndTime,
                    cs.ClassroomId,
                    ClassroomName = cr != null ? cr.Name : "Phòng trực tuyến / Zoom",
                    Location = cr != null ? cr.Location : "Online"
                }
            ).ToListAsync();

            return rawSchedules
                .GroupBy(x => x.ClassId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x =>
                    {
                        var scheduleDay = IsoToScheduleDayOfWeek(x.DayOfWeek);
                        return new ClassScheduleItemDto
                        {
                            ScheduleId = x.Id,
                            DayOfWeek = scheduleDay,
                            DayName = GetDayOfWeekName(scheduleDay),
                            TimeSlotId = x.TimeSlotId,
                            TimeSlotName = x.TimeSlotName,
                            TimeRange = FormatTimeRange(x.RawStartTime ?? new TimeSpan(18, 0, 0), x.RawEndTime ?? new TimeSpan(20, 0, 0)),
                            ClassroomId = x.ClassroomId,
                            ClassroomName = x.ClassroomName,
                            Location = x.Location
                        };
                    }).ToList()
                );
        }

        /// <summary>
        /// Truy vấn gộp (Batch Query) danh sách giảng viên phụ trách theo danh sách ClassId.
        /// Tái sử dụng giữa MyClasses và MyClassDetail.
        /// </summary>
        protected async Task<Dictionary<int, List<ClassTeacherInfoDto>>> GetTeachersByClassIdsAsync(IReadOnlyCollection<int> classIds)
        {
            if (classIds.Count == 0)
            {
                return new Dictionary<int, List<ClassTeacherInfoDto>>();
            }

            var rawTeachers = await (
                from ct in Db.ClassTeachers.AsNoTracking()
                join t in Db.Teachers.AsNoTracking() on ct.TeacherId equals t.Id
                join u in Db.Users.AsNoTracking() on t.UserId equals u.Id
                where classIds.Contains(ct.ClassId)
                orderby ct.Role == "main" ? 0 : 1, u.FullName
                select new
                {
                    ct.ClassId,
                    TeacherId = t.Id,
                    u.FullName,
                    u.Email,
                    u.AvatarUrl,
                    t.Qualification,
                    ct.Role
                }
            ).ToListAsync();

            return rawTeachers
                .GroupBy(x => x.ClassId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => new ClassTeacherInfoDto
                    {
                        TeacherId = x.TeacherId,
                        FullName = x.FullName,
                        Email = x.Email,
                        AvatarUrl = x.AvatarUrl,
                        Qualification = x.Qualification,
                        Role = x.Role
                    }).ToList()
                );
        }
    }
}
