namespace CNPM_EnglishLearning.Areas.Teacher.Models
{
    public class GradingModel
    {
        public int Id { get; set; }
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
