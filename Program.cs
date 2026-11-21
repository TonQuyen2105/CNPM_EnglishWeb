using CNPM_EnglishLearning.Services.Interfaces;
using CNPM_EnglishLearning.Services.Implementations;
using CNPM_EnglishLearning.Data;
using CNPM_EnglishLearning.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

// Kích hoạt LegacyTimestampBehavior cho Npgsql để tương thích linh hoạt với cả UTC và Unspecified DateTime
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add MVC services to the container
builder.Services.AddControllersWithViews();

// Cấu hình CORS mở cho mọi nguồn (hỗ trợ Live Server port 5500, file://, hoặc localhost:5000)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Register HtmlViewEngine to render static .html views seamlessly alongside .cshtml
builder.Services.Configure<MvcViewOptions>(options =>
{
    options.ViewEngines.Insert(0, new HtmlViewEngine());
});

// Register application services (Dependency Injection)
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<IExamService, ExamService>();

var app = builder.Build();

app.UseCors("AllowAll");

// Đảm bảo schema PostgreSQL có cột phone_number trong bảng users và seed dữ liệu bài thi tự luận
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.ExecuteSqlRaw("ALTER TABLE users ADD COLUMN IF NOT EXISTS phone_number VARCHAR(50);");
        db.Database.ExecuteSqlRaw("DELETE FROM payments WHERE id = 2;");
        db.Database.ExecuteSqlRaw("DELETE FROM enrollments WHERE id = 43 OR (user_id = 8 AND class_id = 6);");
        db.Database.ExecuteSqlRaw("UPDATE course_classes SET status = 'ongoing' WHERE id = 8;");
        db.Database.ExecuteSqlRaw("UPDATE course_classes SET status = 'upcoming' WHERE id = 9;");

        foreach (var c in db.CourseClasses.AsNoTracking().ToList())
        {
            Console.WriteLine($"[CLASS_IN_DB] ID: {c.Id}, CourseId: {c.CourseId}, Code: {c.ClassCode}, Name: {c.ClassName}, Status: '{c.Status}', Price: {c.Price}");
        }

        var schedules89 = db.ClassSchedules.AsNoTracking().Where(cs => cs.ClassId == 8 || cs.ClassId == 9).ToList();
        Console.WriteLine($"[SCHEDULE_IN_DB] Schedules for class 8 & 9: {schedules89.Count}");
        if (schedules89.Count == 0)
        {
            var defaultTimeSlot = db.TimeSlots.FirstOrDefault();
            var defaultClassroom = db.Classrooms.FirstOrDefault();
            if (defaultTimeSlot != null && defaultClassroom != null)
            {
                var nextSchedId = (db.ClassSchedules.Any() ? db.ClassSchedules.Max(s => s.Id) : 0) + 1;
                db.ClassSchedules.Add(new ClassSchedule
                {
                    Id = nextSchedId,
                    ClassId = 8,
                    DayOfWeek = 3, // Thứ 3
                    TimeSlotId = defaultTimeSlot.Id,
                    ClassroomId = defaultClassroom.Id
                });
                db.ClassSchedules.Add(new ClassSchedule
                {
                    Id = nextSchedId + 1,
                    ClassId = 8,
                    DayOfWeek = 5, // Thứ 5
                    TimeSlotId = defaultTimeSlot.Id,
                    ClassroomId = defaultClassroom.Id
                });
                db.ClassSchedules.Add(new ClassSchedule
                {
                    Id = nextSchedId + 2,
                    ClassId = 9,
                    DayOfWeek = 2, // Thứ 2
                    TimeSlotId = defaultTimeSlot.Id,
                    ClassroomId = defaultClassroom.Id
                });
                db.ClassSchedules.Add(new ClassSchedule
                {
                    Id = nextSchedId + 3,
                    ClassId = 9,
                    DayOfWeek = 4, // Thứ 4
                    TimeSlotId = defaultTimeSlot.Id,
                    ClassroomId = defaultClassroom.Id
                });
                db.SaveChanges();
                Console.WriteLine("[SCHEDULE_IN_DB] Seeded schedules for class 8 & 9 successfully.");
            }
        }

        // Seed Đề thi tự luận (All-Essay Exam) cho học sinh ID 8
        const string essayExamTitle = "Bài thi Tự luận Tiếng Anh Chuyên sâu (Academic Essay Writing Test)";
        var existingExam = db.Exams.FirstOrDefault(e => e.Title == essayExamTitle);
        if (existingExam == null)
        {
            var validTeacherId = db.Teachers.Select(t => t.Id).FirstOrDefault();
            if (validTeacherId == 0)
            {
                validTeacherId = db.Exams.Select(e => e.CreatedByTeacherId).FirstOrDefault();
            }

            var nextExamId = (db.Exams.Any() ? db.Exams.Max(e => e.Id) : 0) + 1;
            var newExam = new Exam
            {
                Id = nextExamId,
                CourseId = 4, // Gắn với khóa học user 8 tham gia: "Giao tiếp trong cuộc sống"
                CreatedByTeacherId = validTeacherId,
                Title = essayExamTitle,
                Description = "Bài thi tự luận chuyên sâu đánh giá năng lực tư duy phản biện, lập luận học thuật và khả năng sử dụng tiếng Anh thương mại & học thuật. Gồm 3 câu hỏi tự luận (Essay questions) chấm điểm bởi giảng viên.",
                DurationMinutes = 60,
                PassScore = 5.0m,
                MaxAttempts = 3,
                IsPublished = true,
                CreatedAt = DateTime.UtcNow
            };
            db.Exams.Add(newExam);
            db.SaveChanges();

            var nextQId = (db.Questions.Any() ? db.Questions.Max(q => q.Id) : 0) + 1;
            var q1 = new Question
            {
                Id = nextQId,
                QuestionText = "Topic 1: AI & The Future of Language Education\n'Some people believe that artificial intelligence and AI language tutors will soon replace human English teachers in classrooms, while others insist that human teachers remain indispensable.'\nDiscuss both views and give your own personal opinion with clear arguments and relevant examples (150 - 180 words).",
                QuestionType = "essay",
                DefaultPoints = 2.5m,
                CreatedAt = DateTime.UtcNow
            };
            var q2 = new Question
            {
                Id = nextQId + 1,
                QuestionText = "Topic 2: Daily Self-Study Habits for Working Professionals\n'Many busy professionals fail to maintain consistent English self-study routines due to intense work schedules and digital distractions.'\nExplain two core reasons why this problem occurs, and outline two actionable, realistic strategies they can implement to build long-term language learning habits (150 - 200 words).",
                QuestionType = "essay",
                DefaultPoints = 2.5m,
                CreatedAt = DateTime.UtcNow
            };
            var q3 = new Question
            {
                Id = nextQId + 2,
                QuestionText = "Topic 3: Formal Academic Statement of Purpose & Career Ambitions\nWrite a formal statement of purpose / letter of application (200 - 250 words) addressed to the Admissions Committee of an international postgraduate fellowship program.\nYour essay must:\n1. Briefly introduce your professional or academic background.\n2. Explain why advanced English fluency is critical to your next career milestones.\n3. Describe how you intend to apply your learned skills to contribute positively to your field or community upon graduation.",
                QuestionType = "essay",
                DefaultPoints = 5.0m,
                CreatedAt = DateTime.UtcNow
            };
            db.Questions.AddRange(q1, q2, q3);
            db.SaveChanges();

            var nextEqId = (db.ExamQuestions.Any() ? db.ExamQuestions.Max(eq => eq.Id) : 0) + 1;
            var eq1 = new ExamQuestion { Id = nextEqId, ExamId = newExam.Id, QuestionId = q1.Id, Points = 2.5m, OrderIndex = 1 };
            var eq2 = new ExamQuestion { Id = nextEqId + 1, ExamId = newExam.Id, QuestionId = q2.Id, Points = 2.5m, OrderIndex = 2 };
            var eq3 = new ExamQuestion { Id = nextEqId + 2, ExamId = newExam.Id, QuestionId = q3.Id, Points = 5.0m, OrderIndex = 3 };
            db.ExamQuestions.AddRange(eq1, eq2, eq3);
            db.SaveChanges();

            Console.WriteLine($"[Database Seeder] Successfully inserted All-Essay Exam ID {newExam.Id} (TeacherId: {validTeacherId}) with 3 essay questions.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Database Migration Note] {ex.Message}");
    }
}

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Serve static files from wwwroot at root (/css/...) and with /wwwroot prefix (/wwwroot/css/...)
app.UseStaticFiles();
app.UseStaticFiles(new StaticFileOptions
{
    RequestPath = "/wwwroot"
});

// Cho phép truy cập trực tiếp các view tĩnh trong Areas/ nếu được gọi trực tiếp qua URL
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(builder.Environment.ContentRootPath, "Areas")),
    RequestPath = "/Areas"
});

// Phục vụ các file tĩnh ở thư mục gốc (index.html, styleguide.html)
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(builder.Environment.ContentRootPath),
    RequestPath = ""
});

// Middleware tự động bắt và phân giải chính xác mọi đường dẫn HTML trên cả 3 cổng
// (chống 100% lỗi trang trắng 404 khi học viên / giảng viên / admin chuyển trang)
app.UseMiddleware<HtmlRouteMiddleware>();

app.UseRouting();

app.UseAuthorization();

// Area routing for Admin, Teacher, and Student
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

// Default fallback route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Endpoint kiểm tra nhanh trạng thái dữ liệu mẫu trong ApplicationDbContext
app.MapGet("/api/db-status", (ApplicationDbContext db) =>
{
    return Results.Ok(new
    {
        status = "Success",
        message = "ApplicationDbContext đã được nạp dữ liệu mẫu từ DbInitializer!",
        users = db.Users.Count(),
        teachers = db.Teachers.Count(),
        subjects = db.Subjects.Count(),
        courses = db.Courses.Count(),
        classes = db.CourseClasses.Count(),
        enrollments = db.Enrollments.Count(),
        flashcards = db.Flashcards.Count(),
        estimatedRevenue = db.CalculateEstimatedRevenue()
    });
});

app.Run();
