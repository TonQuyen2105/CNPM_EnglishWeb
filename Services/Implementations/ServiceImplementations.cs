using CNPM_EnglishLearning.Data;
using CNPM_EnglishLearning.Models.Entities;
using CNPM_EnglishLearning.Services.Interfaces;

namespace CNPM_EnglishLearning.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context = new();

        public User? Authenticate(string email, string password)
        {
            return _context.Users.FirstOrDefault(u => 
                u.Email.Equals(email, StringComparison.OrdinalIgnoreCase) && 
                u.Password == password && 
                u.IsActive);
        }

        public bool ChangePassword(int userId, string oldPassword, string newPassword)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == userId && u.Password == oldPassword);
            if (user == null) return false;
            user.Password = newPassword;
            return true;
        }
    }

    public class CourseService : ICourseService
    {
        private readonly ApplicationDbContext _context = new();

        public List<Course> GetAllCourses() => _context.Courses.Where(c => c.IsPublished).ToList();

        public Course? GetCourseById(int id) => _context.Courses.FirstOrDefault(c => c.Id == id);

        public List<CourseClass> GetClassesByCourseId(int courseId) =>
            _context.CourseClasses.Where(c => c.CourseId == courseId).ToList();
    }

    public class ExamService : IExamService
    {
        private readonly ApplicationDbContext _context = new();

        public List<Exam> GetAvailableExams() => _context.Exams.Where(e => e.IsPublished).ToList();

        public Exam? GetExamById(int id) => _context.Exams.FirstOrDefault(e => e.Id == id);

        public List<Question> GetQuestionsByExamId(int examId)
        {
            var questionIds = _context.ExamQuestions
                .Where(eq => eq.ExamId == examId)
                .OrderBy(eq => eq.OrderIndex)
                .Select(eq => eq.QuestionId)
                .ToList();

            return _context.Questions.Where(q => questionIds.Contains(q.Id)).ToList();
        }

        public bool SubmitExam(UserExam userExam, List<UserExamAnswer> answers)
        {
            userExam.Id = _context.UserExams.Any() ? _context.UserExams.Max(e => e.Id) + 1 : 1;
            userExam.SubmittedAt = DateTime.UtcNow;
            userExam.Status = "submitted";
            _context.UserExams.Add(userExam);

            foreach (var a in answers)
            {
                a.UserExamId = userExam.Id;
                _context.UserExamAnswers.Add(a);
            }
            return true;
        }
    }
}
