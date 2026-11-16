using CNPM_EnglishLearning.Areas.Student.Services.Implementations;
using CNPM_EnglishLearning.Areas.Student.Services.Interfaces;
using CNPM_EnglishLearning.Data;

namespace CNPM_EnglishLearning.Areas.Student.Services
{
    /// <summary>
    /// OOP Service Factory & Facade Pattern:
    /// Khởi tạo lười (Lazy initialization) và tái sử dụng các instance Service trên cùng 1 phiên ApplicationDbContext.
    /// </summary>
    public class StudentServiceFactory
    {
        private readonly ApplicationDbContext _db;

        private IStudentDashboardService? _dashboardService;
        private IStudentCourseService? _courseService;
        private IStudentClassScheduleService? _classScheduleService;
        private IStudentLearningService? _learningService;
        private IStudentFlashcardService? _flashcardService;
        private IStudentExamService? _examService;
        private IStudentAnalyticsService? _analyticsService;
        private IStudentProfileService? _profileService;

        public StudentServiceFactory(ApplicationDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public IStudentDashboardService Dashboard => _dashboardService ??= new StudentDashboardService(_db);
        public IStudentCourseService Courses => _courseService ??= new StudentCourseService(_db);
        public IStudentClassScheduleService ClassSchedule => _classScheduleService ??= new StudentClassScheduleService(_db);
        public IStudentLearningService Learning => _learningService ??= new StudentLearningService(_db);
        public IStudentFlashcardService Flashcards => _flashcardService ??= new StudentFlashcardService(_db);
        public IStudentExamService Exams => _examService ??= new StudentExamService(_db);
        public IStudentAnalyticsService Analytics => _analyticsService ??= new StudentAnalyticsService(_db);
        public IStudentProfileService Profile => _profileService ??= new StudentProfileService(_db);
    }
}
