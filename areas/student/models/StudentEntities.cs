using System.ComponentModel.DataAnnotations;

namespace CNPM_EnglishLearning.Areas.Student.Models
{
    public class FlashcardModel
    {
        public int Id { get; set; }
        public string DeckName { get; set; } = "TOEIC 600 Essential Words";
        public string Vocabulary { get; set; } = string.Empty;
        public string Phonetic { get; set; } = string.Empty;
        public string WordType { get; set; } = "Noun";
        public string MeaningVi { get; set; } = string.Empty;
        public string ExampleSentence { get; set; } = string.Empty;

        // Spaced Repetition System (SRS)
        public int RepetitionLevel { get; set; } = 1;
        public DateTime NextReviewAt { get; set; } = DateTime.UtcNow;
        public bool IsMastered { get; set; } = false;
    }

    public class ProgressModel
    {
        public int StudentId { get; set; }
        public int CompletedLessons { get; set; } = 8;
        public int TotalLessons { get; set; } = 24;
        public int MasteredVocabularies { get; set; } = 180;
        public int StudyStreakDays { get; set; } = 5;
        public double ListeningSkillScore { get; set; } = 8.5;
        public double ReadingSkillScore { get; set; } = 9.0;
        public double SpeakingSkillScore { get; set; } = 8.0;
        public double WritingSkillScore { get; set; } = 7.8;
    }

    public class StudentExamResultModel
    {
        public int Id { get; set; }
        public string ExamTitle { get; set; } = string.Empty;
        public DateTime TakenAt { get; set; } = DateTime.UtcNow;
        public int TimeSpentMinutes { get; set; } = 38;
        public int CorrectAnswers { get; set; } = 35;
        public int TotalQuestions { get; set; } = 40;
        public decimal Score { get; set; } = 8.8m;
        public string Grade { get; set; } = "Pass";
    }

    public class StudentProfileModel
    {
        public int Id { get; set; }
        public string FullName { get; set; } = "Nguyễn Hoàng Anh";
        public string Email { get; set; } = "student@el.com";
        public string PhoneNumber { get; set; } = "0987 654 321";
        public string TargetGoal { get; set; } = "Giao tiếp lưu loát & IELTS 6.5+";
    }
}
