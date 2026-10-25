using CNPM_EnglishLearning.Areas.Admin.Models;
using CNPM_EnglishLearning.Areas.Teacher.Models;
using CNPM_EnglishLearning.Areas.Student.Models;

namespace CNPM_EnglishLearning.Data
{
    /// <summary>
    /// ApplicationDbContext - Quản lý truy xuất dữ liệu toàn hệ thống
    /// Sẵn sàng kết nối SQL Server thông qua Entity Framework Core
    /// </summary>
    public class ApplicationDbContext
    {
        public List<UserModel> Users { get; set; } = new();
        public List<TeacherModel> Teachers { get; set; } = new();
        public List<SubjectModel> Subjects { get; set; } = new();
        public List<CourseModel> Courses { get; set; } = new();
        public List<ClassModel> Classes { get; set; } = new();
        public List<ClassroomModel> Classrooms { get; set; } = new();
        public List<TimeSlotModel> TimeSlots { get; set; } = new();
        public List<EnrollmentModel> Enrollments { get; set; } = new();
        public List<ExamModel> Exams { get; set; } = new();
        public List<ReviewModel> Reviews { get; set; } = new();
        public List<LessonModel> Lessons { get; set; } = new();
        public List<QuestionModel> Questions { get; set; } = new();
        public List<GradingModel> Gradings { get; set; } = new();
        public List<FlashcardModel> Flashcards { get; set; } = new();
        public ProgressModel Progress { get; set; } = new();

        public ApplicationDbContext()
        {
            DbInitializer.Seed(this);
        }
    }
}
