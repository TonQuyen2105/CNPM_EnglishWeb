using CNPM_EnglishLearning.Models.Entities;

namespace CNPM_EnglishLearning.Data
{
    public static class DbInitializer
    {
        public static void Seed(ApplicationDbContext context)
        {
            if (context.Users.Any()) return;

            var pastMonth = DateTime.UtcNow.AddMonths(-1);
            var now = DateTime.UtcNow;

            // 1. users: 1 Admin, 6 Giáo viên, 40 Học sinh
            context.Users.AddRange(new List<User>
            {
                new User { Id = 1, Email = "admin@el.com", Password = "123456", FullName = "Nguyễn Quản Trị", Role = "admin", IsActive = true, CreatedAt = pastMonth },
                new User { Id = 2, Email = "teacher@el.com", Password = "123456", FullName = "Lê Thị Mai", Role = "teacher", IsActive = true, CreatedAt = pastMonth },
                new User { Id = 3, Email = "hung.nguyen@el.com", Password = "123456", FullName = "Nguyễn Văn Hùng", Role = "teacher", IsActive = true, CreatedAt = pastMonth },
                new User { Id = 4, Email = "huy.pham@el.com", Password = "123456", FullName = "Phạm Quang Huy", Role = "teacher", IsActive = true, CreatedAt = pastMonth },
                new User { Id = 5, Email = "student@el.com", Password = "123456", FullName = "Nguyễn Hoàng Anh", Role = "student", IsActive = true, CreatedAt = pastMonth },
                new User { Id = 6, Email = "nam.tran@el.com", Password = "123456", FullName = "Trần Phương Nam", Role = "student", IsActive = true, CreatedAt = pastMonth },
                new User { Id = 7, Email = "ngan.le@el.com", Password = "123456", FullName = "Lê Thị Kim Ngân", Role = "student", IsActive = false, CreatedAt = pastMonth }
            });

            // 2. teachers: liên kết với users
            context.Teachers.AddRange(new List<Teacher>
            {
                new Teacher { Id = 1, UserId = 2, Bio = "Chuyên gia đào tạo kỹ năng Speaking và Writing học thuật", Qualification = "IELTS 8.5, ThS. TESOL", IsVerified = true, CreatedAt = pastMonth },
                new Teacher { Id = 2, UserId = 3, Bio = "Hơn 8 năm kinh nghiệm giảng dạy tiếng Anh giao tiếp", Qualification = "Thạc sĩ ĐH Ngoại Ngữ", IsVerified = true, CreatedAt = pastMonth },
                new Teacher { Id = 3, UserId = 4, Bio = "Giảng viên chuyên môn TOEIC Cấp Tốc", Qualification = "TOEIC 980", IsVerified = false, CreatedAt = pastMonth }
            });

            // 3. subjects: 4 môn học
            context.Subjects.AddRange(new List<Subject>
            {
                new Subject { Id = 1, Name = "Tiếng Anh Giao Tiếp", Description = "Luyện phản xạ, phát âm chuẩn IPA, đàm thoại tự tin", IsActive = true, CreatedAt = pastMonth },
                new Subject { Id = 2, Name = "Luyện Thi TOEIC", Description = "Luyện đề 2 kỹ năng và 4 kỹ năng cam kết đầu ra", IsActive = true, CreatedAt = pastMonth },
                new Subject { Id = 3, Name = "Luyện Thi IELTS", Description = "Lộ trình từ 4.5 đến Master 7.5+ toàn diện 4 kỹ năng", IsActive = true, CreatedAt = pastMonth },
                new Subject { Id = 4, Name = "Tiếng Anh Thiếu Nhi", Description = "Phương pháp tương tác cho trẻ 6-12 tuổi", IsActive = true, CreatedAt = pastMonth }
            });

            // 4. courses: Khóa học
            context.Courses.AddRange(new List<Course>
            {
                new Course { Id = 1, SubjectId = 1, Title = "Tiếng Anh Giao Tiếp Cơ Bản", Description = "Khóa học nền tảng cho người mới bắt đầu", Level = "beginner", Price = 2500000, IsPublished = true, CreatedAt = pastMonth },
                new Course { Id = 2, SubjectId = 3, Title = "Luyện Thi IELTS 6.5+", Description = "Chiến lược Writing Task 2 & Speaking Mock Test", Level = "advanced", Price = 5800000, IsPublished = true, CreatedAt = pastMonth },
                new Course { Id = 3, SubjectId = 2, Title = "TOEIC 750+ Cấp Tốc", Description = "Bộ bẫy Part 5, 6, 7 và kỹ năng nghe bắt từ khóa", Level = "intermediate", Price = 3200000, IsPublished = true, CreatedAt = pastMonth }
            });

            // 5. classrooms
            context.Classrooms.AddRange(new List<Classroom>
            {
                new Classroom { Id = 1, Name = "Phòng LAB-01", Location = "Tầng 1", Capacity = 25, Description = "Phòng máy tính tai nghe Sony", IsActive = true, CreatedAt = pastMonth },
                new Classroom { Id = 2, Name = "Phòng A101", Location = "Tầng 2", Capacity = 30, Description = "Phòng học lý thuyết máy chiếu 4K", IsActive = true, CreatedAt = pastMonth },
                new Classroom { Id = 3, Name = "Phòng B202", Location = "Tầng 2", Capacity = 60, Description = "Hội trường tổ chức thi thử", IsActive = true, CreatedAt = pastMonth }
            });

            // 6. time_slots
            context.TimeSlots.AddRange(new List<TimeSlot>
            {
                new TimeSlot { Id = 1, Name = "Ca Sáng 1", StartTime = new TimeSpan(8, 30, 0), EndTime = new TimeSpan(10, 30, 0), IsActive = true },
                new TimeSlot { Id = 2, Name = "Ca Tối 1", StartTime = new TimeSpan(18, 0, 0), EndTime = new TimeSpan(19, 30, 0), IsActive = true },
                new TimeSlot { Id = 3, Name = "Ca Tối 2", StartTime = new TimeSpan(19, 30, 0), EndTime = new TimeSpan(21, 0, 0), IsActive = true }
            });

            // 7. course_classes
            context.CourseClasses.AddRange(new List<CourseClass>
            {
                new CourseClass { Id = 1, CourseId = 1, ClassCode = "COMM-01", ClassName = "Giao Tiếp Cơ Bản K01", StartDate = now.AddDays(-20), EndDate = now.AddMonths(2), MaxStudents = 20, Price = 2500000, Status = "ongoing", CreatedAt = pastMonth },
                new CourseClass { Id = 2, CourseId = 2, ClassCode = "IELTS-B01", ClassName = "Luyện Thi IELTS 6.5+ Sáng", StartDate = now.AddDays(-15), EndDate = now.AddMonths(2), MaxStudents = 15, Price = 5800000, Status = "ongoing", CreatedAt = pastMonth },
                new CourseClass { Id = 3, CourseId = 3, ClassCode = "TOEIC-A01", ClassName = "TOEIC Cấp Tốc Tối", StartDate = now.AddDays(5), EndDate = now.AddMonths(3), MaxStudents = 25, Price = 3200000, Status = "upcoming", CreatedAt = pastMonth }
            });

            // 8. class_teachers
            context.ClassTeachers.AddRange(new List<ClassTeacher>
            {
                new ClassTeacher { Id = 1, ClassId = 1, TeacherId = 1, Role = "main", AssignedAt = pastMonth },
                new ClassTeacher { Id = 2, ClassId = 2, TeacherId = 1, Role = "main", AssignedAt = pastMonth },
                new ClassTeacher { Id = 3, ClassId = 3, TeacherId = 2, Role = "main", AssignedAt = pastMonth }
            });

            // 9. class_schedules
            context.ClassSchedules.AddRange(new List<ClassSchedule>
            {
                new ClassSchedule { Id = 1, ClassId = 1, ClassroomId = 1, TimeSlotId = 2, DayOfWeek = 2, CreatedAt = pastMonth },
                new ClassSchedule { Id = 2, ClassId = 1, ClassroomId = 1, TimeSlotId = 2, DayOfWeek = 4, CreatedAt = pastMonth },
                new ClassSchedule { Id = 3, ClassId = 1, ClassroomId = 1, TimeSlotId = 2, DayOfWeek = 6, CreatedAt = pastMonth }
            });

            // 10. enrollments (LƯU Ý: Không có bảng thanh toán, doanh thu ước tính lấy từ price của lớp)
            context.Enrollments.AddRange(new List<Enrollment>
            {
                new Enrollment { Id = 1, UserId = 5, ClassId = 1, EnrolledAt = now.AddDays(-18), Status = "active" },
                new Enrollment { Id = 2, UserId = 6, ClassId = 2, EnrolledAt = now.AddDays(-14), Status = "active" },
                new Enrollment { Id = 3, UserId = 7, ClassId = 3, EnrolledAt = now.AddDays(-2), Status = "active" }
            });

            // 11. flashcards
            context.Flashcards.AddRange(new List<Flashcard>
            {
                new Flashcard { Id = 1, UserId = 5, Word = "Negotiation", Meaning = "Sự đàm phán, thương lượng", Phonetic = "/nɪˌɡoʊ.ʃiˈeɪ.ʃən/", ExampleSentence = "The agreement was reached after months of tough negotiations.", IntervalDays = 1, CreatedAt = pastMonth },
                new Flashcard { Id = 2, UserId = 5, Word = "Comprehensive", Meaning = "Toàn diện, bao quát", Phonetic = "/ˌkɑːm.prəˈhen.sɪv/", ExampleSentence = "We offer a comprehensive training programme for all new staff.", IntervalDays = 3, CreatedAt = pastMonth }
            });

            Console.WriteLine($"[DbInitializer] Đã nạp thành công dữ liệu mẫu vào ApplicationDbContext: {context.Users.Count} Users, {context.Teachers.Count} Teachers, {context.Courses.Count} Courses, {context.Enrollments.Count} Enrollments.");
        }
    }
}
