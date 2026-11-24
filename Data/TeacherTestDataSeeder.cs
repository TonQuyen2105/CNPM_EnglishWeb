using System.Security.Cryptography;
using CNPM_EnglishLearning.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace CNPM_EnglishLearning.Data;

// Synthetic fixtures persisted in PostgreSQL; no client-side mock data or credential changes.
internal static class TeacherTestDataSeeder
{
    private const string Prefix = "EL-PRACTICE-V1-";

    public static async Task RunAsync(ApplicationDbContext db, bool inspectOnly)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(4031780)");
        var connection = db.Database.GetDbConnection();
        foreach (var entity in db.Model.GetEntityTypes().OrderBy(e => e.GetTableName()))
        {
            var table = entity.GetTableName()!;
            if (!System.Text.RegularExpressions.Regex.IsMatch(table, "^[a-z_]+$"))
                throw new InvalidOperationException("Tên bảng không hợp lệ.");
            await using var command = connection.CreateCommand();
            command.Transaction = transaction.GetDbTransaction();
            command.CommandText = $"SELECT COUNT(*) FROM public.\"{table}\"";
            Console.WriteLine($"{table}: {await command.ExecuteScalarAsync()}");
        }
        if (inspectOnly)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction.GetDbTransaction();
            command.CommandText = "SELECT conrelid::regclass::text, pg_get_constraintdef(oid) FROM pg_constraint WHERE contype = 'c' AND connamespace = 'public'::regnamespace";
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync()) Console.WriteLine($"{reader.GetString(0)}: {reader.GetString(1)}");
            return;
        }
        var email = Environment.GetEnvironmentVariable("TEACHER_TEST_EMAIL") ?? ApplicationDataSeeder.Email;
        var teacher = await (from t in db.Teachers join u in db.Users on t.UserId equals u.Id
                             where u.Email == email && u.Role == "teacher" && u.IsActive select t).SingleOrDefaultAsync()
            ?? throw new InvalidOperationException("Không tìm thấy giảng viên. Đặt TEACHER_TEST_EMAIL bằng email giảng viên hiện có.");
        var existing = await db.CourseClasses.CountAsync(c => c.ClassCode.StartsWith(Prefix));
        if (existing > 0)
        {
            if (existing != 6) throw new InvalidOperationException("Bộ dữ liệu chỉ còn một phần; cần kiểm tra trước khi nhập lại.");
            Console.WriteLine("Đã có đủ 6 lớp của bộ dữ liệu này. Không chèn trùng.");
            return;
        }
        var now = DateTime.UtcNow;
        var today = DateTime.SpecifyKind(now.AddHours(7).Date, DateTimeKind.Utc);
        string[] names = ["Nguyễn Minh Khang", "Trần Ngọc Anh", "Lê Bảo Ngọc", "Phạm Gia Huy", "Hoàng Thu Hà", "Vũ Đức Minh",
            "Đặng Khánh Linh", "Bùi Tuấn Kiệt", "Đỗ Phương Thảo", "Ngô Hải Đăng", "Hồ Quỳnh Như", "Dương Nhật Nam",
            "Lý Mai Chi", "Phan Anh Tuấn", "Võ Thanh Trúc", "Đinh Quang Huy", "Mai Hoài An", "Cao Minh Châu",
            "Tạ Bảo Long", "Trịnh Ngọc Hân", "Nguyễn Đức Anh", "Trần Thùy Dương", "Lê Quốc Bảo", "Phạm Yến Nhi"];
        var students = new List<User>();
        for (var i = 0; i < names.Length; i++)
        {
            var studentEmail = $"learner.practice.v1.{i + 1:00}@englishlearning.example";
            if (await db.Users.AnyAsync(u => u.Email == studentEmail))
                throw new InvalidOperationException("Email kiểm thử đã tồn tại ngoài bộ dữ liệu. Dừng để tránh sửa nhầm tài khoản.");
            var student = new User { FullName = names[i], Email = studentEmail, Role = "student" };
            student.Password = new PasswordHasher<User>().HashPassword(student, Convert.ToHexString(RandomNumberGenerator.GetBytes(32)));
            students.Add(student);
        }
        db.Users.AddRange(students);
        var room = new Classroom { Name = "Phòng B301", Capacity = 30 };
        db.Classrooms.Add(room);
        var slots = Enumerable.Range(0, 6).Select(i => new TimeSlot
        {
            Name = $"Khung giờ {8 + i * 2}:00", StartTime = TimeSpan.FromHours(8 + i * 2), EndTime = TimeSpan.FromHours(9.5 + i * 2)
        }).ToArray();
        db.TimeSlots.AddRange(slots);
        await db.SaveChangesAsync();
        string[] titles = ["Giao tiếp trong cuộc sống", "Ngữ pháp và viết câu", "Tiếng Anh công sở"];
        string[] lessonTitles = ["Giới thiệu và làm quen", "Thói quen mỗi ngày", "Hỏi đường và địa điểm", "Lên kế hoạch cuối tuần", "Viết thư trao đổi", "Ôn tập và thực hành"];
        string[] types = ["speaking", "grammar", "vocabulary", "reading", "writing", "quiz"];
        for (var c = 0; c < 3; c++)
        {
            var subject = new Subject { Name = titles[c] };
            db.Subjects.Add(subject);
            await db.SaveChangesAsync();
            var course = new Course { SubjectId = subject.Id, Title = titles[c], Description = "Học qua tình huống thực hành, thảo luận và bài tập theo từng buổi.", Level = c == 0 ? "beginner" : "intermediate", Price = 1800000 + c * 300000, IsPublished = true };
            db.Courses.Add(course);
            await db.SaveChangesAsync();
            var lessons = Enumerable.Range(0, 6).Select(i => new Lesson
            {
                CourseId = course.Id, OrderIndex = i + 1, Title = lessonTitles[i], ContentType = types[i],
                Content = $"Chủ đề: {titles[c]} — {lessonTitles[i]}.\nMục tiêu: vận dụng từ vựng và cấu trúc câu trong ngữ cảnh.\nKhởi động (10 phút): trao đổi theo cặp.\nThực hành (25 phút): đọc ví dụ, viết câu và trình bày.\nBài tập: viết một đoạn 80–100 từ, tự kiểm tra ngữ pháp trước khi nộp."
            }).ToArray();
            db.Lessons.AddRange(lessons);
            await db.SaveChangesAsync();
            for (var k = 0; k < 2; k++)
            {
                var index = c * 2 + k;
                var status = index < 3 ? "ongoing" : index == 3 ? "upcoming" : index == 4 ? "completed" : "cancelled";
                var start = today.AddDays(index == 3 ? 7 : index == 4 ? -70 : -14);
                var courseClass = new CourseClass { CourseId = course.Id, ClassCode = Prefix + (index + 1).ToString("00"), ClassName = titles[c] + (k == 0 ? " · Nhóm 1" : " · Nhóm 2"), StartDate = start, EndDate = index == 4 ? today.AddDays(-7) : start.AddDays(56), Status = status, Price = course.Price, MaxStudents = 24 };
                db.CourseClasses.Add(courseClass);
                await db.SaveChangesAsync();
                db.ClassTeachers.Add(new ClassTeacher { ClassId = courseClass.Id, TeacherId = teacher.Id });
                foreach (var student in students.Skip(c * 8).Take(k == 0 ? 8 : 5))
                    db.Enrollments.Add(new Enrollment { ClassId = courseClass.Id, UserId = student.Id, EnrolledAt = start.AddDays(-2), Status = status == "completed" ? "completed" : status == "cancelled" ? "cancelled" : "active" });
                for (var d = 0; d < 2; d++)
                {
                    var targetDow = today.AddDays(d * 3).DayOfWeek;
                    var day = targetDow == DayOfWeek.Sunday ? 7 : (int)targetDow;
                    // Avoid overlapping the teacher's existing assignments.
                    var occupied = await (from sc in db.ClassSchedules
                        join ct in db.ClassTeachers on sc.ClassId equals ct.ClassId
                        join cl in db.CourseClasses on sc.ClassId equals cl.Id
                        join ts in db.TimeSlots on sc.TimeSlotId equals ts.Id
                        where ct.TeacherId == teacher.Id && cl.Status != "cancelled"
                            && cl.StartDate <= courseClass.EndDate && cl.EndDate >= courseClass.StartDate
                        select new { sc.DayOfWeek, ts.StartTime, ts.EndTime }).ToListAsync();
                    var availableDay = Enumerable.Range(0, 7).Select(offset => (day - 1 + offset) % 7 + 1)
                        .FirstOrDefault(candidate => !occupied.Any(o => o.DayOfWeek == candidate
                            && o.StartTime < slots[index].EndTime && o.EndTime > slots[index].StartTime));
                    if (availableDay == 0) throw new InvalidOperationException("Không còn khung giờ trống để bổ sung lịch giảng dạy.");
                    day = availableDay;
                    var schedule = new ClassSchedule { ClassId = courseClass.Id, ClassroomId = room.Id, TimeSlotId = slots[index].Id, DayOfWeek = day };
                    db.ClassSchedules.Add(schedule);
                    await db.SaveChangesAsync();
                    var startIso = start.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)start.DayOfWeek;
                    var lessonDate = start.AddDays((day - startIso + 7) % 7);
                    for (var n = 0; n < 3; n++)
                        db.ClassLessons.Add(new ClassLesson { ClassId = courseClass.Id, LessonId = lessons[d + n * 2].Id, TeacherId = teacher.Id, ScheduleId = schedule.Id, ScheduleDate = lessonDate.AddDays(7 * n), Status = status == "cancelled" ? "cancelled" : lessonDate.AddDays(7 * n) < today ? "completed" : "scheduled" });
                }
            }
            var questions = new List<Question>();
            for (var i = 0; i < 6; i++)
            {
                questions.Add(new Question { LessonId = lessons[i].Id, QuestionType = "essay", DefaultPoints = 3,
                    QuestionText = $"Write 80–100 words about {new[] { "yourself and your interests", "your daily routine", "your neighbourhood", "your weekend plans", "a message to a colleague", "what you learned this month" }[i]}." });
                questions.Add(new Question { LessonId = lessons[i].Id, QuestionType = "multiple_choice", DefaultPoints = 1, QuestionText = $"Bài {i + 1}: Choose the correct present simple sentence." });
                questions.Add(new Question { LessonId = lessons[i].Id, QuestionType = "fill_blank", DefaultPoints = 1, QuestionText = $"Bài {i + 1}: She ___ English every evening. (study)" });
            }
            db.Questions.AddRange(questions);
            await db.SaveChangesAsync();
            var correctOptions = new Dictionary<int, QuestionOption>();
            foreach (var question in questions)
            {
                if (question.QuestionType == "multiple_choice")
                {
                    var option = new QuestionOption { QuestionId = question.Id, OptionText = "She studies English every day.", IsCorrect = true };
                    correctOptions[question.Id] = option;
                    db.QuestionOptions.Add(option);
                    foreach (var text in new[] { "She study English every day.", "She studying English every day.", "She are study English every day." })
                        db.QuestionOptions.Add(new QuestionOption { QuestionId = question.Id, OptionText = text });
                }
                if (question.QuestionType == "fill_blank") db.QuestionAnswers.Add(new QuestionAnswer { QuestionId = question.Id, AnswerText = "studies" });
            }
            await db.SaveChangesAsync();
            for (var e = 0; e < 2; e++)
            {
                var exam = new Exam { CourseId = course.Id, CreatedByTeacherId = teacher.Id, Title = titles[c] + (e == 0 ? " — Kiểm tra giữa khóa" : " — Kiểm tra cuối khóa"), DurationMinutes = 45, PassScore = 5, MaxAttempts = 2, IsPublished = e == 0 };
                db.Exams.Add(exam);
                await db.SaveChangesAsync();
                var examQuestions = questions.Skip(e * 9).Take(6).ToArray();
                for (var q = 0; q < examQuestions.Length; q++) db.ExamQuestions.Add(new ExamQuestion { ExamId = exam.Id, QuestionId = examQuestions[q].Id, OrderIndex = q + 1, Points = examQuestions[q].DefaultPoints });
                if (e != 0) continue; // Draft exams have no submissions.
                for (var s = 0; s < 8; s++)
                {
                    var pending = s < 3;
                    var passed = s >= 5;
                    var submittedAt = now.AddDays(c == 2 ? -10 : -2).AddMinutes(s);
                    var attempt = new UserExam { UserId = students[c * 8 + s].Id, ExamId = exam.Id, StartedAt = submittedAt.AddMinutes(-45), SubmittedAt = submittedAt, Status = pending ? "submitted" : passed ? "passed" : "failed", TotalScore = pending ? null : passed ? 8 : 4 };
                    db.UserExams.Add(attempt);
                    await db.SaveChangesAsync();
                    foreach (var q in examQuestions)
                    {
                        var essay = q.QuestionType == "essay";
                        db.UserExamAnswers.Add(new UserExamAnswer
                        {
                            UserExamId = attempt.Id, QuestionId = q.Id,
                            SelectedOptionId = q.QuestionType == "multiple_choice" ? correctOptions[q.Id].Id : null,
                            TextAnswer = essay ? "I study English every evening. I practise with my friends and write short paragraphs. My goal is to communicate confidently at work. On weekends, I review new words and read stories." : q.QuestionType == "fill_blank" ? "studies" : null,
                            GradingStatus = essay ? pending ? "pending_review" : "graded" : "auto_graded",
                            ScoreEarned = essay ? pending ? null : passed ? 2 : 0 : 1,
                            GradedByTeacherId = essay && !pending ? teacher.Id : null,
                            GradedAt = essay && !pending ? submittedAt.AddDays(1) : null,
                            Feedback = essay && !pending ? passed ? "Ý rõ ràng; cần bổ sung ví dụ và viết đủ độ dài." : "Cần phát triển ý và kiểm tra cấu trúc câu." : null
                        });
                    }
                }
            }
            foreach (var student in students.Skip(c * 8).Take(8))
            {
                for (var l = 0; l < 3; l++) db.UserProgresses.Add(new UserProgress { UserId = student.Id, LessonId = lessons[l].Id, IsCompleted = l < 2, CompletedAt = l < 2 ? now.AddDays(-3 + l) : null, Score = l < 2 ? 8 : null });
                db.CourseReviews.Add(new CourseReview { UserId = student.Id, CourseId = course.Id, Rating = 4, Comment = "Nội dung dễ theo dõi, hoạt động thực hành hữu ích." });
                db.Flashcards.Add(new Flashcard { UserId = student.Id, Word = "confident", Meaning = "Tự tin", ExampleSentence = "I feel confident when I speak English.", NextReviewAt = now.AddDays(1) });
            }
            await db.SaveChangesAsync();
        }
        if (await db.CourseClasses.CountAsync(c => c.ClassCode.StartsWith(Prefix)) != 6)
            throw new InvalidOperationException("Kiểm tra số lớp không đạt.");
        await transaction.CommitAsync();
        Console.WriteLine("Đã lưu: 24 học viên, 3 khóa học, 6 lớp, 39 ghi danh, 12 lịch dạy, 36 buổi học, 18 bài giảng, 54 câu hỏi, 6 đề thi, 24 lượt nộp, 144 câu trả lời (18 chờ chấm), 72 tiến độ, 24 đánh giá, 24 thẻ từ.");
    }
}
