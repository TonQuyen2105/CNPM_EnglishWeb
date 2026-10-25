using CNPM_EnglishLearning.Areas.Admin.Models;

namespace CNPM_EnglishLearning.Data
{
    /// <summary>
    /// ApplicationDbContext - Khớp 100% 22 bảng cơ sở dữ liệu SQL Server theo yêu cầu
    /// Không chứa bảng thanh toán và điểm danh (Doanh thu = tổng price của enrollments)
    /// </summary>
    public class ApplicationDbContext
    {
        public List<User> Users { get; set; } = new();
        public List<Teacher> Teachers { get; set; } = new();
        public List<Subject> Subjects { get; set; } = new();
        public List<Course> Courses { get; set; } = new();
        public List<Lesson> Lessons { get; set; } = new();
        public List<Classroom> Classrooms { get; set; } = new();
        public List<TimeSlot> TimeSlots { get; set; } = new();
        public List<CourseClass> CourseClasses { get; set; } = new();
        public List<ClassTeacher> ClassTeachers { get; set; } = new();
        public List<ClassSchedule> ClassSchedules { get; set; } = new();
        public List<ClassLesson> ClassLessons { get; set; } = new();
        public List<Enrollment> Enrollments { get; set; } = new();
        public List<UserProgress> UserProgresses { get; set; } = new();
        public List<CourseReview> CourseReviews { get; set; } = new();
        public List<Flashcard> Flashcards { get; set; } = new();
        public List<Question> Questions { get; set; } = new();
        public List<QuestionOption> QuestionOptions { get; set; } = new();
        public List<QuestionAnswer> QuestionAnswers { get; set; } = new();
        public List<Exam> Exams { get; set; } = new();
        public List<ExamQuestion> ExamQuestions { get; set; } = new();
        public List<UserExam> UserExams { get; set; } = new();
        public List<UserExamAnswer> UserExamAnswers { get; set; } = new();

        public ApplicationDbContext()
        {
            DbInitializer.Seed(this);
        }

        // Tính doanh thu ước tính theo quy tắc: tổng price của các enrollment
        public decimal CalculateEstimatedRevenue()
        {
            return (from e in Enrollments
                    join c in CourseClasses on e.ClassId equals c.Id
                    select c.Price).Sum();
        }
    }
}
