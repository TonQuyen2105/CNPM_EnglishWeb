using CNPM_EnglishLearning.Models.Entities;

namespace CNPM_EnglishLearning.Areas.Teacher.ViewModels
{
    public class TeacherDashboardViewModel
    {
        public int ActiveClassesCount { get; set; }
        public int TotalStudentsCount { get; set; }
        public int PendingGradingCount { get; set; }
        public double AverageRating { get; set; } = 4.9;
        public List<CourseClass> TodayClasses { get; set; } = new();
    }

    public class GradingViewModel
    {
        public int ExamId { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string SubmissionText { get; set; } = string.Empty;
        public decimal TaskAchievement { get; set; } = 7.0m;
        public decimal CoherenceCohesion { get; set; } = 7.0m;
        public decimal LexicalResource { get; set; } = 7.0m;
        public decimal GrammarAccuracy { get; set; } = 7.0m;
        public decimal FinalScore { get; set; } = 7.0m;
        public string TeacherFeedback { get; set; } = string.Empty;
    }
}
