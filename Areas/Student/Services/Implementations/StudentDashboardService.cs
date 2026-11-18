using CNPM_EnglishLearning.Areas.Student.DTOs;
using CNPM_EnglishLearning.Areas.Student.DTOs.Common;
using CNPM_EnglishLearning.Areas.Student.Services.Interfaces;
using CNPM_EnglishLearning.Data;
using CNPM_EnglishLearning.Models.Entities;
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

            // 1. Lấy thông tin tài khoản học sinh
            var studentUser = await Db.Users.AsNoTracking()
                .Where(u => u.Id == studentId)
                .Select(u => new
                {
                    u.Id,
                    u.FullName,
                    u.Email,
                    u.AvatarUrl
                })
                .FirstOrDefaultAsync();

            var profileDto = studentUser != null ? new DashboardStudentProfileDto
            {
                Id = studentUser.Id,
                FullName = studentUser.FullName,
                Email = studentUser.Email,
                AvatarUrl = studentUser.AvatarUrl,
                StreakDays = 5
            } : null;

            // 2. Lấy toàn bộ các lớp học đang active của học sinh
            var activeEnrollments = await (
                from e in Db.Enrollments.AsNoTracking()
                join cc in Db.CourseClasses.AsNoTracking() on e.ClassId equals cc.Id
                join c in Db.Courses.AsNoTracking() on cc.CourseId equals c.Id
                where e.UserId == studentId && e.Status == EnrollmentActive
                orderby e.EnrolledAt descending
                select new
                {
                    ClassId = cc.Id,
                    cc.ClassCode,
                    cc.ClassName,
                    CourseId = c.Id,
                    CourseTitle = c.Title,
                    c.ThumbnailUrl
                }
            ).ToListAsync();

            var enrolledCourseIds = activeEnrollments.Select(x => x.CourseId).Distinct().ToList();
            var enrolledClassIds = activeEnrollments.Select(x => x.ClassId).Distinct().ToList();
            var classToCourseMap = activeEnrollments
                .GroupBy(x => x.ClassId)
                .ToDictionary(g => g.Key, g => g.First().CourseId);

            // 3. Batch queries: tiến độ bài học theo lớp, giáo viên, lịch học
            var (totalByClass, completedByClass) = await GetClassProgressDictionariesAsync(enrolledClassIds, classToCourseMap, studentId);
            var teachersByClass = await GetTeachersByClassIdsAsync(enrolledClassIds);
            var schedulesByClass = await GetSchedulesByClassIdsAsync(enrolledClassIds);

            // Lấy danh sách lesson của class_lessons để ưu tiên thứ tự bài học theo lớp
            var classLessonEntities = await (
                from cl in Db.ClassLessons.AsNoTracking()
                join l in Db.Lessons.AsNoTracking() on cl.LessonId equals l.Id
                where enrolledClassIds.Contains(cl.ClassId)
                orderby cl.ScheduleDate, l.OrderIndex
                select new { cl.ClassId, Lesson = l }
            ).ToListAsync();

            var classLessonsMap = classLessonEntities
                .GroupBy(x => x.ClassId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Lesson).DistinctBy(l => l.Id).ToList());

            var allCourseLessons = enrolledCourseIds.Count == 0
                ? new List<Lesson>()
                : await Db.Lessons.AsNoTracking()
                    .Where(l => enrolledCourseIds.Contains(l.CourseId))
                    .OrderBy(l => l.OrderIndex)
                    .ThenBy(l => l.Id)
                    .ToListAsync();

            var completedLessonIds = (await Db.UserProgresses.AsNoTracking()
                .Where(p => p.UserId == studentId && p.IsCompleted)
                .Select(p => p.LessonId)
                .ToListAsync())
                .ToHashSet();

            var activeCoursesDto = activeEnrollments.Select(item =>
            {
                var totalLessons = totalByClass.GetValueOrDefault(item.ClassId, 0);
                var completedLessons = completedByClass.GetValueOrDefault(item.ClassId, 0);

                var targetLessons = classLessonsMap.TryGetValue(item.ClassId, out var cLessons) && cLessons.Count > 0
                    ? cLessons
                    : allCourseLessons.Where(l => l.CourseId == item.CourseId).ToList();

                var nextLesson = targetLessons.FirstOrDefault(l => !completedLessonIds.Contains(l.Id)) ?? targetLessons.LastOrDefault();

                var classTeachers = teachersByClass.GetValueOrDefault(item.ClassId);
                var teacherName = classTeachers?.FirstOrDefault(t => t.Role == "main")?.FullName ?? classTeachers?.FirstOrDefault()?.FullName;

                var classSchedules = schedulesByClass.GetValueOrDefault(item.ClassId);
                var scheduleSummary = classSchedules != null && classSchedules.Count > 0
                    ? string.Join(", ", classSchedules.Select(s => $"{s.DayName} {s.TimeRange}"))
                    : null;

                return new DashboardActiveCourseDto
                {
                    ClassId = item.ClassId,
                    ClassCode = item.ClassCode,
                    ClassName = item.ClassName,
                    CourseId = item.CourseId,
                    CourseTitle = item.CourseTitle,
                    TeacherName = teacherName,
                    ScheduleSummary = scheduleSummary,
                    ThumbnailUrl = item.ThumbnailUrl,
                    TotalLessons = totalLessons,
                    CompletedLessons = completedLessons,
                    ProgressPercentage = CalculatePercentage(completedLessons, totalLessons),
                    NextLessonId = nextLesson?.Id,
                    NextLessonTitle = nextLesson?.Title
                };
            }).ToList();

            // 4. Thống kê chỉ số (Stats)
            var masteredCount = await Db.Flashcards.AsNoTracking()
                .CountAsync(f => f.UserId == studentId && f.IntervalDays >= 1);

            var progressScores = await Db.UserProgresses.AsNoTracking()
                .Where(p => p.UserId == studentId && p.Score.HasValue)
                .Select(p => (double)p.Score!.Value)
                .ToListAsync();

            var examScores = await Db.UserExams.AsNoTracking()
                .Where(ue => ue.UserId == studentId && ue.TotalScore.HasValue)
                .Select(ue => (double)ue.TotalScore!.Value)
                .ToListAsync();

            var allScores = progressScores.Concat(examScores).ToList();
            var avgScore = allScores.Count > 0 ? Math.Round(allScores.Average(), 1) : 8.5;
            if (avgScore > 10.0)
            {
                avgScore = Math.Round(avgScore / 10.0, 1);
            }

            var scoreRank = avgScore switch
            {
                >= 9.0 => "Xuất sắc",
                >= 8.0 => "Giỏi",
                >= 6.5 => "Khá",
                >= 5.0 => "Trung bình",
                _ => "Cần cố gắng"
            };

            var enrolledClassNames = string.Join(" & ", activeEnrollments.Select(x => !string.IsNullOrEmpty(x.ClassCode) ? x.ClassCode : x.ClassName));

            var allEnrolledLessonIds = classLessonEntities.Count > 0
                ? classLessonEntities.Select(x => x.Lesson.Id).Distinct().ToHashSet()
                : allCourseLessons.Select(l => l.Id).Distinct().ToHashSet();

            var totalEnrolledLessonsCount = allEnrolledLessonIds.Count > 0 ? allEnrolledLessonIds.Count : allCourseLessons.Count;
            var totalEnrolledLessonsCompleted = allEnrolledLessonIds.Count > 0 
                ? allEnrolledLessonIds.Count(lid => completedLessonIds.Contains(lid)) 
                : completedLessonIds.Count;

            var statsDto = new DashboardStatsDto
            {
                EnrolledClassesCount = activeEnrollments.Count,
                EnrolledClassNames = string.IsNullOrEmpty(enrolledClassNames) ? "Chưa tham gia lớp nào" : enrolledClassNames,
                MasteredVocabularies = masteredCount,
                AverageScore = avgScore,
                ScoreRank = scoreRank,
                TotalLessonsCompleted = totalEnrolledLessonsCompleted,
                TotalLessons = totalEnrolledLessonsCount,
                AttendanceRate = 100
            };

            // 5. Đếm Flashcard đến hạn & đề thi chờ làm
            var dueFlashcardsCount = await Db.Flashcards.AsNoTracking()
                .CountAsync(f => f.UserId == studentId && (f.NextReviewAt == null || f.NextReviewAt <= now));

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

            // 6. Buổi học sắp diễn ra gần nhất
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
                    orderby cl.ScheduleDate, cl.Id
                    select new
                    {
                        ClassLessonId = cl.Id,
                        cl.ClassId,
                        cc.ClassName,
                        cl.LessonId,
                        LessonTitle = l != null ? l.Title : "Buổi học định kỳ",
                        cl.ScheduleDate,
                        RawStartTime = (TimeSpan?)ts.StartTime,
                        RawEndTime = (TimeSpan?)ts.EndTime,
                        ClassroomName = cr != null ? cr.Name : "Phòng học trực tuyến / Zoom"
                    }
                ).FirstOrDefaultAsync();

                if (upcomingClassLesson != null)
                {
                    var sessionDate = upcomingClassLesson.ScheduleDate ?? today.AddDays(1);
                    var startTime = upcomingClassLesson.RawStartTime ?? new TimeSpan(18, 0, 0);
                    var endTime = upcomingClassLesson.RawEndTime ?? new TimeSpan(20, 0, 0);
                    nextSessionDto = new DashboardNextSessionDto
                    {
                        ClassLessonId = upcomingClassLesson.ClassLessonId,
                        ClassId = upcomingClassLesson.ClassId,
                        ClassName = upcomingClassLesson.ClassName,
                        LessonId = upcomingClassLesson.LessonId,
                        LessonTitle = upcomingClassLesson.LessonTitle,
                        ScheduleDate = sessionDate.ToString("yyyy-MM-dd"),
                        TimeSlot = FormatTimeRange(startTime, endTime),
                        Classroom = upcomingClassLesson.ClassroomName
                    };
                }
                else
                {
                    // Fallback thông minh từ lịch cố định hàng tuần (class_schedules) nếu chưa tạo bản ghi class_lessons
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
                StudentProfile = profileDto,
                Stats = statsDto,
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
