using CNPM_EnglishLearning.Areas.Student.DTOs;
using CNPM_EnglishLearning.Areas.Student.DTOs.Common;
using CNPM_EnglishLearning.Areas.Student.Models;
using CNPM_EnglishLearning.Areas.Student.Services.Interfaces;
using CNPM_EnglishLearning.Data;
using CNPM_EnglishLearning.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace CNPM_EnglishLearning.Areas.Student.Services.Implementations
{
    /// <summary>
    /// Nhóm 4: Nghiệp vụ Nội dung Bài học & Tiến độ tự học (Self-study).
    /// </summary>
    public class StudentLearningService : BaseStudentService, IStudentLearningService
    {
        public StudentLearningService(ApplicationDbContext db) : base(db)
        {
        }

        /// <summary>
        /// 4.1. Lấy mục lục bài học của một khóa học kèm cờ hoàn thành của học sinh.
        /// </summary>
        public async Task<ServiceResult<CourseSyllabusResponseDto>> GetCourseSyllabusAsync(int studentId, int courseId)
        {
            var course = await Db.Courses.AsNoTracking()
                .Where(c => c.Id == courseId)
                .Select(c => new { c.Id, c.Title })
                .FirstOrDefaultAsync();

            if (course == null)
            {
                return ServiceResult<CourseSyllabusResponseDto>.NotFound($"Không tìm thấy khóa học có ID = {courseId}.");
            }

            var lessons = await Db.Lessons.AsNoTracking()
                .Where(l => l.CourseId == courseId)
                .OrderBy(l => l.OrderIndex)
                .ThenBy(l => l.Id)
                .ToListAsync();

            var lessonIds = lessons.Select(l => l.Id).ToList();
            var progressMap = lessonIds.Count == 0
                ? new Dictionary<int, UserProgress>()
                : (await Db.UserProgresses.AsNoTracking()
                    .Where(p => p.UserId == studentId && lessonIds.Contains(p.LessonId))
                    .ToListAsync())
                    .GroupBy(p => p.LessonId)
                    .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.CompletedAt).First());

            var lessonDtos = lessons.Select(l =>
            {
                progressMap.TryGetValue(l.Id, out var prog);
                return new CourseSyllabusLessonItemDto
                {
                    LessonId = l.Id,
                    Title = l.Title,
                    OrderIndex = l.OrderIndex,
                    ContentType = l.ContentType,
                    IsCompleted = prog?.IsCompleted ?? false,
                    Score = prog?.Score,
                    CompletedAt = prog?.CompletedAt
                };
            }).ToList();

            var totalLessons = lessonDtos.Count;
            var completedLessons = lessonDtos.Count(x => x.IsCompleted);

            var response = new CourseSyllabusResponseDto
            {
                CourseId = course.Id,
                CourseTitle = course.Title,
                TotalLessons = totalLessons,
                CompletedLessons = completedLessons,
                ProgressPercentage = CalculatePercentage(completedLessons, totalLessons),
                Lessons = lessonDtos
            };

            return ServiceResult<CourseSyllabusResponseDto>.Ok(response, "Lấy mục lục bài học thành công.");
        }

        /// <summary>
        /// 4.2. Xem chi tiết nội dung bài học kèm câu hỏi luyện tập.
        /// </summary>
        public async Task<ServiceResult<LessonDetailDto>> GetLessonDetailAsync(int studentId, int lessonId)
        {
            var lessonInfo = await (
                from l in Db.Lessons.AsNoTracking()
                join c in Db.Courses.AsNoTracking() on l.CourseId equals c.Id into cGroup
                from c in cGroup.DefaultIfEmpty()
                where l.Id == lessonId
                select new
                {
                    Lesson = l,
                    CourseTitle = c != null ? c.Title : string.Empty
                }
            ).FirstOrDefaultAsync();

            if (lessonInfo == null)
            {
                return ServiceResult<LessonDetailDto>.NotFound($"Không tìm thấy bài học có ID = {lessonId}.");
            }

            var lesson = lessonInfo.Lesson;

            // 1. Lấy trạng thái tiến độ của học sinh với bài học này
            var progress = await Db.UserProgresses.AsNoTracking()
                .Where(p => p.UserId == studentId && p.LessonId == lessonId)
                .OrderByDescending(p => p.CompletedAt)
                .FirstOrDefaultAsync();

            // 2. Lấy danh sách câu hỏi luyện tập đi kèm bài học và batch-load các lựa chọn (options)
            var questions = await Db.Questions.AsNoTracking()
                .Where(q => q.LessonId == lessonId)
                .OrderBy(q => q.Id)
                .ToListAsync();

            var questionIds = questions.Select(q => q.Id).ToList();
            var optionsByQuestion = questionIds.Count == 0
                ? new Dictionary<int, List<QuestionOptionSafeDto>>()
                : (await Db.QuestionOptions.AsNoTracking()
                    .Where(opt => questionIds.Contains(opt.QuestionId))
                    .OrderBy(opt => opt.Id)
                    .Select(opt => new QuestionOptionSafeDto
                    {
                        Id = opt.Id,
                        QuestionId = opt.QuestionId,
                        OptionText = opt.OptionText
                    })
                    .ToListAsync())
                    .GroupBy(opt => opt.QuestionId)
                    .ToDictionary(g => g.Key, g => g.ToList());

            var practiceQuestionDtos = questions.Select(q => new LessonPracticeQuestionDto
            {
                QuestionId = q.Id,
                QuestionText = q.QuestionText,
                AudioUrl = q.AudioUrl,
                QuestionType = q.QuestionType,
                DefaultPoints = q.DefaultPoints,
                Options = optionsByQuestion.GetValueOrDefault(q.Id, new List<QuestionOptionSafeDto>())
            }).ToList();

            var detailDto = new LessonDetailDto
            {
                Id = lesson.Id,
                CourseId = lesson.CourseId,
                CourseTitle = lessonInfo.CourseTitle,
                Title = lesson.Title,
                OrderIndex = lesson.OrderIndex,
                ContentType = lesson.ContentType,
                Content = lesson.Content,
                IsCompleted = progress?.IsCompleted ?? false,
                Score = progress?.Score,
                CompletedAt = progress?.CompletedAt,
                Attachments = StudentExtendedStore.GetLessonAttachments(lesson.Id, lesson.Title, lesson.ContentType),
                PracticeQuestions = practiceQuestionDtos
            };

            return ServiceResult<LessonDetailDto>.Ok(detailDto, "Lấy chi tiết nội dung bài học thành công.");
        }

        /// <summary>
        /// 4.3. Đánh dấu hoàn thành bài học (Upsert bảng user_progress).
        /// </summary>
        public async Task<ServiceResult<CompleteLessonResponseDto>> CompleteLessonAsync(
            int studentId,
            int lessonId,
            CompleteLessonRequestDto request)
        {
            var lessonExists = await Db.Lessons.AsNoTracking().AnyAsync(l => l.Id == lessonId);
            if (!lessonExists)
            {
                return ServiceResult<CompleteLessonResponseDto>.NotFound($"Không tìm thấy bài học có ID = {lessonId}.");
            }

            var score = request.Score.HasValue ? Math.Clamp(request.Score.Value, 0m, 100m) : 100m;
            var isCompleted = request.IsCompleted ?? true;
            var now = DateTime.UtcNow;

            var progressEntity = await Db.UserProgresses
                .FirstOrDefaultAsync(p => p.UserId == studentId && p.LessonId == lessonId);

            if (progressEntity != null)
            {
                progressEntity.IsCompleted = isCompleted;
                progressEntity.Score = isCompleted ? score : null;
                progressEntity.CompletedAt = isCompleted ? now : null;
            }
            else
            {
                var nextId = await GetNextIdAsync(Db.UserProgresses, p => p.Id);
                progressEntity = new UserProgress
                {
                    Id = nextId,
                    UserId = studentId,
                    LessonId = lessonId,
                    Score = isCompleted ? score : null,
                    IsCompleted = isCompleted,
                    CompletedAt = isCompleted ? now : null
                };
                Db.UserProgresses.Add(progressEntity);
            }

            await Db.SaveChangesAsync();

            var response = new CompleteLessonResponseDto
            {
                UserProgressId = progressEntity.Id,
                UserId = progressEntity.UserId,
                LessonId = progressEntity.LessonId,
                IsCompleted = progressEntity.IsCompleted,
                Score = progressEntity.Score,
                CompletedAt = progressEntity.CompletedAt
            };

            return ServiceResult<CompleteLessonResponseDto>.Ok(response, "Đánh dấu hoàn thành bài học thành công.");
        }
    }
}
