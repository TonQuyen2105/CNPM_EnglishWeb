using CNPM_EnglishLearning.Models.Entities;

namespace CNPM_EnglishLearning.Areas.Admin.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalStudents { get; set; }
        public int TotalTeachers { get; set; }
        public int OngoingClasses { get; set; }
        public decimal EstimatedRevenue { get; set; }
        public List<Enrollment> RecentEnrollments { get; set; } = new();
    }

    public class UserManagementViewModel
    {
        public List<User> Users { get; set; } = new();
        public string? SearchTerm { get; set; }
        public string? RoleFilter { get; set; }
    }

    public class ReportViewModel
    {
        public decimal TotalRevenue { get; set; }
        public int TotalStudents { get; set; }
        public int TotalClasses { get; set; }
        public double CompletionRate { get; set; }
    }
}
