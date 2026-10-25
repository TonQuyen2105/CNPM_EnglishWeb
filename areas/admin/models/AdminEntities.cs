using System.ComponentModel.DataAnnotations;

namespace CNPM_EnglishLearning.Areas.Admin.Models
{
    public class ClassModel : BaseModel
    {
        public int CourseId { get; set; }
        public CourseModel? Course { get; set; }

        public int? TeacherId { get; set; }
        public TeacherModel? Teacher { get; set; }

        public int? ClassroomId { get; set; }
        public ClassroomModel? Classroom { get; set; }

        [Required]
        [MaxLength(50)]
        public string ClassCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string ClassName { get; set; } = string.Empty;

        public DateTime StartDate { get; set; } = DateTime.UtcNow;
        public DateTime EndDate { get; set; } = DateTime.UtcNow.AddMonths(3);

        public int MaxStudents { get; set; } = 20;
        public int EnrolledCount { get; set; } = 0;

        public string Status { get; set; } = "upcoming"; // upcoming, ongoing, completed, cancelled
    }

    public class ClassroomModel : BaseModel
    {
        [Required]
        [MaxLength(50)]
        public string RoomNumber { get; set; } = string.Empty;

        public int Capacity { get; set; } = 30;
        public string Equipment { get; set; } = "Máy chiếu, Máy lạnh, Loa trợ giảng";
        public string Status { get; set; } = "available"; // available, occupied, maintenance
    }

    public class TimeSlotModel : BaseModel
    {
        public string ShiftName { get; set; } = "Ca 1";
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
    }

    public class ScheduleModel : BaseModel
    {
        public int ClassId { get; set; }
        public ClassModel? Class { get; set; }

        public int ClassroomId { get; set; }
        public int TimeSlotId { get; set; }
        public int DayOfWeek { get; set; } // 2: Monday, ..., 8: Sunday
    }

    public class EnrollmentModel : BaseModel
    {
        public int UserId { get; set; }
        public UserModel? User { get; set; }

        public int ClassId { get; set; }
        public ClassModel? Class { get; set; }

        public decimal PaidAmount { get; set; }
        public string PaymentMethod { get; set; } = "BankTransfer";
        public string Status { get; set; } = "completed"; // completed, pending, refunded
    }

    public class ExamModel : BaseModel
    {
        public string Title { get; set; } = string.Empty;
        public string SubjectCode { get; set; } = string.Empty;
        public int DurationMinutes { get; set; } = 45;
        public int TotalQuestions { get; set; } = 40;
        public decimal PassingScore { get; set; } = 5.0m;
        public string Status { get; set; } = "published";
    }

    public class ReviewModel : BaseModel
    {
        public int UserId { get; set; }
        public UserModel? User { get; set; }

        public int CourseId { get; set; }
        public int Rating { get; set; } = 5; // 1-5
        public string Comment { get; set; } = string.Empty;
        public bool IsApproved { get; set; } = true;
    }

    public class ReportModel
    {
        public decimal TotalRevenue { get; set; }
        public int TotalStudents { get; set; }
        public int TotalClasses { get; set; }
        public double CompletionRate { get; set; }
    }
}
