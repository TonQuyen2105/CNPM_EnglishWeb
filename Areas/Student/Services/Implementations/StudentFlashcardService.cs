using CNPM_EnglishLearning.Areas.Student.DTOs;
using CNPM_EnglishLearning.Areas.Student.DTOs.Common;
using CNPM_EnglishLearning.Areas.Student.Services.Interfaces;
using CNPM_EnglishLearning.Data;
using CNPM_EnglishLearning.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace CNPM_EnglishLearning.Areas.Student.Services.Implementations
{
    /// <summary>
    /// Nhóm 5: Nghiệp vụ Học từ vựng với Flashcards tích hợp thuật toán Spaced Repetition (SRS).
    /// </summary>
    public class StudentFlashcardService : BaseStudentService, IStudentFlashcardService
    {
        private const string ReviewStatusRemembered = "remembered";
        private const string ReviewStatusForgot = "forgot";

        public StudentFlashcardService(ApplicationDbContext db) : base(db)
        {
        }

        /// <summary>
        /// 5.1. Lấy danh sách thẻ cần ôn tập hôm nay (mode = due) hoặc toàn bộ kho thẻ (mode = all).
        /// </summary>
        public async Task<ServiceResult<List<FlashcardItemDto>>> GetFlashcardsAsync(int studentId, string? mode)
        {
            var now = DateTime.UtcNow;
            var normalizedMode = string.IsNullOrWhiteSpace(mode) ? "due" : mode.Trim().ToLowerInvariant();

            var query = Db.Flashcards.AsNoTracking().Where(f => f.UserId == studentId);

            if (normalizedMode == "due")
            {
                query = query.Where(f => f.NextReviewAt == null || f.NextReviewAt <= now);
            }

            var cards = await query
                .OrderBy(f => f.NextReviewAt ?? DateTime.MinValue)
                .ThenByDescending(f => f.CreatedAt)
                .Select(f => MapToDto(f, now))
                .ToListAsync();

            return ServiceResult<List<FlashcardItemDto>>.Ok(cards, "Lấy danh sách thẻ Flashcard thành công.");
        }

        /// <summary>
        /// 5.2. Thêm thẻ Flashcard mới.
        /// </summary>
        public async Task<ServiceResult<FlashcardItemDto>> CreateFlashcardAsync(int studentId, UpsertFlashcardRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Word) || string.IsNullOrWhiteSpace(request.Meaning))
            {
                return ServiceResult<FlashcardItemDto>.BadRequest("Từ vựng (word) và nghĩa (meaning) không được để trống.");
            }

            var now = DateTime.UtcNow;
            var nextId = await GetNextIdAsync(Db.Flashcards, f => f.Id);

            var entity = new Flashcard
            {
                Id = nextId,
                UserId = studentId,
                Word = request.Word.Trim(),
                Phonetic = request.Phonetic?.Trim(),
                Meaning = request.Meaning.Trim(),
                ExampleSentence = request.ExampleSentence?.Trim(),
                AudioUrl = request.AudioUrl?.Trim(),
                IntervalDays = 1,
                NextReviewAt = now,
                CreatedAt = now
            };

            Db.Flashcards.Add(entity);
            await Db.SaveChangesAsync();

            return ServiceResult<FlashcardItemDto>.Created(MapToDto(entity, now), "Thêm thẻ Flashcard mới thành công.");
        }

        /// <summary>
        /// 5.3. Cập nhật trạng thái ôn tập thẻ theo thuật toán Spaced Repetition (remembered / forgot).
        /// </summary>
        public async Task<ServiceResult<FlashcardItemDto>> ReviewFlashcardAsync(
            int studentId,
            int flashcardId,
            ReviewFlashcardRequestDto request)
        {
            var status = request.Status?.Trim().ToLowerInvariant();
            if (status != ReviewStatusRemembered && status != ReviewStatusForgot)
            {
                return ServiceResult<FlashcardItemDto>.BadRequest(
                    "Giá trị status không hợp lệ. Chỉ chấp nhận 'remembered' (đã thuộc) hoặc 'forgot' (cần ôn lại).");
            }

            var entity = await Db.Flashcards.FirstOrDefaultAsync(f => f.Id == flashcardId);
            if (entity == null)
            {
                return ServiceResult<FlashcardItemDto>.NotFound($"Không tìm thấy thẻ Flashcard có ID = {flashcardId}.");
            }

            if (entity.UserId != studentId)
            {
                return ServiceResult<FlashcardItemDto>.Forbidden("Bạn không có quyền cập nhật thẻ Flashcard của học viên khác.");
            }

            var now = DateTime.UtcNow;
            if (status == ReviewStatusRemembered)
            {
                var currentInterval = entity.IntervalDays < 1 ? 1 : entity.IntervalDays;
                entity.IntervalDays = Math.Min(365, currentInterval * 2);
                entity.NextReviewAt = now.AddDays(entity.IntervalDays);
            }
            else
            {
                entity.IntervalDays = 1;
                entity.NextReviewAt = now.AddDays(1);
            }

            await Db.SaveChangesAsync();

            return ServiceResult<FlashcardItemDto>.Ok(
                MapToDto(entity, now),
                $"Đã cập nhật lịch ôn tập thẻ '{entity.Word}' (lần ôn tiếp theo sau {entity.IntervalDays} ngày).");
        }

        /// <summary>
        /// 5.4a. Chỉnh sửa thông tin thẻ Flashcard.
        /// </summary>
        public async Task<ServiceResult<FlashcardItemDto>> UpdateFlashcardAsync(
            int studentId,
            int flashcardId,
            UpsertFlashcardRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Word) || string.IsNullOrWhiteSpace(request.Meaning))
            {
                return ServiceResult<FlashcardItemDto>.BadRequest("Từ vựng (word) và nghĩa (meaning) không được để trống.");
            }

            var entity = await Db.Flashcards.FirstOrDefaultAsync(f => f.Id == flashcardId);
            if (entity == null)
            {
                return ServiceResult<FlashcardItemDto>.NotFound($"Không tìm thấy thẻ Flashcard có ID = {flashcardId}.");
            }

            if (entity.UserId != studentId)
            {
                return ServiceResult<FlashcardItemDto>.Forbidden("Bạn không có quyền chỉnh sửa thẻ Flashcard của học viên khác.");
            }

            entity.Word = request.Word.Trim();
            entity.Phonetic = request.Phonetic?.Trim();
            entity.Meaning = request.Meaning.Trim();
            entity.ExampleSentence = request.ExampleSentence?.Trim();
            entity.AudioUrl = request.AudioUrl?.Trim();

            await Db.SaveChangesAsync();

            return ServiceResult<FlashcardItemDto>.Ok(MapToDto(entity, DateTime.UtcNow), "Cập nhật thẻ Flashcard thành công.");
        }

        /// <summary>
        /// 5.4b. Xóa thẻ Flashcard.
        /// </summary>
        public async Task<ServiceResult<object>> DeleteFlashcardAsync(int studentId, int flashcardId)
        {
            var entity = await Db.Flashcards.FirstOrDefaultAsync(f => f.Id == flashcardId);
            if (entity == null)
            {
                return ServiceResult<object>.NotFound($"Không tìm thấy thẻ Flashcard có ID = {flashcardId}.");
            }

            if (entity.UserId != studentId)
            {
                return ServiceResult<object>.Forbidden("Bạn không có quyền xóa thẻ Flashcard của học viên khác.");
            }

            Db.Flashcards.Remove(entity);
            await Db.SaveChangesAsync();

            return ServiceResult<object>.Ok(new { deleted_id = flashcardId }, "Xóa thẻ Flashcard thành công.");
        }

        private static FlashcardItemDto MapToDto(Flashcard f, DateTime now)
        {
            return new FlashcardItemDto
            {
                Id = f.Id,
                UserId = f.UserId,
                Word = f.Word,
                Phonetic = f.Phonetic,
                Meaning = f.Meaning,
                ExampleSentence = f.ExampleSentence,
                AudioUrl = f.AudioUrl,
                IntervalDays = f.IntervalDays,
                NextReviewAt = f.NextReviewAt,
                IsDue = f.NextReviewAt == null || f.NextReviewAt <= now,
                CreatedAt = f.CreatedAt
            };
        }
    }
}
