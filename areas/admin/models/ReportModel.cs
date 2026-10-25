namespace CNPM_EnglishLearning.Areas.Admin.Models
{
    public class ReportModel
    {
        public decimal TotalRevenue { get; set; }
        public int TotalStudents { get; set; }
        public int TotalClasses { get; set; }
        public double CompletionRate { get; set; }
    }
}
