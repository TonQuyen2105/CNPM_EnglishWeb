using CNPM_EnglishLearning.Areas.Admin.Models;
using CNPM_EnglishLearning.Areas.Teacher.Models;
using CNPM_EnglishLearning.Areas.Student.Models;

namespace CNPM_EnglishLearning.Data
{
    public static class DbInitializer
    {
        public static void Seed(ApplicationDbContext context)
        {
            if (context.Users.Any()) return;

            // 1. Users
            context.Users.AddRange(new List<UserModel>
            {
                new UserModel { Id = 1, FullName = "Nguyễn Quản Trị", Email = "admin@el.com", Role = "admin", IsActive = true },
                new UserModel { Id = 2, FullName = "Lê Thị Mai", Email = "teacher@el.com", Role = "teacher", IsActive = true },
                new UserModel { Id = 3, FullName = "Nguyễn Văn Hùng", Email = "hung.nguyen@el.com", Role = "teacher", IsActive = true },
                new UserModel { Id = 4, FullName = "Nguyễn Hoàng Anh", Email = "student@el.com", Role = "student", IsActive = true },
                new UserModel { Id = 5, FullName = "Trần Phương Nam", Email = "nam.tran@el.com", Role = "student", IsActive = true }
            });

            // 2. Subjects
            context.Subjects.AddRange(new List<SubjectModel>
            {
                new SubjectModel { Id = 1, Code = "COMM", Name = "Tiếng Anh Giao Tiếp", Description = "Phát âm IPA và phản xạ giao tiếp tự tin", CourseCount = 4 },
                new SubjectModel { Id = 2, Code = "TOEIC", Name = "Luyện Thi TOEIC", Description = "Luyện đề 2 kỹ năng và 4 kỹ năng cam kết đầu ra", CourseCount = 3 },
                new SubjectModel { Id = 3, Code = "IELTS", Name = "Luyện Thi IELTS", Description = "Lộ trình từ 4.5 đến Master 7.5+", CourseCount = 3 },
                new SubjectModel { Id = 4, Code = "KIDS", Name = "Tiếng Anh Thiếu Nhi", Description = "Học qua trò chơi cho trẻ 6-12 tuổi", CourseCount = 2 }
            });

            // 3. Courses
            context.Courses.AddRange(new List<CourseModel>
            {
                new CourseModel { Id = 1, SubjectId = 1, Title = "Tiếng Anh Giao Tiếp Cơ Bản", Level = "beginner", Price = 2500000, LessonCount = 24, IsPublished = true },
                new CourseModel { Id = 2, SubjectId = 3, Title = "Luyện Thi IELTS 6.5+", Level = "advanced", Price = 5800000, LessonCount = 36, IsPublished = true },
                new CourseModel { Id = 3, SubjectId = 2, Title = "TOEIC 750+ Cấp Tốc", Level = "intermediate", Price = 3200000, LessonCount = 30, IsPublished = true }
            });

            // 4. Classes
            context.Classes.AddRange(new List<ClassModel>
            {
                new ClassModel { Id = 1, CourseId = 1, TeacherId = 1, ClassCode = "COMM-01", ClassName = "Giao Tiếp Cơ Bản K01", MaxStudents = 20, EnrolledCount = 18, Status = "ongoing" },
                new ClassModel { Id = 2, CourseId = 2, TeacherId = 1, ClassCode = "IELTS-B01", ClassName = "Luyện Thi IELTS 6.5+ Sáng", MaxStudents = 15, EnrolledCount = 15, Status = "ongoing" }
            });

            // 5. Flashcards
            context.Flashcards.AddRange(new List<FlashcardModel>
            {
                new FlashcardModel { Id = 1, Vocabulary = "Negotiation", Phonetic = "/nɪˌɡoʊ.ʃiˈeɪ.ʃən/", WordType = "Noun", MeaningVi = "Sự đàm phán, thương lượng", ExampleSentence = "The agreement was reached after months of tough negotiations." },
                new FlashcardModel { Id = 2, Vocabulary = "Comprehensive", Phonetic = "/ˌkɑːm.prəˈhen.sɪv/", WordType = "Adjective", MeaningVi = "Toàn diện, bao quát", ExampleSentence = "We offer a comprehensive training programme for all new staff." }
            });
        }
    }
}
