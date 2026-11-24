using CNPM_EnglishLearning.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CNPM_EnglishLearning.Data;

internal static class TeacherDemoSeeder
{
    public const string Email = "teacher.demo@example.com";

    public static async Task<bool> SeedAsync(ApplicationDbContext db, string password)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();
        // Serialize demo imports so two runs cannot create the same fixture.
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(4031779)");
        if (await db.Users.AnyAsync(user => user.Email.ToLower() == Email))
            return false;

        var teacherUser = new User { Email = Email, FullName = "Giảng viên Demo", Role = "teacher" };
        teacherUser.Password = new PasswordHasher<User>().HashPassword(teacherUser, password);
        var student = new User { Email = "student.teacher-demo@example.com", FullName = "Học viên Demo" };
        student.Password = new PasswordHasher<User>().HashPassword(student, Guid.NewGuid().ToString("N"));
        var subject = new Subject { Name = "[Demo Teacher] Tiếng Anh giao tiếp" };
        var room = new Classroom { Name = "DEMO-TEACHER-ROOM", Capacity = 20 };
        var slot = new TimeSlot { Name = "[Demo Teacher] Ca sáng", StartTime = TimeSpan.FromHours(9), EndTime = TimeSpan.FromHours(10.5) };
        db.AddRange(teacherUser, student, subject, room, slot);
        await db.SaveChangesAsync();

        var teacher = new Teacher { UserId = teacherUser.Id, Bio = "Hồ sơ kiểm thử khu vực Teacher.", IsVerified = true };
        var course = new Course { SubjectId = subject.Id, Title = "[Demo Teacher] Giao tiếp cơ bản", IsPublished = false };
        db.AddRange(teacher, course);
        await db.SaveChangesAsync();

        var today = DateTime.SpecifyKind(DateTime.UtcNow.AddHours(7).Date, DateTimeKind.Utc);
        var courseClass = new CourseClass
        {
            CourseId = course.Id, ClassCode = "DEMO-TEACHER-01", ClassName = "[Demo Teacher] Lớp giao tiếp",
            StartDate = today.AddDays(-7), EndDate = today.AddDays(30), Status = "ongoing"
        };
        var lesson = new Lesson
        {
            CourseId = course.Id, Title = "[Demo Teacher] Giới thiệu bản thân", OrderIndex = 1,
            ContentType = "speaking", Content = "Practice introducing yourself in English."
        };
        var exam = new Exam
        {
            CourseId = course.Id, CreatedByTeacherId = teacher.Id, Title = "[Demo Teacher] Kiểm tra đầu khóa",
            IsPublished = false, DurationMinutes = 20
        };
        db.AddRange(courseClass, lesson, exam);
        await db.SaveChangesAsync();

        var schedule = new ClassSchedule
        {
            ClassId = courseClass.Id, ClassroomId = room.Id, TimeSlotId = slot.Id,
            DayOfWeek = today.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)today.DayOfWeek
        };
        db.AddRange(
            new ClassTeacher { ClassId = courseClass.Id, TeacherId = teacher.Id },
            new Enrollment { ClassId = courseClass.Id, UserId = student.Id },
            schedule);
        await db.SaveChangesAsync();

        db.ClassLessons.Add(new ClassLesson
        {
            ClassId = courseClass.Id, LessonId = lesson.Id, TeacherId = teacher.Id,
            ScheduleId = schedule.Id, ScheduleDate = today
        });
        var question = new Question
        {
            LessonId = lesson.Id, QuestionText = "Introduce yourself in three sentences.",
            QuestionType = "essay", DefaultPoints = 10
        };
        var attempt = new UserExam
        {
            UserId = student.Id, ExamId = exam.Id, StartedAt = DateTime.UtcNow.AddMinutes(-5),
            SubmittedAt = DateTime.UtcNow, Status = "submitted"
        };
        db.AddRange(question, attempt);
        await db.SaveChangesAsync();

        db.AddRange(
            new ExamQuestion { ExamId = exam.Id, QuestionId = question.Id, OrderIndex = 1, Points = 10 },
            new UserExamAnswer
            {
                UserExamId = attempt.Id, QuestionId = question.Id, GradingStatus = "pending_review",
                TextAnswer = "My name is Alex. I am learning English. I enjoy reading."
            });
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return true;
    }
}
