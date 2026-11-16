using CNPM_EnglishLearning.Areas.Student.DTOs;
using CNPM_EnglishLearning.Data;
using Microsoft.AspNetCore.Mvc;

namespace CNPM_EnglishLearning.Areas.Student.Controllers.Api
{
    /// <summary>
    /// Nhóm 4: API Nội dung Bài học & Đánh dấu Tiến độ (Self-study).
    /// </summary>
    public class LessonsApiController : BaseStudentApiController
    {
        public LessonsApiController(ApplicationDbContext db) : base(db)
        {
        }

        /// <summary>
        /// 4.2. Xem chi tiết nội dung bài học.
        /// Endpoint: GET /api/v1/student/lessons/{lessonId}
        /// </summary>
        [HttpGet("lessons/{lessonId:int}")]
        public async Task<IActionResult> GetLessonDetail([FromRoute] int lessonId)
        {
            var studentId = await ResolveStudentIdAsync();
            var result = await Services.Learning.GetLessonDetailAsync(studentId, lessonId);
            return ToActionResult(result);
        }

        /// <summary>
        /// 4.3. Đánh dấu hoàn thành bài học.
        /// Endpoint: POST /api/v1/student/lessons/{lessonId}/complete
        /// </summary>
        [HttpPost("lessons/{lessonId:int}/complete")]
        public async Task<IActionResult> CompleteLesson(
            [FromRoute] int lessonId,
            [FromBody] CompleteLessonRequestDto? request)
        {
            var studentId = await ResolveStudentIdAsync();
            var safeRequest = request ?? new CompleteLessonRequestDto { Score = 100 };
            var result = await Services.Learning.CompleteLessonAsync(studentId, lessonId, safeRequest);
            return ToActionResult(result);
        }
    }
}
