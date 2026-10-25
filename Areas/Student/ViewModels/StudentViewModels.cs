using CNPM_EnglishLearning.Models.Entities;

namespace CNPM_EnglishLearning.Areas.Student.ViewModels
{
    public class StudentDashboardViewModel
    {
        public int EnrolledClassesCount { get; set; }
        public int MasteredVocabularies { get; set; } = 180;
        public double AverageScore { get; set; } = 8.5;
        public int StudyStreakDays { get; set; } = 5;
        public List<CourseClass> OngoingClasses { get; set; } = new();
    }

    public class ProgressViewModel
    {
        public int CompletedLessons { get; set; } = 8;
        public int TotalLessons { get; set; } = 24;
        public double ListeningScore { get; set; } = 8.5;
        public double ReadingScore { get; set; } = 9.0;
        public double SpeakingScore { get; set; } = 8.0;
        public double WritingScore { get; set; } = 7.8;
    }
}
