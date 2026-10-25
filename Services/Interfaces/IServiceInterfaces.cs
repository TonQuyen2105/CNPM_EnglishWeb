using CNPM_EnglishLearning.Models.Entities;

namespace CNPM_EnglishLearning.Services.Interfaces
{
    public interface IAuthService
    {
        User? Authenticate(string email, string password);
        bool ChangePassword(int userId, string oldPassword, string newPassword);
    }

    public interface ICourseService
    {
        List<Course> GetAllCourses();
        Course? GetCourseById(int id);
        List<CourseClass> GetClassesByCourseId(int courseId);
    }

    public interface IExamService
    {
        List<Exam> GetAvailableExams();
        Exam? GetExamById(int id);
        List<Question> GetQuestionsByExamId(int examId);
        bool SubmitExam(UserExam userExam, List<UserExamAnswer> answers);
    }
}
