namespace CNPM_EnglishLearning.Areas.Student.Models
{
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
}
