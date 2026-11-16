using CNPM_EnglishLearning.Areas.Student.DTOs;
using CNPM_EnglishLearning.Areas.Student.DTOs.Common;
using CNPM_EnglishLearning.Areas.Student.Services.Interfaces;
using CNPM_EnglishLearning.Data;
using Microsoft.EntityFrameworkCore;

namespace CNPM_EnglishLearning.Areas.Student.Services.Implementations
{
    /// <summary>
    /// Nhóm 7: Nghiệp vụ Thống kê Năng lực 4 Kỹ năng & Các chỉ số học tập tích lũy.
    /// </summary>
    public class StudentAnalyticsService : BaseStudentService, IStudentAnalyticsService
    {
        public StudentAnalyticsService(ApplicationDbContext db) : base(db)
        {
        }

        public async Task<ServiceResult<AnalyticsOverviewDto>> GetAnalyticsOverviewAsync(int studentId)
        {
            // 1. Truy vấn gộp (GroupBy) tiến độ và điểm trung bình theo từng kỹ năng (content_type) trong 1 câu SQL
            var skillProgressGroups = await (
                from p in Db.UserProgresses.AsNoTracking()
                join l in Db.Lessons.AsNoTracking() on p.LessonId equals l.Id
                where p.UserId == studentId && p.IsCompleted
                group p by l.ContentType.ToLower() into g
                select new
                {
                    ContentType = g.Key,
                    CompletedCount = g.Select(x => x.LessonId).Distinct().Count(),
                    AverageScore = g.Average(x => (double?)(x.Score ?? 100m)) ?? 0.0
                }
            ).ToListAsync();

            var skillDict = skillProgressGroups.ToDictionary(
                x => x.ContentType,
                x => new SkillMetricDto
                {
                    CompletedLessons = x.CompletedCount,
                    AverageScore = Math.Round(x.AverageScore, 1)
                },
                StringComparer.OrdinalIgnoreCase);

            var skillsProficiency = new SkillsProficiencyDto
            {
                Listening = skillDict.GetValueOrDefault("listening", new SkillMetricDto()),
                Reading = skillDict.GetValueOrDefault("reading", new SkillMetricDto()),
                Writing = skillDict.GetValueOrDefault("writing", new SkillMetricDto()),
                Speaking = skillDict.GetValueOrDefault("speaking", new SkillMetricDto())
            };

            // 2. Tính các chỉ số tích lũy (Accumulated Stats)
            var totalCoursesEnrolled = await (
                from e in Db.Enrollments.AsNoTracking()
                join cc in Db.CourseClasses.AsNoTracking() on e.ClassId equals cc.Id
                where e.UserId == studentId && e.Status != EnrollmentCancelled
                select cc.CourseId
            ).Distinct().CountAsync();

            var totalLessonsCompleted = skillProgressGroups.Sum(x => x.CompletedCount);

            var totalExamsPassed = await Db.UserExams.AsNoTracking()
                .Where(ue => ue.UserId == studentId && ue.Status == ExamPassed)
                .Select(ue => ue.ExamId)
                .Distinct()
                .CountAsync();

            var totalFlashcardsMastered = await Db.Flashcards.AsNoTracking()
                .CountAsync(f => f.UserId == studentId && f.IntervalDays >= 1);

            var response = new AnalyticsOverviewDto
            {
                SkillsProficiency = skillsProficiency,
                AccumulatedStats = new AccumulatedStatsDto
                {
                    TotalCoursesEnrolled = totalCoursesEnrolled,
                    TotalLessonsCompleted = totalLessonsCompleted,
                    TotalExamsPassed = totalExamsPassed,
                    TotalFlashcardsMastered = totalFlashcardsMastered
                }
            };

            return ServiceResult<AnalyticsOverviewDto>.Ok(
                response,
                "Lấy báo cáo phân tích 4 kỹ năng và chỉ số học tập tích lũy thành công.");
        }
    }
}
