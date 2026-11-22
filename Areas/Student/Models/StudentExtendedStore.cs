using System.Collections.Concurrent;
using System.Text.Json.Serialization;

namespace CNPM_EnglishLearning.Areas.Student.Models
{
    /// <summary>
    /// Mô hình lưu trữ mở rộng cho Hồ sơ Học sinh (bổ sung phone_number & learning_goal theo Gap Analysis #1).
    /// </summary>
    public class UserProfileExtension
    {
        public int UserId { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public string LearningGoal { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Mô hình Yêu cầu Đăng ký Tư vấn Khóa học (bổ sung bảng consultations theo Gap Analysis #2).
    /// </summary>
    public class ConsultationRecord
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("user_id")]
        public int? UserId { get; set; }

        [JsonPropertyName("course_id")]
        public int CourseId { get; set; }

        [JsonPropertyName("course_title")]
        public string? CourseTitle { get; set; }

        [JsonPropertyName("full_name")]
        public string FullName { get; set; } = string.Empty;

        [JsonPropertyName("phone_number")]
        public string PhoneNumber { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("note")]
        public string? Note { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = "pending"; // pending, contacted, resolved

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Mô hình Giao dịch Thanh toán khi Đăng ký Lớp học (bổ sung bảng payments/orders theo Gap Analysis #2).
    /// </summary>
    public class EnrollmentPaymentRecord
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("enrollment_id")]
        public int EnrollmentId { get; set; }

        [JsonPropertyName("user_id")]
        public int UserId { get; set; }

        [JsonPropertyName("class_id")]
        public int ClassId { get; set; }

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("payment_method")]
        public string PaymentMethod { get; set; } = "vnpay"; // vnpay, momo, bank_transfer, cash

        [JsonPropertyName("payment_status")]
        public string PaymentStatus { get; set; } = "completed";

        [JsonPropertyName("transaction_code")]
        public string TransactionCode { get; set; } = string.Empty;

        [JsonPropertyName("paid_at")]
        public DateTime PaidAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Mô hình Tài liệu đính kèm buổi học / bài học (bổ sung attachments/lesson_materials theo Gap Analysis #4).
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
    /// Kho lưu trữ mở rộng đồng bộ (Thread-safe Singleton Store) giải quyết 4 điểm thiếu hụt Schema
    /// mà không làm xung đột cấu trúc 22 bảng hiện tại trên Supabase PostgreSQL.
    /// </summary>
    public static class StudentExtendedStore
    {
        private static int _consultationSeq = 1;
        private static int _paymentSeq = 1;

        // 1. Mở rộng bảng users (phone_number, learning_goal)
        private static readonly ConcurrentDictionary<int, UserProfileExtension> _userProfiles = new();

        // 2. Bảng consultations & payments
        private static readonly ConcurrentDictionary<int, ConsultationRecord> _consultations = new();
        private static readonly ConcurrentDictionary<int, EnrollmentPaymentRecord> _paymentsByEnrollmentId = new();

        // 3. Mở rộng bảng user_exam_answers (audio_url cho câu hỏi pronunciation)
        // Key: $"{userExamId}:{questionId}"
        private static readonly ConcurrentDictionary<string, string> _examAnswerAudioUrls = new();

        // 4. Mở rộng bảng lessons (attachments / tài liệu buổi học)
        private static readonly ConcurrentDictionary<int, List<LessonAttachmentItem>> _lessonAttachments = new();

        static StudentExtendedStore()
        {
            // Seed dữ liệu mặc định cho học sinh mẫu (UserId = 5, 6, 7)
            _userProfiles[5] = new UserProfileExtension
            {
                UserId = 5,
                PhoneNumber = "0901234567",
                LearningGoal = "Đạt IELTS 6.5 và giao tiếp tiếng Anh phản xạ tự nhiên trong 6 tháng"
            };
            _userProfiles[6] = new UserProfileExtension
            {
                UserId = 6,
                PhoneNumber = "0987654321",
                LearningGoal = "Đạt chứng chỉ IELTS 7.0 phục vụ du học"
            };
            _userProfiles[7] = new UserProfileExtension
            {
                UserId = 7,
                PhoneNumber = "0912345678",
                LearningGoal = "Đạt TOEIC 750+ để tốt nghiệp và làm việc tại doanh nghiệp nước ngoài"
            };
        }

        public static UserProfileExtension GetOrCreateUserProfileExtension(int userId)
        {
            return _userProfiles.GetOrAdd(userId, id => new UserProfileExtension
            {
                UserId = id,
                PhoneNumber = "0901234567",
                LearningGoal = "Đạt IELTS 6.5 trong 6 tháng tới"
            });
        }

        public static UserProfileExtension UpdateUserProfileExtension(int userId, string? phoneNumber, string? learningGoal)
        {
            var current = GetOrCreateUserProfileExtension(userId);
            if (phoneNumber != null)
            {
                current.PhoneNumber = phoneNumber.Trim();
            }
            if (learningGoal != null)
            {
                current.LearningGoal = learningGoal.Trim();
            }
            current.UpdatedAt = DateTime.UtcNow;
            _userProfiles[userId] = current;
            return current;
        }

        public static ConsultationRecord AddConsultation(ConsultationRecord record)
        {
            var id = Interlocked.Increment(ref _consultationSeq);
            record.Id = id;
            record.CreatedAt = DateTime.UtcNow;
            _consultations[id] = record;
            return record;
        }

        public static IReadOnlyCollection<ConsultationRecord> GetAllConsultations() => _consultations.Values.ToList();

        public static EnrollmentPaymentRecord RecordPayment(int enrollmentId, int userId, int classId, decimal amount, string paymentMethod)
        {
            var id = Interlocked.Increment(ref _paymentSeq);
            var normalizedMethod = string.IsNullOrWhiteSpace(paymentMethod) ? "vnpay" : paymentMethod.Trim().ToLowerInvariant();
            var payment = new EnrollmentPaymentRecord
            {
                Id = id,
                EnrollmentId = enrollmentId,
                UserId = userId,
                ClassId = classId,
                Amount = amount,
                PaymentMethod = normalizedMethod,
                PaymentStatus = "completed",
                TransactionCode = $"PAY-{DateTime.UtcNow:yyyyMMdd}-{enrollmentId:D4}-{id:D4}",
                PaidAt = DateTime.UtcNow
            };
            _paymentsByEnrollmentId[enrollmentId] = payment;
            return payment;
        }

        public static EnrollmentPaymentRecord? GetPaymentByEnrollment(int enrollmentId)
        {
            _paymentsByEnrollmentId.TryGetValue(enrollmentId, out var payment);
            return payment;
        }

        public static void SetExamAnswerAudioUrl(int userExamId, int questionId, string? audioUrl)
        {
            var key = $"{userExamId}:{questionId}";
            if (string.IsNullOrWhiteSpace(audioUrl))
            {
                _examAnswerAudioUrls.TryRemove(key, out _);
                return;
            }
            _examAnswerAudioUrls[key] = audioUrl.Trim();
        }

        public static string? GetExamAnswerAudioUrl(int userExamId, int questionId)
        {
            var key = $"{userExamId}:{questionId}";
            return _examAnswerAudioUrls.TryGetValue(key, out var url) ? url : null;
        }

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
