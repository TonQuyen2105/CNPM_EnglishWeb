using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using CNPM_EnglishLearning.Areas.Student.Models;

namespace CNPM_EnglishLearning.Areas.Student.DTOs
{
    // =========================================================================
    // NHÓM 1: DASHBOARD & TỔNG QUAN HỌC TẬP
    // =========================================================================

    public class DashboardOverviewDto
    {
        [JsonPropertyName("student_profile")]
        public DashboardStudentProfileDto? StudentProfile { get; set; }

        [JsonPropertyName("stats")]
        public DashboardStatsDto Stats { get; set; } = new();

        [JsonPropertyName("active_courses")]
        public List<DashboardActiveCourseDto> ActiveCourses { get; set; } = new();

        [JsonPropertyName("quick_review")]
        public DashboardQuickReviewDto QuickReview { get; set; } = new();

        [JsonPropertyName("next_session")]
        public DashboardNextSessionDto? NextSession { get; set; }
    }

    public class DashboardStudentProfileDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("full_name")]
        public string FullName { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("avatar_url")]
        public string? AvatarUrl { get; set; }

        [JsonPropertyName("streak_days")]
        public int StreakDays { get; set; } = 5;
    }

    public class DashboardStatsDto
    {
        [JsonPropertyName("enrolled_classes_count")]
        public int EnrolledClassesCount { get; set; }

        [JsonPropertyName("enrolled_class_names")]
        public string EnrolledClassNames { get; set; } = string.Empty;

        [JsonPropertyName("mastered_vocabularies")]
        public int MasteredVocabularies { get; set; }

        [JsonPropertyName("average_score")]
        public double AverageScore { get; set; } = 8.5;

        [JsonPropertyName("score_rank")]
        public string ScoreRank { get; set; } = "Giỏi";

        [JsonPropertyName("total_lessons_completed")]
        public int TotalLessonsCompleted { get; set; }

        [JsonPropertyName("total_lessons")]
        public int TotalLessons { get; set; }

        [JsonPropertyName("attendance_rate")]
        public int AttendanceRate { get; set; } = 100;
    }

    public class DashboardActiveCourseDto
    {
        [JsonPropertyName("class_id")]
        public int ClassId { get; set; }

        [JsonPropertyName("class_code")]
        public string ClassCode { get; set; } = string.Empty;

        [JsonPropertyName("class_name")]
        public string ClassName { get; set; } = string.Empty;

        [JsonPropertyName("course_id")]
        public int CourseId { get; set; }

        [JsonPropertyName("course_title")]
        public string CourseTitle { get; set; } = string.Empty;

        [JsonPropertyName("teacher_name")]
        public string? TeacherName { get; set; }

        [JsonPropertyName("schedule_summary")]
        public string? ScheduleSummary { get; set; }

        [JsonPropertyName("thumbnail_url")]
        public string? ThumbnailUrl { get; set; }

        [JsonPropertyName("progress_percentage")]
        public int ProgressPercentage { get; set; }

        [JsonPropertyName("completed_lessons")]
        public int CompletedLessons { get; set; }

        [JsonPropertyName("total_lessons")]
        public int TotalLessons { get; set; }

        [JsonPropertyName("next_lesson_id")]
        public int? NextLessonId { get; set; }

        [JsonPropertyName("next_lesson_title")]
        public string? NextLessonTitle { get; set; }
    }

    public class DashboardQuickReviewDto
    {
        [JsonPropertyName("due_flashcards_count")]
        public int DueFlashcardsCount { get; set; }

        [JsonPropertyName("pending_exams_count")]
        public int PendingExamsCount { get; set; }
    }

    public class DashboardNextSessionDto
    {
        [JsonPropertyName("class_lesson_id")]
        public int ClassLessonId { get; set; }

        [JsonPropertyName("class_id")]
        public int ClassId { get; set; }

        [JsonPropertyName("class_name")]
        public string ClassName { get; set; } = string.Empty;

        [JsonPropertyName("lesson_id")]
        public int LessonId { get; set; }

        [JsonPropertyName("lesson_title")]
        public string LessonTitle { get; set; } = string.Empty;

        [JsonPropertyName("schedule_date")]
        public string ScheduleDate { get; set; } = string.Empty;

        [JsonPropertyName("time_slot")]
        public string TimeSlot { get; set; } = string.Empty;

        [JsonPropertyName("classroom")]
        public string Classroom { get; set; } = string.Empty;
    }

    // =========================================================================
    // NHÓM 2: KHÁM PHÁ KHÓA HỌC, TƯ VẤN & ĐĂNG KÝ LỚP
    // =========================================================================

    public class CourseFilterQueryDto
    {
        [FromQuery(Name = "subject_id")]
        public int? SubjectId { get; set; }

        [FromQuery(Name = "level")]
        public string? Level { get; set; }

        [FromQuery(Name = "search")]
        public string? Search { get; set; }

        [FromQuery(Name = "page")]
        public int Page { get; set; } = 1;

        [FromQuery(Name = "limit")]
        public int Limit { get; set; } = 10;
    }

    public class CourseListItemDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("subject_id")]
        public int SubjectId { get; set; }

        [JsonPropertyName("subject_name")]
        public string SubjectName { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("thumbnail_url")]
        public string? ThumbnailUrl { get; set; }

        [JsonPropertyName("level")]
        public string Level { get; set; } = string.Empty;

        [JsonPropertyName("price")]
        public decimal Price { get; set; }

        [JsonPropertyName("average_rating")]
        public double AverageRating { get; set; }

        [JsonPropertyName("reviews_count")]
        public int ReviewsCount { get; set; }

        [JsonPropertyName("total_lessons")]
        public int TotalLessons { get; set; }

        [JsonPropertyName("open_classes_count")]
        public int OpenClassesCount { get; set; }
    }

    public class CourseDetailDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("subject_id")]
        public int SubjectId { get; set; }

        [JsonPropertyName("subject_name")]
        public string SubjectName { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("thumbnail_url")]
        public string? ThumbnailUrl { get; set; }

        [JsonPropertyName("level")]
        public string Level { get; set; } = string.Empty;

        [JsonPropertyName("price")]
        public decimal Price { get; set; }

        [JsonPropertyName("average_rating")]
        public double AverageRating { get; set; }

        [JsonPropertyName("reviews_count")]
        public int ReviewsCount { get; set; }

        [JsonPropertyName("syllabus")]
        public List<CourseSyllabusLessonItemDto> Syllabus { get; set; } = new();

        [JsonPropertyName("available_classes")]
        public List<CourseOpenClassDto> AvailableClasses { get; set; } = new();
    }

    public class CourseSyllabusLessonItemDto
    {
        [JsonPropertyName("lesson_id")]
        public int LessonId { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("order_index")]
        public int OrderIndex { get; set; }

        [JsonPropertyName("content_type")]
        public string ContentType { get; set; } = string.Empty;

        [JsonPropertyName("is_completed")]
        public bool IsCompleted { get; set; }

        [JsonPropertyName("score")]
        public decimal? Score { get; set; }

        [JsonPropertyName("completed_at")]
        public DateTime? CompletedAt { get; set; }
    }

    public class CourseOpenClassDto
    {
        [JsonPropertyName("class_id")]
        public int ClassId { get; set; }

        [JsonPropertyName("class_code")]
        public string ClassCode { get; set; } = string.Empty;

        [JsonPropertyName("class_name")]
        public string ClassName { get; set; } = string.Empty;

        [JsonPropertyName("start_date")]
        public DateTime StartDate { get; set; }

        [JsonPropertyName("end_date")]
        public DateTime EndDate { get; set; }

        [JsonPropertyName("price")]
        public decimal Price { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("max_students")]
        public int MaxStudents { get; set; }

        [JsonPropertyName("enrolled_students")]
        public int EnrolledStudents { get; set; }

        [JsonPropertyName("available_slots")]
        public int AvailableSlots { get; set; }

        [JsonPropertyName("is_enrolled")]
        public bool IsEnrolled { get; set; }

        [JsonPropertyName("schedules")]
        public List<ClassScheduleItemDto> Schedules { get; set; } = new();
    }

    public class ClassScheduleItemDto
    {
        [JsonPropertyName("schedule_id")]
        public int ScheduleId { get; set; }

        [JsonPropertyName("day_of_week")]
        public int DayOfWeek { get; set; }

        [JsonPropertyName("day_name")]
        public string DayName { get; set; } = string.Empty;

        [JsonPropertyName("time_slot_id")]
        public int TimeSlotId { get; set; }

        [JsonPropertyName("time_slot_name")]
        public string TimeSlotName { get; set; } = string.Empty;

        [JsonPropertyName("time_range")]
        public string TimeRange { get; set; } = string.Empty;

        [JsonPropertyName("classroom_id")]
        public int ClassroomId { get; set; }

        [JsonPropertyName("classroom_name")]
        public string ClassroomName { get; set; } = string.Empty;

        [JsonPropertyName("location")]
        public string? Location { get; set; }
    }

    public class CreateConsultationRequestDto
    {
        [JsonPropertyName("course_id")]
        public int CourseId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ và tên.")]
        [JsonPropertyName("full_name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        [JsonPropertyName("phone_number")]
        public string PhoneNumber { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("note")]
        public string? Note { get; set; }
    }

    public class CreateEnrollmentRequestDto
    {
        [Required]
        [JsonPropertyName("class_id")]
        public int ClassId { get; set; }

        [JsonPropertyName("payment_method")]
        public string PaymentMethod { get; set; } = "vnpay";
    }

    public class EnrollmentResultDto
    {
        [JsonPropertyName("enrollment_id")]
        public int EnrollmentId { get; set; }

        [JsonPropertyName("user_id")]
        public int UserId { get; set; }

        [JsonPropertyName("class_id")]
        public int ClassId { get; set; }

        [JsonPropertyName("class_code")]
        public string ClassCode { get; set; } = string.Empty;

        [JsonPropertyName("class_name")]
        public string ClassName { get; set; } = string.Empty;

        [JsonPropertyName("course_title")]
        public string CourseTitle { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("enrolled_at")]
        public DateTime EnrolledAt { get; set; }

        [JsonPropertyName("payment")]
        public EnrollmentPaymentRecord? Payment { get; set; }
    }

    // =========================================================================
    // NHÓM 3: LỚP HỌC CỦA TÔI & LỊCH HỌC CÁ NHÂN
    // =========================================================================

    public class MyClassListItemDto
    {
        [JsonPropertyName("enrollment_id")]
        public int EnrollmentId { get; set; }

        [JsonPropertyName("enrollment_status")]
        public string EnrollmentStatus { get; set; } = string.Empty;

        [JsonPropertyName("enrolled_at")]
        public DateTime EnrolledAt { get; set; }

        [JsonPropertyName("class_id")]
        public int ClassId { get; set; }

        [JsonPropertyName("class_code")]
        public string ClassCode { get; set; } = string.Empty;

        [JsonPropertyName("class_name")]
        public string ClassName { get; set; } = string.Empty;

        [JsonPropertyName("class_status")]
        public string ClassStatus { get; set; } = string.Empty;

        [JsonPropertyName("start_date")]
        public DateTime StartDate { get; set; }

        [JsonPropertyName("end_date")]
        public DateTime EndDate { get; set; }

        [JsonPropertyName("course_id")]
        public int CourseId { get; set; }

        [JsonPropertyName("course_title")]
        public string CourseTitle { get; set; } = string.Empty;

        [JsonPropertyName("thumbnail_url")]
        public string? ThumbnailUrl { get; set; }

        [JsonPropertyName("teachers")]
        public List<ClassTeacherInfoDto> Teachers { get; set; } = new();

        [JsonPropertyName("schedules")]
        public List<ClassScheduleItemDto> Schedules { get; set; } = new();

        [JsonPropertyName("schedule_summary")]
        public string? ScheduleSummary { get; set; }

        [JsonPropertyName("progress_percentage")]
        public int ProgressPercentage { get; set; }

        [JsonPropertyName("completed_lessons")]
        public int CompletedLessons { get; set; }

        [JsonPropertyName("total_lessons")]
        public int TotalLessons { get; set; }
    }

    public class ClassTeacherInfoDto
    {
        [JsonPropertyName("teacher_id")]
        public int TeacherId { get; set; }

        [JsonPropertyName("full_name")]
        public string FullName { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("avatar_url")]
        public string? AvatarUrl { get; set; }

        [JsonPropertyName("qualification")]
        public string? Qualification { get; set; }

        [JsonPropertyName("role")]
        public string Role { get; set; } = "main";
    }

    public class MyClassDetailDto
    {
        [JsonPropertyName("class_id")]
        public int ClassId { get; set; }

        [JsonPropertyName("class_code")]
        public string ClassCode { get; set; } = string.Empty;

        [JsonPropertyName("class_name")]
        public string ClassName { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("start_date")]
        public DateTime StartDate { get; set; }

        [JsonPropertyName("end_date")]
        public DateTime EndDate { get; set; }

        [JsonPropertyName("course_id")]
        public int CourseId { get; set; }

        [JsonPropertyName("course_title")]
        public string CourseTitle { get; set; } = string.Empty;

        [JsonPropertyName("teachers")]
        public List<ClassTeacherInfoDto> Teachers { get; set; } = new();

        [JsonPropertyName("weekly_schedules")]
        public List<ClassScheduleItemDto> WeeklySchedules { get; set; } = new();

        [JsonPropertyName("class_lessons")]
        public List<ClassLessonTimelineItemDto> ClassLessons { get; set; } = new();
    }

    public class ClassLessonTimelineItemDto
    {
        [JsonPropertyName("class_lesson_id")]
        public int ClassLessonId { get; set; }

        [JsonPropertyName("lesson_id")]
        public int LessonId { get; set; }

        [JsonPropertyName("lesson_title")]
        public string LessonTitle { get; set; } = string.Empty;

        [JsonPropertyName("content_type")]
        public string ContentType { get; set; } = string.Empty;

        [JsonPropertyName("order_index")]
        public int OrderIndex { get; set; }

        [JsonPropertyName("schedule_date")]
        public DateTime? ScheduleDate { get; set; }

        [JsonPropertyName("session_status")]
        public string SessionStatus { get; set; } = string.Empty;

        [JsonPropertyName("time_slot")]
        public string? TimeSlot { get; set; }

        [JsonPropertyName("classroom")]
        public string? Classroom { get; set; }

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }

        [JsonPropertyName("is_completed")]
        public bool IsCompleted { get; set; }

        [JsonPropertyName("student_score")]
        public decimal? StudentScore { get; set; }
    }

    public class ClassLessonSessionDetailDto
    {
        [JsonPropertyName("class_lesson_id")]
        public int ClassLessonId { get; set; }

        [JsonPropertyName("class_id")]
        public int ClassId { get; set; }

        [JsonPropertyName("class_name")]
        public string ClassName { get; set; } = string.Empty;

        [JsonPropertyName("lesson_id")]
        public int LessonId { get; set; }

        [JsonPropertyName("lesson_title")]
        public string LessonTitle { get; set; } = string.Empty;

        [JsonPropertyName("content_type")]
        public string ContentType { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string? Content { get; set; }

        [JsonPropertyName("schedule_date")]
        public DateTime? ScheduleDate { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("time_slot")]
        public string TimeSlot { get; set; } = string.Empty;

        [JsonPropertyName("classroom")]
        public string Classroom { get; set; } = string.Empty;

        [JsonPropertyName("location")]
        public string? Location { get; set; }

        [JsonPropertyName("teacher_name")]
        public string? TeacherName { get; set; }

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }

        [JsonPropertyName("is_completed")]
        public bool IsCompleted { get; set; }

        [JsonPropertyName("attachments")]
        public List<LessonAttachmentItem> Attachments { get; set; } = new();
    }

    public class WeeklyScheduleResponseDto
    {
        [JsonPropertyName("start_date")]
        public string StartDate { get; set; } = string.Empty;

        [JsonPropertyName("end_date")]
        public string EndDate { get; set; } = string.Empty;

        [JsonPropertyName("sessions")]
        public List<StudentScheduleSessionItemDto> Sessions { get; set; } = new();
    }

    public class StudentScheduleSessionItemDto
    {
        [JsonPropertyName("class_lesson_id")]
        public int? ClassLessonId { get; set; }

        [JsonPropertyName("schedule_id")]
        public int? ScheduleId { get; set; }

        [JsonPropertyName("class_id")]
        public int ClassId { get; set; }

        [JsonPropertyName("class_code")]
        public string ClassCode { get; set; } = string.Empty;

        [JsonPropertyName("class_name")]
        public string ClassName { get; set; } = string.Empty;

        [JsonPropertyName("course_title")]
        public string CourseTitle { get; set; } = string.Empty;

        [JsonPropertyName("lesson_id")]
        public int? LessonId { get; set; }

        [JsonPropertyName("lesson_title")]
        public string LessonTitle { get; set; } = string.Empty;

        [JsonPropertyName("schedule_date")]
        public string ScheduleDate { get; set; } = string.Empty;

        [JsonPropertyName("day_of_week")]
        public int DayOfWeek { get; set; }

        [JsonPropertyName("time_slot")]
        public string TimeSlot { get; set; } = string.Empty;

        [JsonPropertyName("start_time")]
        public string StartTime { get; set; } = string.Empty;

        [JsonPropertyName("end_time")]
        public string EndTime { get; set; } = string.Empty;

        [JsonPropertyName("classroom")]
        public string Classroom { get; set; } = string.Empty;

        [JsonPropertyName("location")]
        public string? Location { get; set; }

        [JsonPropertyName("teacher_name")]
        public string? TeacherName { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = "scheduled";

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }
    }

    // =========================================================================
    // NHÓM 4: NỘI DUNG BÀI HỌC & TIẾN ĐỘ (SELF-STUDY)
    // =========================================================================

    public class CourseSyllabusResponseDto
    {
        [JsonPropertyName("course_id")]
        public int CourseId { get; set; }

        [JsonPropertyName("course_title")]
        public string CourseTitle { get; set; } = string.Empty;

        [JsonPropertyName("total_lessons")]
        public int TotalLessons { get; set; }

        [JsonPropertyName("completed_lessons")]
        public int CompletedLessons { get; set; }

        [JsonPropertyName("progress_percentage")]
        public int ProgressPercentage { get; set; }

        [JsonPropertyName("lessons")]
        public List<CourseSyllabusLessonItemDto> Lessons { get; set; } = new();
    }

    public class LessonDetailDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("course_id")]
        public int CourseId { get; set; }

        [JsonPropertyName("course_title")]
        public string CourseTitle { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("order_index")]
        public int OrderIndex { get; set; }

        [JsonPropertyName("content_type")]
        public string ContentType { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string? Content { get; set; }

        [JsonPropertyName("is_completed")]
        public bool IsCompleted { get; set; }

        [JsonPropertyName("score")]
        public decimal? Score { get; set; }

        [JsonPropertyName("completed_at")]
        public DateTime? CompletedAt { get; set; }

        [JsonPropertyName("attachments")]
        public List<LessonAttachmentItem> Attachments { get; set; } = new();

        [JsonPropertyName("practice_questions")]
        public List<LessonPracticeQuestionDto> PracticeQuestions { get; set; } = new();
    }

    public class LessonPracticeQuestionDto
    {
        [JsonPropertyName("question_id")]
        public int QuestionId { get; set; }

        [JsonPropertyName("question_text")]
        public string QuestionText { get; set; } = string.Empty;

        [JsonPropertyName("audio_url")]
        public string? AudioUrl { get; set; }

        [JsonPropertyName("question_type")]
        public string QuestionType { get; set; } = string.Empty;

        [JsonPropertyName("default_points")]
        public decimal DefaultPoints { get; set; }

        [JsonPropertyName("options")]
        public List<QuestionOptionSafeDto> Options { get; set; } = new();
    }

    public class CompleteLessonRequestDto
    {
        [JsonPropertyName("score")]
        public decimal? Score { get; set; } = 100;

        [JsonPropertyName("is_completed")]
        public bool? IsCompleted { get; set; } = true;
    }

    public class CompleteLessonResponseDto
    {
        [JsonPropertyName("user_progress_id")]
        public int UserProgressId { get; set; }

        [JsonPropertyName("user_id")]
        public int UserId { get; set; }

        [JsonPropertyName("lesson_id")]
        public int LessonId { get; set; }

        [JsonPropertyName("is_completed")]
        public bool IsCompleted { get; set; }

        [JsonPropertyName("score")]
        public decimal? Score { get; set; }

        [JsonPropertyName("completed_at")]
        public DateTime? CompletedAt { get; set; }
    }

    // =========================================================================
    // NHÓM 5: HỌC TỪ VỰNG VỚI FLASHCARDS (SPACED REPETITION)
    // =========================================================================

    public class FlashcardItemDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("user_id")]
        public int UserId { get; set; }

        [JsonPropertyName("word")]
        public string Word { get; set; } = string.Empty;

        [JsonPropertyName("phonetic")]
        public string? Phonetic { get; set; }

        [JsonPropertyName("meaning")]
        public string Meaning { get; set; } = string.Empty;

        [JsonPropertyName("example_sentence")]
        public string? ExampleSentence { get; set; }

        [JsonPropertyName("audio_url")]
        public string? AudioUrl { get; set; }

        [JsonPropertyName("interval_days")]
        public int IntervalDays { get; set; }

        [JsonPropertyName("next_review_at")]
        public DateTime? NextReviewAt { get; set; }

        [JsonPropertyName("is_due")]
        public bool IsDue { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }
    }

    public class UpsertFlashcardRequestDto
    {
        [Required(ErrorMessage = "Từ vựng không được để trống.")]
        [JsonPropertyName("word")]
        public string Word { get; set; } = string.Empty;

        [JsonPropertyName("phonetic")]
        public string? Phonetic { get; set; }

        [Required(ErrorMessage = "Nghĩa của từ không được để trống.")]
        [JsonPropertyName("meaning")]
        public string Meaning { get; set; } = string.Empty;

        [JsonPropertyName("example_sentence")]
        public string? ExampleSentence { get; set; }

        [JsonPropertyName("audio_url")]
        public string? AudioUrl { get; set; }
    }

    public class ReviewFlashcardRequestDto
    {
        /// <summary>
        /// Giá trị hợp lệ: "remembered" | "forgot"
        /// </summary>
        [Required]
        [JsonPropertyName("status")]
        public string Status { get; set; } = "remembered";
    }

    // =========================================================================
    // NHÓM 6: HỆ THỐNG THI TRỰC TUYẾN & XEM KẾT QUẢ
    // =========================================================================

    public class ExamListItemDto
    {
        [JsonPropertyName("exam_id")]
        public int ExamId { get; set; }

        [JsonPropertyName("course_id")]
        public int? CourseId { get; set; }

        [JsonPropertyName("course_title")]
        public string? CourseTitle { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("duration_minutes")]
        public int DurationMinutes { get; set; }

        [JsonPropertyName("pass_score")]
        public decimal PassScore { get; set; }

        [JsonPropertyName("max_attempts")]
        public int? MaxAttempts { get; set; }

        [JsonPropertyName("attempt_count")]
        public int AttemptCount { get; set; }

        [JsonPropertyName("remaining_attempts")]
        public int? RemainingAttempts { get; set; }

        [JsonPropertyName("best_score")]
        public decimal? BestScore { get; set; }

        [JsonPropertyName("in_progress_user_exam_id")]
        public int? InProgressUserExamId { get; set; }

        [JsonPropertyName("can_start")]
        public bool CanStart { get; set; }
    }

    public class ExamSessionStartDto
    {
        [JsonPropertyName("user_exam_id")]
        public int UserExamId { get; set; }

        [JsonPropertyName("exam_id")]
        public int ExamId { get; set; }

        [JsonPropertyName("exam_title")]
        public string ExamTitle { get; set; } = string.Empty;

        [JsonPropertyName("attempt_number")]
        public int AttemptNumber { get; set; }

        [JsonPropertyName("duration_minutes")]
        public int DurationMinutes { get; set; }

        [JsonPropertyName("started_at")]
        public DateTime StartedAt { get; set; }

        [JsonPropertyName("expires_at")]
        public DateTime ExpiresAt { get; set; }

        [JsonPropertyName("remaining_seconds")]
        public int RemainingSeconds { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = "in_progress";

        [JsonPropertyName("questions")]
        public List<ExamQuestionSessionDto> Questions { get; set; } = new();
    }

    public class ExamQuestionSessionDto
    {
        [JsonPropertyName("question_id")]
        public int QuestionId { get; set; }

        [JsonPropertyName("order_index")]
        public int OrderIndex { get; set; }

        [JsonPropertyName("points")]
        public decimal Points { get; set; }

        [JsonPropertyName("question_type")]
        public string QuestionType { get; set; } = string.Empty;

        [JsonPropertyName("question_text")]
        public string QuestionText { get; set; } = string.Empty;

        [JsonPropertyName("audio_url")]
        public string? AudioUrl { get; set; }

        [JsonPropertyName("options")]
        public List<QuestionOptionSafeDto> Options { get; set; } = new();

        [JsonPropertyName("saved_selected_option_id")]
        public int? SavedSelectedOptionId { get; set; }

        [JsonPropertyName("saved_text_answer")]
        public string? SavedTextAnswer { get; set; }

        [JsonPropertyName("saved_audio_url")]
        public string? SavedAudioUrl { get; set; }
    }

    public class QuestionOptionSafeDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("question_id")]
        public int QuestionId { get; set; }

        [JsonPropertyName("option_text")]
        public string OptionText { get; set; } = string.Empty;
    }

    public class SaveExamAnswerRequestDto
    {
        [Required]
        [JsonPropertyName("question_id")]
        public int QuestionId { get; set; }

        [JsonPropertyName("selected_option_id")]
        public int? SelectedOptionId { get; set; }

        [JsonPropertyName("text_answer")]
        public string? TextAnswer { get; set; }

        [JsonPropertyName("audio_url")]
        public string? AudioUrl { get; set; }
    }

    public class SaveExamAnswerResponseDto
    {
        [JsonPropertyName("user_exam_id")]
        public int UserExamId { get; set; }

        [JsonPropertyName("question_id")]
        public int QuestionId { get; set; }

        [JsonPropertyName("selected_option_id")]
        public int? SelectedOptionId { get; set; }

        [JsonPropertyName("text_answer")]
        public string? TextAnswer { get; set; }

        [JsonPropertyName("audio_url")]
        public string? AudioUrl { get; set; }

        [JsonPropertyName("saved_at")]
        public DateTime SavedAt { get; set; } = DateTime.UtcNow;
    }

    public class SubmitExamResultDto
    {
        [JsonPropertyName("user_exam_id")]
        public int UserExamId { get; set; }

        [JsonPropertyName("exam_id")]
        public int ExamId { get; set; }

        [JsonPropertyName("exam_title")]
        public string ExamTitle { get; set; } = string.Empty;

        [JsonPropertyName("started_at")]
        public DateTime StartedAt { get; set; }

        [JsonPropertyName("submitted_at")]
        public DateTime SubmittedAt { get; set; }

        [JsonPropertyName("duration_seconds")]
        public int DurationSeconds { get; set; }

        [JsonPropertyName("total_questions")]
        public int TotalQuestions { get; set; }

        [JsonPropertyName("auto_graded_questions")]
        public int AutoGradedQuestions { get; set; }

        [JsonPropertyName("pending_review_questions")]
        public int PendingReviewQuestions { get; set; }

        [JsonPropertyName("correct_answers_count")]
        public int CorrectAnswersCount { get; set; }

        [JsonPropertyName("total_score")]
        public decimal? TotalScore { get; set; }

        [JsonPropertyName("pass_score")]
        public decimal PassScore { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("grading_status")]
        public string GradingStatus { get; set; } = string.Empty;
    }

    public class UserExamHistoryItemDto
    {
        [JsonPropertyName("user_exam_id")]
        public int UserExamId { get; set; }

        [JsonPropertyName("exam_id")]
        public int ExamId { get; set; }

        [JsonPropertyName("exam_title")]
        public string ExamTitle { get; set; } = string.Empty;

        [JsonPropertyName("attempt_number")]
        public int AttemptNumber { get; set; }

        [JsonPropertyName("started_at")]
        public DateTime StartedAt { get; set; }

        [JsonPropertyName("submitted_at")]
        public DateTime? SubmittedAt { get; set; }

        [JsonPropertyName("duration_seconds")]
        public int DurationSeconds { get; set; }

        [JsonPropertyName("correct_answers_count")]
        public int CorrectAnswersCount { get; set; }

        [JsonPropertyName("total_questions")]
        public int TotalQuestions { get; set; }

        [JsonPropertyName("total_score")]
        public decimal? TotalScore { get; set; }

        [JsonPropertyName("pass_score")]
        public decimal PassScore { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;
    }

    public class UserExamReviewDetailDto
    {
        [JsonPropertyName("user_exam_id")]
        public int UserExamId { get; set; }

        [JsonPropertyName("exam_id")]
        public int ExamId { get; set; }

        [JsonPropertyName("exam_title")]
        public string ExamTitle { get; set; } = string.Empty;

        [JsonPropertyName("attempt_number")]
        public int AttemptNumber { get; set; }

        [JsonPropertyName("started_at")]
        public DateTime StartedAt { get; set; }

        [JsonPropertyName("submitted_at")]
        public DateTime? SubmittedAt { get; set; }

        [JsonPropertyName("duration_seconds")]
        public int DurationSeconds { get; set; }

        [JsonPropertyName("total_score")]
        public decimal? TotalScore { get; set; }

        [JsonPropertyName("pass_score")]
        public decimal PassScore { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("questions")]
        public List<UserExamQuestionReviewItemDto> Questions { get; set; } = new();
    }

    public class UserExamQuestionReviewItemDto
    {
        [JsonPropertyName("question_id")]
        public int QuestionId { get; set; }

        [JsonPropertyName("order_index")]
        public int OrderIndex { get; set; }

        [JsonPropertyName("question_type")]
        public string QuestionType { get; set; } = string.Empty;

        [JsonPropertyName("question_text")]
        public string QuestionText { get; set; } = string.Empty;

        [JsonPropertyName("audio_url")]
        public string? AudioUrl { get; set; }

        [JsonPropertyName("max_points")]
        public decimal MaxPoints { get; set; }

        [JsonPropertyName("score_earned")]
        public decimal? ScoreEarned { get; set; }

        [JsonPropertyName("is_correct")]
        public bool? IsCorrect { get; set; }

        [JsonPropertyName("grading_status")]
        public string GradingStatus { get; set; } = string.Empty;

        [JsonPropertyName("feedback")]
        public string? Feedback { get; set; }

        [JsonPropertyName("selected_option_id")]
        public int? SelectedOptionId { get; set; }

        [JsonPropertyName("text_answer")]
        public string? TextAnswer { get; set; }

        [JsonPropertyName("student_audio_url")]
        public string? StudentAudioUrl { get; set; }

        [JsonPropertyName("correct_option_id")]
        public int? CorrectOptionId { get; set; }

        [JsonPropertyName("correct_answer_text")]
        public string? CorrectAnswerText { get; set; }

        [JsonPropertyName("reference_audio_url")]
        public string? ReferenceAudioUrl { get; set; }

        [JsonPropertyName("options")]
        public List<QuestionOptionReviewDto> Options { get; set; } = new();
    }

    public class QuestionOptionReviewDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("option_text")]
        public string OptionText { get; set; } = string.Empty;

        [JsonPropertyName("is_correct")]
        public bool IsCorrect { get; set; }
    }

    // =========================================================================
    // NHÓM 7: THỐNG KÊ NĂNG LỰC & CHỈ SỐ HỌC TẬP
    // =========================================================================

    public class AnalyticsOverviewDto
    {
        [JsonPropertyName("skills_proficiency")]
        public SkillsProficiencyDto SkillsProficiency { get; set; } = new();

        [JsonPropertyName("accumulated_stats")]
        public AccumulatedStatsDto AccumulatedStats { get; set; } = new();
    }

    public class SkillsProficiencyDto
    {
        [JsonPropertyName("listening")]
        public SkillMetricDto Listening { get; set; } = new();

        [JsonPropertyName("reading")]
        public SkillMetricDto Reading { get; set; } = new();

        [JsonPropertyName("writing")]
        public SkillMetricDto Writing { get; set; } = new();

        [JsonPropertyName("speaking")]
        public SkillMetricDto Speaking { get; set; } = new();
    }

    public class SkillMetricDto
    {
        [JsonPropertyName("completed_lessons")]
        public int CompletedLessons { get; set; }

        [JsonPropertyName("average_score")]
        public double AverageScore { get; set; }
    }

    public class AccumulatedStatsDto
    {
        [JsonPropertyName("total_courses_enrolled")]
        public int TotalCoursesEnrolled { get; set; }

        [JsonPropertyName("total_lessons_completed")]
        public int TotalLessonsCompleted { get; set; }

        [JsonPropertyName("total_exams_passed")]
        public int TotalExamsPassed { get; set; }

        [JsonPropertyName("total_flashcards_mastered")]
        public int TotalFlashcardsMastered { get; set; }
    }

    // =========================================================================
    // NHÓM 8: ĐÁNH GIÁ & PHẢN HỒI KHÓA HỌC
    // =========================================================================

    public class CourseReviewItemDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("course_id")]
        public int CourseId { get; set; }

        [JsonPropertyName("user_id")]
        public int UserId { get; set; }

        [JsonPropertyName("user_full_name")]
        public string UserFullName { get; set; } = string.Empty;

        [JsonPropertyName("user_avatar_url")]
        public string? UserAvatarUrl { get; set; }

        [JsonPropertyName("rating")]
        public int Rating { get; set; }

        [JsonPropertyName("comment")]
        public string? Comment { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }
    }

    public class CreateCourseReviewRequestDto
    {
        [Range(1, 5, ErrorMessage = "Điểm đánh giá (rating) phải từ 1 đến 5.")]
        [JsonPropertyName("rating")]
        public int Rating { get; set; } = 5;

        [JsonPropertyName("comment")]
        public string? Comment { get; set; }
    }

    // =========================================================================
    // NHÓM 9: QUẢN LÝ HỒ SƠ CÁ NHÂN (PROFILE)
    // =========================================================================

    public class StudentProfileDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("full_name")]
        public string FullName { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("avatar_url")]
        public string? AvatarUrl { get; set; }

        [JsonPropertyName("phone_number")]
        public string PhoneNumber { get; set; } = string.Empty;

        [JsonPropertyName("learning_goal")]
        public string LearningGoal { get; set; } = string.Empty;
    }

    public class UpdateStudentProfileRequestDto
    {
        [Required(ErrorMessage = "Họ và tên không được để trống.")]
        [JsonPropertyName("full_name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email không được để trống.")]
        [EmailAddress(ErrorMessage = "Định dạng email không hợp lệ.")]
        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("phone_number")]
        public string? PhoneNumber { get; set; }

        [JsonPropertyName("learning_goal")]
        public string? LearningGoal { get; set; }

        [JsonPropertyName("avatar_url")]
        public string? AvatarUrl { get; set; }
    }

    // ==========================================
    // NHÓM XÁC THỰC: HỌC SINH ĐĂNG NHẬP (AUTH)
    // ==========================================

    public class StudentLoginRequestDto
    {
        [Required(ErrorMessage = "Email không được để trống.")]
        [EmailAddress(ErrorMessage = "Định dạng email không hợp lệ.")]
        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu không được để trống.")]
        [JsonPropertyName("password")]
        public string Password { get; set; } = string.Empty;
    }

    public class StudentLoginUserDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("full_name")]
        public string FullName { get; set; } = string.Empty;

        [JsonPropertyName("role")]
        public string Role { get; set; } = "student";

        [JsonPropertyName("phone_number")]
        public string? PhoneNumber { get; set; }

        [JsonPropertyName("avatar_url")]
        public string? AvatarUrl { get; set; }

        [JsonPropertyName("is_active")]
        public bool IsActive { get; set; }
    }

    public class StudentLoginResponseDto
    {
        [JsonPropertyName("user")]
        public StudentLoginUserDto User { get; set; } = new();

        [JsonPropertyName("token")]
        public string Token { get; set; } = string.Empty;

        [JsonPropertyName("redirect_url")]
        public string RedirectUrl { get; set; } = "/Areas/Student/Views/dashboard.html";
    }
}
