using Microsoft.EntityFrameworkCore.Storage;
using CNPM_EnglishLearning.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CNPM_EnglishLearning.Data;

internal static class ApplicationDataSeeder
{
    public const string Email = "teacher@englishlearning.example";

    public static async Task<bool> SeedAsync(ApplicationDbContext db, string password)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();
        // Serialize demo imports so two runs cannot create the same fixture.
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(4031779)");
        if (await db.CourseClasses.AnyAsync(item => item.ClassCode == "EL-GT-2026-01"))
            return false;

        var teacherUser = new User { Email = Email, FullName = "Lê Minh Anh", Role = "teacher" };
        teacherUser.Password = new PasswordHasher<User>().HashPassword(teacherUser, password);
        var student = new User { Email = "student@englishlearning.example", FullName = "Trần Hoàng Nam" };
        student.Password = new PasswordHasher<User>().HashPassword(student, password);
        var admin = new User { Email = "admin@englishlearning.example", FullName = "Nguyễn Quản Trị", Role = "admin" };
        admin.Password = new PasswordHasher<User>().HashPassword(admin, password);
        db.Users.Add(admin);
        var subject = new Subject { Name = "Tiếng Anh giao tiếp" };
        var room = new Classroom { Name = "Phòng A201", Capacity = 20 };
        var slot = new TimeSlot { Name = "Ca sáng", StartTime = TimeSpan.FromHours(9), EndTime = TimeSpan.FromHours(10.5) };
        db.AddRange(teacherUser, student, subject, room, slot);
        await db.SaveChangesAsync();

        var teacher = new Teacher { UserId = teacherUser.Id, Bio = "Giảng dạy tiếng Anh giao tiếp và kỹ năng nói.", IsVerified = true };
        var course = new Course { SubjectId = subject.Id, Title = "Giao tiếp cơ bản", IsPublished = true };
        db.AddRange(teacher, course);
        await db.SaveChangesAsync();

        var today = DateTime.SpecifyKind(DateTime.UtcNow.AddHours(7).Date, DateTimeKind.Utc);
        var courseClass = new CourseClass
        {
            CourseId = course.Id, ClassCode = "EL-GT-2026-01", ClassName = "Lớp giao tiếp",
            StartDate = today.AddDays(-7), EndDate = today.AddDays(30), Status = "ongoing"
        };
        var lesson = new Lesson
        {
            CourseId = course.Id, Title = "Giới thiệu bản thân", OrderIndex = 1,
            ContentType = "speaking", Content = "Practice introducing yourself in English."
        };
        var exam = new Exam
        {
            CourseId = course.Id, CreatedByTeacherId = teacher.Id, Title = "Kiểm tra đầu khóa",
            IsPublished = true, DurationMinutes = 20
        };
        db.AddRange(courseClass, lesson, exam);
        await db.SaveChangesAsync();

        var schedule = new ClassSchedule
        {
            ClassId = courseClass.Id, ClassroomId = room.Id, TimeSlotId = slot.Id,
            DayOfWeek = (int)today.DayOfWeek + 1
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
        var choice = new Question
        {
            LessonId = lesson.Id, QuestionText = "Choose the correct sentence.",
            QuestionType = "multiple_choice", DefaultPoints = 1
        };
        var blank = new Question
        {
            LessonId = lesson.Id, QuestionText = "I ___ a student.",
            QuestionType = "fill_blank", DefaultPoints = 1
        };
        db.AddRange(choice, blank);
        await db.SaveChangesAsync();
        var correct = new QuestionOption { QuestionId = choice.Id, OptionText = "I am a student.", IsCorrect = true };
        db.AddRange(correct,
            new QuestionOption { QuestionId = choice.Id, OptionText = "I is a student.", IsCorrect = false },
            new QuestionAnswer { QuestionId = blank.Id, AnswerText = "am", IsCaseSensitive = false },
            new ExamQuestion { ExamId = exam.Id, QuestionId = choice.Id, Points = 1, OrderIndex = 2 },
            new ExamQuestion { ExamId = exam.Id, QuestionId = blank.Id, Points = 1, OrderIndex = 3 },
            new Flashcard { UserId = student.Id, Word = "Introduce", Meaning = "Giới thiệu", ExampleSentence = "Let me introduce myself.", NextReviewAt = DateTime.UtcNow.AddDays(1) },
            new UserProgress { UserId = student.Id, LessonId = lesson.Id, IsCompleted = true, CompletedAt = DateTime.UtcNow },
            new CourseReview { UserId = student.Id, CourseId = course.Id, Rating = 5, Comment = "Bài học dễ hiểu, có nhiều hoạt động thực hành." });
        await db.SaveChangesAsync();
        db.AddRange(
            new UserExamAnswer { UserExamId = attempt.Id, QuestionId = choice.Id, SelectedOptionId = correct.Id, ScoreEarned = 1, GradingStatus = "auto_graded" },
            new UserExamAnswer { UserExamId = attempt.Id, QuestionId = blank.Id, TextAnswer = "am", ScoreEarned = 1, GradingStatus = "auto_graded" });
        await db.SaveChangesAsync();

        // Verify all mapped business tables inside the transaction before committing.
        foreach (var entity in db.Model.GetEntityTypes())
        {
            var table = entity.GetTableName()!;
            if (!System.Text.RegularExpressions.Regex.IsMatch(table, "^[a-z_]+$"))
                throw new InvalidOperationException("Unexpected table identifier.");
            var connection = db.Database.GetDbConnection();
            await using var command = connection.CreateCommand();
            command.Transaction = transaction.GetDbTransaction();
            command.CommandText = $"SELECT COUNT(*) FROM public.\"{table}\"";
            var count = Convert.ToInt64(await command.ExecuteScalarAsync());
            if (count == 0) throw new InvalidOperationException($"Empty table: {table}");
            Console.WriteLine($"{table}: {count}");
        }
        await transaction.CommitAsync();
        return true;
    }
}
