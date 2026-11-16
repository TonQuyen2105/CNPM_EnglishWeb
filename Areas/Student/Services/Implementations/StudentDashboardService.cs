using CNPM_EnglishLearning.Areas.Student.DTOs;
using CNPM_EnglishLearning.Areas.Student.DTOs.Common;
using CNPM_EnglishLearning.Areas.Student.Services.Interfaces;
using CNPM_EnglishLearning.Data;
using Microsoft.EntityFrameworkCore;

namespace CNPM_EnglishLearning.Areas.Student.Services.Implementations
{
    /// <summary>
    /// Nhóm 1: Nghiệp vụ Dashboard & Tổng quan học tập của Học sinh.
    /// Kế thừa BaseStudentService để tái sử dụng các phương thức truy vấn gộp (Batch Query).
    /// </summary>
    public class StudentDashboardService : BaseStudentService, IStudentDashboardService
    {
        public StudentDashboardService(ApplicationDbContext db) : base(db)
        {
        }

        public async Task<ServiceResult<DashboardOverviewDto>> GetDashboardOverviewAsync(int studentId)
        {
            var now = DateTime.UtcNow;
            var today = now.Date;

            // 1. Lấy toàn bộ các lớp học đang active của học sinh trong 1 câu truy vấn Join
            var activeEnrollments = await (
                from e in Db.Enrollments.AsNoTracking()
                join cc in Db.CourseClasses.AsNoTracking() on e.ClassId equals cc.Id
                join c in Db.Courses.AsNoTracking() on cc.CourseId equals c.Id
                where e.UserId == studentId && e.Status == EnrollmentActive
                orderby e.EnrolledAt descending
                select new
                {
                    ClassId = cc.Id,
                    cc.ClassName,
                    CourseId = c.Id,
                    CourseTitle = c.Title,
                    c.ThumbnailUrl
                }
            ).ToListAsync();

            // Tái sử dụng danh sách CourseId và ClassId cho các truy vấn tiếp theo
            var enrolledCourseIds = activeEnrollments.Select(x => x.CourseId).Distinct().ToList();
            var enrolledClassIds = activeEnrollments.Select(x => x.ClassId).Distinct().ToList();

            // 2. Truy vấn gộp tiến độ bài học theo danh sách khóa học (0 lỗi N+1)
            var (totalByCourse, completedByCourse) = await GetCourseProgressDictionariesAsync(enrolledCourseIds, studentId);

            var activeCoursesDto = activeEnrollments.Select(item =>
            {
                var totalLessons = totalByCourse.GetValueOrDefault(item.CourseId, 0);
                var completedLessons = completedByCourse.GetValueOrDefault(item.CourseId, 0);
                return new DashboardActiveCourseDto
                {
                    ClassId = item.ClassId,
                    ClassName = item.ClassName,
                    CourseId = item.CourseId,
                    CourseTitle = item.CourseTitle,
                    ThumbnailUrl = item.ThumbnailUrl,
                    TotalLessons = totalLessons,
                    CompletedLessons = completedLessons,
                    ProgressPercentage = CalculatePercentage(completedLessons, totalLessons)
                };
            }).ToList();

            // 3. Đếm số lượng Flashcard đến hạn ôn tập hôm nay (next_review_at <= NOW() hoặc NULL)
            var dueFlashcardsCount = await Db.Flashcards.AsNoTracking()
                .CountAsync(f => f.UserId == studentId && (f.NextReviewAt == null || f.NextReviewAt <= now));

            // 4. Đếm số lượng bài thi chưa hoàn thành (thuộc các khóa đang học hoặc bài thi chung)
            var completedExamIds = await Db.UserExams.AsNoTracking()
                .Where(ue => ue.UserId == studentId && (ue.Status == ExamPassed || ue.Status == ExamSubmitted))
                .Select(ue => ue.ExamId)
                .Distinct()
                .ToListAsync();

            var pendingExamsCount = await Db.Exams.AsNoTracking()
                .CountAsync(ex =>
                    ex.IsPublished &&
                    (ex.CourseId == null || enrolledCourseIds.Contains(ex.CourseId.Value)) &&
                    !completedExamIds.Contains(ex.Id));

            // 5. Lấy buổi học sắp diễn ra gần nhất (ưu tiên bảng class_lessons, fallback sang lịch tuần class_schedules)
            DashboardNextSessionDto? nextSessionDto = null;

            if (enrolledClassIds.Count > 0)
            {
                var upcomingClassLesson = await (
                    from cl in Db.ClassLessons.AsNoTracking()
                    join cc in Db.CourseClasses.AsNoTracking() on cl.ClassId equals cc.Id
                    join l in Db.Lessons.AsNoTracking() on cl.LessonId equals l.Id into lGroup
                    from l in lGroup.DefaultIfEmpty()
                    join cs in Db.ClassSchedules.AsNoTracking() on cl.ScheduleId equals cs.Id into csGroup
                    from cs in csGroup.DefaultIfEmpty()
                    join ts in Db.TimeSlots.AsNoTracking() on cs.TimeSlotId equals ts.Id into tsGroup
                    from ts in tsGroup.DefaultIfEmpty()
                    join cr in Db.Classrooms.AsNoTracking() on cs.ClassroomId equals cr.Id into crGroup
                    from cr in crGroup.DefaultIfEmpty()
                    where enrolledClassIds.Contains(cl.ClassId)
                          && cl.Status == SessionScheduled
                          && (cl.ScheduleDate == null || cl.ScheduleDate >= today)
                    orderby cl.ScheduleDate ?? DateTime.MaxValue, cl.Id
                    select new
                    {
                        ClassLessonId = cl.Id,
                        cl.ClassId,
                        cc.ClassName,
                        cl.LessonId,
                        LessonTitle = l != null ? l.Title : "Buổi học định kỳ",
                        cl.ScheduleDate,
                        StartTime = ts != null ? ts.StartTime : new TimeSpan(18, 0, 0),
                        EndTime = ts != null ? ts.EndTime : new TimeSpan(20, 0, 0),
                        ClassroomName = cr != null ? cr.Name : "Phòng học trực tuyến / Zoom"
                    }
                ).FirstOrDefaultAsync();

                if (upcomingClassLesson != null)
                {
                    var sessionDate = upcomingClassLesson.ScheduleDate ?? today.AddDays(1);
                    nextSessionDto = new DashboardNextSessionDto
                    {
                        ClassLessonId = upcomingClassLesson.ClassLessonId,
                        ClassId = upcomingClassLesson.ClassId,
                        ClassName = upcomingClassLesson.ClassName,
                        LessonId = upcomingClassLesson.LessonId,
                        LessonTitle = upcomingClassLesson.LessonTitle,
                        ScheduleDate = sessionDate.ToString("yyyy-MM-dd"),
                        TimeSlot = FormatTimeRange(upcomingClassLesson.StartTime, upcomingClassLesson.EndTime),
                        Classroom = upcomingClassLesson.ClassroomName
                    };
                }
                else
                {
                    // Fallback thông minh từ lịch cố định hàng tuần (class_schedules) nếu chưa tạo bản ghi class_lessons
                    var schedulesByClass = await GetSchedulesByClassIdsAsync(enrolledClassIds);
                    var firstClassWithSchedule = activeEnrollments.FirstOrDefault(c => schedulesByClass.ContainsKey(c.ClassId));

                    if (firstClassWithSchedule != null && schedulesByClass.TryGetValue(firstClassWithSchedule.ClassId, out var classSchedules) && classSchedules.Count > 0)
                    {
                        var nextLesson = await Db.Lessons.AsNoTracking()
                            .Where(l => l.CourseId == firstClassWithSchedule.CourseId)
                            .OrderBy(l => l.OrderIndex)
                            .FirstOrDefaultAsync();

                        var firstSchedule = classSchedules[0];
                        nextSessionDto = new DashboardNextSessionDto
                        {
                            ClassLessonId = firstSchedule.ScheduleId,
                            ClassId = firstClassWithSchedule.ClassId,
                            ClassName = firstClassWithSchedule.ClassName,
                            LessonId = nextLesson?.Id ?? 0,
                            LessonTitle = nextLesson?.Title ?? $"Buổi học lớp {firstClassWithSchedule.ClassName}",
                            ScheduleDate = GetNextOccurrenceDate(today, firstSchedule.DayOfWeek).ToString("yyyy-MM-dd"),
                            TimeSlot = firstSchedule.TimeRange,
                            Classroom = firstSchedule.ClassroomName
                        };
                    }
                }
            }

            var response = new DashboardOverviewDto
            {
                ActiveCourses = activeCoursesDto,
                QuickReview = new DashboardQuickReviewDto
                {
                    DueFlashcardsCount = dueFlashcardsCount,
                    PendingExamsCount = pendingExamsCount
                },
                NextSession = nextSessionDto
            };

            return ServiceResult<DashboardOverviewDto>.Ok(response, "Lấy dữ liệu tổng quan trang chủ học sinh thành công.");
        }

        private static DateTime GetNextOccurrenceDate(DateTime fromDate, int targetScheduleDayOfWeek)
        {
            for (var offset = 0; offset < 7; offset++)
            {
                var candidate = fromDate.AddDays(offset);
                if (ToScheduleDayOfWeek(candidate.DayOfWeek) == targetScheduleDayOfWeek)
                {
                    return candidate;
                }
            }
            return fromDate.AddDays(1);
        }
    }
}
