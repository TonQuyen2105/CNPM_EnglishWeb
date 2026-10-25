using System.ComponentModel.DataAnnotations;

namespace CNPM_EnglishLearning.Areas.Teacher.Models
{
    public class LessonModel
    {
        public int Id { get; set; }
        public int CourseId { get; set; }
        public int OrderIndex { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;
        public string VideoUrl { get; set; } = string.Empty;
        public string AttachmentPdf { get; set; } = string.Empty;
        public int DurationMinutes { get; set; } = 45;
    }

    public class QuestionModel
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public string QuestionType { get; set; } = "multiple_choice"; // multiple_choice, essay, true_false
        public string Difficulty { get; set; } = "medium"; // easy, medium, hard
        public decimal Points { get; set; } = 1.0m;
        public List<string> Options { get; set; } = new();
        public string CorrectAnswer { get; set; } = string.Empty;
    }

    public class GradingModel
    {
        public int Id { get; set; }
        public int ExamId { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string SubmissionText { get; set; } = string.Empty;

        // Rubric Criteria (IELTS / General)
        public decimal TaskAchievement { get; set; } = 7.0m;
        public decimal CoherenceCohesion { get; set; } = 7.0m;
        public decimal LexicalResource { get; set; } = 7.0m;
        public decimal GrammarAccuracy { get; set; } = 7.0m;
        public decimal FinalScore { get; set; } = 7.0m;

        public string TeacherFeedback { get; set; } = string.Empty;
        public bool IsGraded { get; set; } = false;
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    }

    public class TeacherProfileModel
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string HighestDegree { get; set; } = string.Empty;
        public string Certifications { get; set; } = string.Empty;
        public int YearsExperience { get; set; } = 0;
        public string Bio { get; set; } = string.Empty;
    }
}
