using CNPM_EnglishLearning.Areas.Student.DTOs;
using CNPM_EnglishLearning.Data;
using Microsoft.AspNetCore.Mvc;

namespace CNPM_EnglishLearning.Areas.Student.Controllers.Api
{
    /// <summary>
    /// Nhóm 5: API Học từ vựng với Flashcards (Spaced Repetition - SRS).
    /// </summary>
    public class FlashcardsApiController : BaseStudentApiController
    {
        public FlashcardsApiController(ApplicationDbContext db) : base(db)
        {
        }

        /// <summary>
        /// 5.1. Lấy danh sách thẻ cần ôn tập hôm nay (mode=due) hoặc tất cả thẻ (mode=all).
        /// Endpoint: GET /api/v1/student/flashcards?mode=due
        /// </summary>
        [HttpGet("flashcards")]
        public async Task<IActionResult> GetFlashcards([FromQuery(Name = "mode")] string? mode = "due")
        {
            var studentId = await ResolveStudentIdAsync();
            var result = await Services.Flashcards.GetFlashcardsAsync(studentId, mode);
            return ToActionResult(result);
        }

        /// <summary>
        /// 5.2. Thêm thẻ Flashcard mới.
        /// Endpoint: POST /api/v1/student/flashcards
        /// </summary>
        [HttpPost("flashcards")]
        public async Task<IActionResult> CreateFlashcard([FromBody] UpsertFlashcardRequestDto request)
        {
            var studentId = await ResolveStudentIdAsync();
            var result = await Services.Flashcards.CreateFlashcardAsync(studentId, request);
            return ToActionResult(result);
        }

        /// <summary>
        /// 5.3. Cập nhật trạng thái ôn tập thẻ (Đã thuộc: "remembered" | Cần ôn lại: "forgot").
        /// Endpoint: PATCH /api/v1/student/flashcards/{flashcardId}/review
        /// </summary>
        [HttpPatch("flashcards/{flashcardId:int}/review")]
        public async Task<IActionResult> ReviewFlashcard(
            [FromRoute] int flashcardId,
            [FromBody] ReviewFlashcardRequestDto request)
        {
            var studentId = await ResolveStudentIdAsync();
            var result = await Services.Flashcards.ReviewFlashcardAsync(studentId, flashcardId, request);
            return ToActionResult(result);
        }

        /// <summary>
        /// 5.4a. Chỉnh sửa thẻ Flashcard.
        /// Endpoint: PUT /api/v1/student/flashcards/{flashcardId}
        /// </summary>
        [HttpPut("flashcards/{flashcardId:int}")]
        public async Task<IActionResult> UpdateFlashcard(
            [FromRoute] int flashcardId,
            [FromBody] UpsertFlashcardRequestDto request)
        {
            var studentId = await ResolveStudentIdAsync();
            var result = await Services.Flashcards.UpdateFlashcardAsync(studentId, flashcardId, request);
            return ToActionResult(result);
        }

        /// <summary>
        /// 5.4b. Xóa thẻ Flashcard.
        /// Endpoint: DELETE /api/v1/student/flashcards/{flashcardId}
        /// </summary>
        [HttpDelete("flashcards/{flashcardId:int}")]
        public async Task<IActionResult> DeleteFlashcard([FromRoute] int flashcardId)
        {
            var studentId = await ResolveStudentIdAsync();
            var result = await Services.Flashcards.DeleteFlashcardAsync(studentId, flashcardId);
            return ToActionResult(result);
        }
    }
}
