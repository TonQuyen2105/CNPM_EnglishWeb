using CNPM_EnglishLearning.Areas.Student.DTOs;
using CNPM_EnglishLearning.Areas.Student.DTOs.Common;
using CNPM_EnglishLearning.Areas.Student.Models;

namespace CNPM_EnglishLearning.Areas.Student.Services.Interfaces
{
    /// <summary>
    /// Nhóm 1: Giao diện nghiệp vụ Dashboard & Tổng quan học tập.
    /// </summary>
    public interface IStudentDashboardService
    {
        Task<ServiceResult<DashboardOverviewDto>> GetDashboardOverviewAsync(int studentId);
    }

    /// <summary>
    /// Nhóm 2 & Nhóm 8: Giao diện nghiệp vụ Khám phá Khóa học, Tư vấn, Đăng ký lớp & Đánh giá khóa học.
    /// </summary>
    public interface IStudentCourseService
    {
        Task<ServiceResult<List<CourseListItemDto>>> GetPublishedCoursesAsync(CourseFilterQueryDto query);
        Task<ServiceResult<CourseDetailDto>> GetCourseDetailAsync(int courseId, int? studentId);
        Task<ServiceResult<ConsultationRecord>> CreateConsultationAsync(int? studentId, CreateConsultationRequestDto request);
        Task<ServiceResult<EnrollmentResultDto>> EnrollClassAsync(int studentId, CreateEnrollmentRequestDto request);
        Task<ServiceResult<BatchEnrollmentResultDto>> EnrollCourseAsync(int studentId, int courseId, CreateCourseEnrollmentRequestDto request);
        Task<ServiceResult<BatchEnrollmentResultDto>> EnrollBatchClassesAsync(int studentId, BatchEnrollmentRequestDto request);
        Task<ServiceResult<List<PaymentDto>>> GetStudentPaymentsAsync(int studentId, int page, int limit, string? status);
        Task<ServiceResult<PaymentDto>> GetPaymentDetailAsync(int studentId, int paymentId);
        Task<ServiceResult<List<CourseReviewItemDto>>> GetCourseReviewsAsync(int courseId, int page, int limit);
        Task<ServiceResult<CourseReviewItemDto>> SubmitCourseReviewAsync(int studentId, int courseId, CreateCourseReviewRequestDto request);
    }

    /// <summary>
    /// Nhóm 3: Giao diện nghiệp vụ Lớp học của tôi & Lịch học cá nhân.
    /// </summary>
    public interface IStudentClassScheduleService
    {
        Task<ServiceResult<List<MyClassListItemDto>>> GetMyClassesAsync(int studentId, string? status, string? search = null);
        Task<ServiceResult<MyClassDetailDto>> GetMyClassDetailAsync(int studentId, int classId);
        Task<ServiceResult<ClassLessonSessionDetailDto>> GetClassLessonDetailAsync(int studentId, int classId, int classLessonId);
        Task<ServiceResult<WeeklyScheduleResponseDto>> GetWeeklyScheduleAsync(int studentId, DateTime? startDate, DateTime? endDate);
    }

    /// <summary>
    /// Nhóm 4: Giao diện nghiệp vụ Nội dung Bài học & Tiến độ tự học (Self-study).
    /// </summary>
    public interface IStudentLearningService
    {
        Task<ServiceResult<CourseSyllabusResponseDto>> GetCourseSyllabusAsync(int studentId, int courseId);
        Task<ServiceResult<LessonDetailDto>> GetLessonDetailAsync(int studentId, int lessonId);
        Task<ServiceResult<CompleteLessonResponseDto>> CompleteLessonAsync(int studentId, int lessonId, CompleteLessonRequestDto request);
    }

    /// <summary>
    /// Nhóm 5: Giao diện nghiệp vụ Học từ vựng với Flashcards (Spaced Repetition).
    /// </summary>
    public interface IStudentFlashcardService
    {
        Task<ServiceResult<List<FlashcardItemDto>>> GetFlashcardsAsync(int studentId, string? mode);
        Task<ServiceResult<FlashcardItemDto>> CreateFlashcardAsync(int studentId, UpsertFlashcardRequestDto request);
        Task<ServiceResult<FlashcardItemDto>> ReviewFlashcardAsync(int studentId, int flashcardId, ReviewFlashcardRequestDto request);
        Task<ServiceResult<FlashcardItemDto>> UpdateFlashcardAsync(int studentId, int flashcardId, UpsertFlashcardRequestDto request);
        Task<ServiceResult<object>> DeleteFlashcardAsync(int studentId, int flashcardId);
    }

    /// <summary>
    /// Nhóm 6: Giao diện nghiệp vụ Hệ thống Thi trực tuyến & Xem kết quả.
    /// </summary>
    public interface IStudentExamService
    {
        Task<ServiceResult<List<ExamListItemDto>>> GetAvailableExamsAsync(int studentId, int? courseId);
        Task<ServiceResult<ExamSessionStartDto>> StartExamSessionAsync(int studentId, int examId);
        Task<ServiceResult<SaveExamAnswerResponseDto>> SaveExamAnswerAsync(int studentId, int userExamId, SaveExamAnswerRequestDto request);
        Task<ServiceResult<SubmitExamResultDto>> SubmitExamAsync(int studentId, int userExamId);
        Task<ServiceResult<List<UserExamHistoryItemDto>>> GetExamHistoryAsync(int studentId);
        Task<ServiceResult<UserExamReviewDetailDto>> GetExamReviewAsync(int studentId, int userExamId);
    }

    /// <summary>
    /// Nhóm 7: Giao diện nghiệp vụ Thống kê Năng lực 4 Kỹ năng & Chỉ số Tích lũy.
    /// </summary>
    public interface IStudentAnalyticsService
    {
        Task<ServiceResult<AnalyticsOverviewDto>> GetAnalyticsOverviewAsync(int studentId);
    }

    /// <summary>
    /// Nhóm 9: Giao diện nghiệp vụ Quản lý Hồ sơ Cá nhân Học sinh.
    /// </summary>
    public interface IStudentProfileService
    {
        Task<ServiceResult<StudentProfileDto>> GetProfileAsync(int studentId);
        Task<ServiceResult<StudentProfileDto>> UpdateProfileAsync(int studentId, UpdateStudentProfileRequestDto request);
    }

    /// <summary>
    /// Nhóm Xác thực: Xác thực và Đăng nhập chuyên biệt cho phân hệ Học sinh.
    /// Kiểm soát nghiêm ngặt 3 điều kiện:
    /// 1. Đúng thông tin email và password
    /// 2. Role của tài khoản phải là "student"
    /// 3. Tài khoản phải ở trạng thái hoạt động (is_active = true)
    /// </summary>
    public interface IStudentAuthService
    {
        Task<ServiceResult<StudentLoginResponseDto>> LoginAsync(StudentLoginRequestDto request);
        Task<ServiceResult<StudentRegisterResponseDto>> RegisterAsync(StudentRegisterRequestDto request);
    }
}
