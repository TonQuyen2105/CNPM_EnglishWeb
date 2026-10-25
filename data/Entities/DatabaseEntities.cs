using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CNPM_EnglishLearning.Data.Entities
{
    // 1. users
    [Table("users")]
    public class User
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("email")]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Column("password")]
        [MaxLength(255)]
        public string Password { get; set; } = "123456";

        [Required]
        [Column("full_name")]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Column("avatar_url")]
        public string? AvatarUrl { get; set; }

        [Required]
        [Column("role")]
        [MaxLength(20)]
        public string Role { get; set; } = "student"; // admin, teacher, student

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }

    // 2. teachers
    [Table("teachers")]
    public class Teacher
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        [Column("bio")]
        public string? Bio { get; set; }

        [Column("qualification")]
        [MaxLength(200)]
        public string? Qualification { get; set; }

        [Column("is_verified")]
        public bool IsVerified { get; set; } = false;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }

    // 3. subjects
    [Table("subjects")]
    public class Subject
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("name")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Column("description")]
        public string? Description { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }

    // 4. courses
    [Table("courses")]
    public class Course
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("subject_id")]
        public int SubjectId { get; set; }

        [ForeignKey("SubjectId")]
        public Subject? Subject { get; set; }

        [Required]
        [Column("title")]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Column("description")]
        public string? Description { get; set; }

        [Column("thumbnail_url")]
        public string? ThumbnailUrl { get; set; }

        [Column("level")]
        [MaxLength(20)]
        public string Level { get; set; } = "beginner"; // beginner, intermediate, advanced

        [Column("price")]
        public decimal Price { get; set; } = 0;

        [Column("is_published")]
        public bool IsPublished { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }

    // 5. lessons
    [Table("lessons")]
    public class Lesson
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("course_id")]
        public int CourseId { get; set; }

        [ForeignKey("CourseId")]
        public Course? Course { get; set; }

        [Required]
        [Column("title")]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Column("order_index")]
        public int OrderIndex { get; set; }

        [Column("content_type")]
        [MaxLength(20)]
        public string ContentType { get; set; } = "vocabulary"; // vocabulary, grammar, listening, reading, speaking, writing, quiz

        [Column("content")]
        public string? Content { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }

    // 6. classrooms
    [Table("classrooms")]
    public class Classroom
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("name")]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [Column("location")]
        [MaxLength(100)]
        public string? Location { get; set; }

        [Column("capacity")]
        public int Capacity { get; set; } = 30;

        [Column("description")]
        public string? Description { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    // 7. time_slots
    [Table("time_slots")]
    public class TimeSlot
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("name")]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [Column("start_time")]
        public TimeSpan StartTime { get; set; }

        [Column("end_time")]
        public TimeSpan EndTime { get; set; }

        [Column("description")]
        public string? Description { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;
    }

    // 8. course_classes
    [Table("course_classes")]
    public class CourseClass
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("course_id")]
        public int CourseId { get; set; }

        [ForeignKey("CourseId")]
        public Course? Course { get; set; }

        [Required]
        [Column("class_code")]
        [MaxLength(50)]
        public string ClassCode { get; set; } = string.Empty;

        [Required]
        [Column("class_name")]
        [MaxLength(150)]
        public string ClassName { get; set; } = string.Empty;

        [Column("start_date")]
        public DateTime StartDate { get; set; }

        [Column("end_date")]
        public DateTime EndDate { get; set; }

        [Column("max_students")]
        public int MaxStudents { get; set; } = 20;

        [Column("price")]
        public decimal Price { get; set; } = 0;

        [Column("status")]
        [MaxLength(20)]
        public string Status { get; set; } = "upcoming"; // upcoming, ongoing, completed, cancelled

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }

    // 9. class_teachers
    [Table("class_teachers")]
    public class ClassTeacher
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("class_id")]
        public int ClassId { get; set; }

        [Column("teacher_id")]
        public int TeacherId { get; set; }

        [Column("role")]
        [MaxLength(20)]
        public string Role { get; set; } = "main"; // main, assistant

        [Column("assigned_at")]
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    }

    // 10. class_schedules
    [Table("class_schedules")]
    public class ClassSchedule
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("class_id")]
        public int ClassId { get; set; }

        [Column("classroom_id")]
        public int ClassroomId { get; set; }

        [Column("time_slot_id")]
        public int TimeSlotId { get; set; }

        [Column("day_of_week")]
        public int DayOfWeek { get; set; } // 1 - 7 (Thứ 2 - Chủ Nhật)

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    // 11. class_lessons
    [Table("class_lessons")]
    public class ClassLesson
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("class_id")]
        public int ClassId { get; set; }

        [Column("lesson_id")]
        public int LessonId { get; set; }

        [Column("schedule_id")]
        public int? ScheduleId { get; set; }

        [Column("teacher_id")]
        public int? TeacherId { get; set; }

        [Column("schedule_date")]
        public DateTime? ScheduleDate { get; set; }

        [Column("status")]
        [MaxLength(20)]
        public string Status { get; set; } = "scheduled"; // scheduled, completed, cancelled

        [Column("notes")]
        public string? Notes { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    // 12. enrollments
    [Table("enrollments")]
    public class Enrollment
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("class_id")]
        public int ClassId { get; set; }

        [Column("enrolled_at")]
        public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;

        [Column("status")]
        [MaxLength(20)]
        public string Status { get; set; } = "active"; // active, completed, cancelled
    }

    // 13. user_progress
    [Table("user_progress")]
    public class UserProgress
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("lesson_id")]
        public int LessonId { get; set; }

        [Column("score")]
        public decimal? Score { get; set; }

        [Column("is_completed")]
        public bool IsCompleted { get; set; } = false;

        [Column("completed_at")]
        public DateTime? CompletedAt { get; set; }
    }

    // 14. course_reviews
    [Table("course_reviews")]
    public class CourseReview
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("course_id")]
        public int CourseId { get; set; }

        [Column("rating")]
        public int Rating { get; set; } = 5; // 1 - 5 sao

        [Column("comment")]
        public string? Comment { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    // 15. flashcards
    [Table("flashcards")]
    public class Flashcard
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Required]
        [Column("word")]
        [MaxLength(100)]
        public string Word { get; set; } = string.Empty;

        [Required]
        [Column("meaning")]
        public string Meaning { get; set; } = string.Empty;

        [Column("phonetic")]
        [MaxLength(100)]
        public string? Phonetic { get; set; }

        [Column("audio_url")]
        public string? AudioUrl { get; set; }

        [Column("example_sentence")]
        public string? ExampleSentence { get; set; }

        [Column("next_review_at")]
        public DateTime? NextReviewAt { get; set; }

        [Column("interval_days")]
        public int IntervalDays { get; set; } = 1;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    // 16. questions
    [Table("questions")]
    public class Question
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("lesson_id")]
        public int? LessonId { get; set; }

        [Required]
        [Column("question_text")]
        public string QuestionText { get; set; } = string.Empty;

        [Column("audio_url")]
        public string? AudioUrl { get; set; }

        [Column("question_type")]
        [MaxLength(30)]
        public string QuestionType { get; set; } = "multiple_choice"; // multiple_choice, fill_blank, pronunciation, essay

        [Column("default_points")]
        public decimal DefaultPoints { get; set; } = 1.0m;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }

    // 17. question_options
    [Table("question_options")]
    public class QuestionOption
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("question_id")]
        public int QuestionId { get; set; }

        [Required]
        [Column("option_text")]
        public string OptionText { get; set; } = string.Empty;

        [Column("is_correct")]
        public bool IsCorrect { get; set; } = false;
    }

    // 18. question_answers
    [Table("question_answers")]
    public class QuestionAnswer
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("question_id")]
        public int QuestionId { get; set; }

        [Required]
        [Column("answer_text")]
        public string AnswerText { get; set; } = string.Empty;

        [Column("reference_audio_url")]
        public string? ReferenceAudioUrl { get; set; }

        [Column("is_case_sensitive")]
        public bool IsCaseSensitive { get; set; } = false;
    }

    // 19. exams
    [Table("exams")]
    public class Exam
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("course_id")]
        public int? CourseId { get; set; }

        [Column("created_by_teacher_id")]
        public int CreatedByTeacherId { get; set; }

        [Required]
        [Column("title")]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Column("description")]
        public string? Description { get; set; }

        [Column("duration_minutes")]
        public int DurationMinutes { get; set; } = 45;

        [Column("pass_score")]
        public decimal PassScore { get; set; } = 5.0m;

        [Column("max_attempts")]
        public int? MaxAttempts { get; set; }

        [Column("is_published")]
        public bool IsPublished { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }

    // 20. exam_questions
    [Table("exam_questions")]
    public class ExamQuestion
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("exam_id")]
        public int ExamId { get; set; }

        [Column("question_id")]
        public int QuestionId { get; set; }

        [Column("points")]
        public decimal Points { get; set; } = 1.0m;

        [Column("order_index")]
        public int OrderIndex { get; set; }
    }

    // 21. user_exams
    [Table("user_exams")]
    public class UserExam
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("exam_id")]
        public int ExamId { get; set; }

        [Column("attempt_number")]
        public int AttemptNumber { get; set; } = 1;

        [Column("started_at")]
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;

        [Column("submitted_at")]
        public DateTime? SubmittedAt { get; set; }

        [Column("total_score")]
        public decimal? TotalScore { get; set; }

        [Column("status")]
        [MaxLength(20)]
        public string Status { get; set; } = "in_progress"; // in_progress, submitted, passed, failed
    }

    // 22. user_exam_answers
    [Table("user_exam_answers")]
    public class UserExamAnswer
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("user_exam_id")]
        public int UserExamId { get; set; }

        [Column("question_id")]
        public int QuestionId { get; set; }

        [Column("selected_option_id")]
        public int? SelectedOptionId { get; set; }

        [Column("text_answer")]
        public string? TextAnswer { get; set; }

        [Column("score_earned")]
        public decimal? ScoreEarned { get; set; }

        [Column("grading_status")]
        [MaxLength(20)]
        public string GradingStatus { get; set; } = "auto_graded"; // auto_graded, pending_review, graded

        [Column("graded_by_teacher_id")]
        public int? GradedByTeacherId { get; set; }

        [Column("graded_at")]
        public DateTime? GradedAt { get; set; }

        [Column("feedback")]
        public string? Feedback { get; set; }
    }
}
