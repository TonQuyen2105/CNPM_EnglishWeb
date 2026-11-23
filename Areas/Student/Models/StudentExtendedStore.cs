using System.Collections.Concurrent;
using System.Text.Json.Serialization;

namespace CNPM_EnglishLearning.Areas.Student.Models
{
    /// <summary>
    /// Mô hình Tài liệu đính kèm buổi học / bài học (tài liệu PDF, slide bài giảng, file âm thanh bài học).
    /// </summary>
    public class LessonAttachmentItem
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("file_type")]
        public string FileType { get; set; } = "pdf"; // pdf, slide, audio, video, doc

        [JsonPropertyName("file_url")]
        public string FileUrl { get; set; } = string.Empty;

        [JsonPropertyName("file_size_kb")]
        public int FileSizeKb { get; set; }
    }

    /// <summary>
    /// Kho hỗ trợ tài liệu học tập (Lesson Materials Store) cung cấp danh mục tài liệu đính kèm mẫu cho các buổi học.
    /// Ghi chú: Các phần mở rộng in-memory trước đây (UserProfileExtension, ConsultationRecord, EnrollmentPaymentRecord, _examAnswerAudioUrls)
    /// đã được loại bỏ hoàn toàn do các bảng và trường đã đồng bộ trực tiếp vào cơ sở dữ liệu (users.phone_number, payments),
    /// đồng thời hệ thống đã loại bỏ chức năng tư vấn khóa học, mục tiêu học tập và dạng câu hỏi phát âm (pronunciation).
    /// </summary>
    public static class StudentExtendedStore
    {
        private static readonly ConcurrentDictionary<int, List<LessonAttachmentItem>> _lessonAttachments = new();

        public static List<LessonAttachmentItem> GetLessonAttachments(int lessonId, string lessonTitle, string contentType)
        {
            return _lessonAttachments.GetOrAdd(lessonId, id =>
            {
                var list = new List<LessonAttachmentItem>
                {
                    new LessonAttachmentItem
                    {
                        Id = id * 10 + 1,
                        Title = $"Slide bài giảng - {lessonTitle}",
                        FileType = "slide",
                        FileUrl = $"/wwwroot/materials/lessons/lesson-{id}-slides.pdf",
                        FileSizeKb = 1420
                    },
                    new LessonAttachmentItem
                    {
                        Id = id * 10 + 2,
                        Title = $"Tài liệu tóm tắt & Bài tập về nhà - {lessonTitle}",
                        FileType = "pdf",
                        FileUrl = $"/wwwroot/materials/lessons/lesson-{id}-handout.pdf",
                        FileSizeKb = 680
                    }
                };

                if (string.Equals(contentType, "listening", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(contentType, "speaking", StringComparison.OrdinalIgnoreCase))
                {
                    list.Add(new LessonAttachmentItem
                    {
                        Id = id * 10 + 3,
                        Title = $"Audio luyện nghe / phát âm - {lessonTitle}",
                        FileType = "audio",
                        FileUrl = $"/wwwroot/materials/lessons/lesson-{id}-audio.mp3",
                        FileSizeKb = 3250
                    });
                }

                return list;
            });
        }
    }
}
