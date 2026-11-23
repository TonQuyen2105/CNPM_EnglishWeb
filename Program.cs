using CNPM_EnglishLearning.Services.Auth;
using CNPM_EnglishLearning.Services.Interfaces;
using CNPM_EnglishLearning.Services.Implementations;
using CNPM_EnglishLearning.Data;
using CNPM_EnglishLearning.Models.Entities;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

// Kích hoạt LegacyTimestampBehavior cho Npgsql để tương thích linh hoạt với cả UTC và Unspecified DateTime
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var migrateGradeHistory = args.Contains("--migrate-grade-history", StringComparer.Ordinal);
var migrateExamReview = args.Contains("--migrate-exam-review", StringComparer.Ordinal);
var migrateLessonClasses = args.Contains("--migrate-lesson-classes", StringComparer.Ordinal);
var seedTeacherDemo = args.Contains("--seed-teacher-demo", StringComparer.Ordinal);
var seedTeacherTest = args.Contains("--seed-teacher-test", StringComparer.Ordinal);
var inspectDatabase = args.Contains("--inspect-database", StringComparer.Ordinal);
var seedAll = args.Contains("--seed-all", StringComparer.Ordinal);
var resetTeacherPassword = args.Contains("--reset-teacher-password", StringComparer.Ordinal);
var builder = WebApplication.CreateBuilder(args.Where(arg => arg != "--seed-teacher-demo"
    && arg != "--seed-teacher-test" && arg != "--inspect-database"
    && arg != "--seed-all" && arg != "--reset-teacher-password"
    && arg != "--migrate-lesson-classes" && arg != "--migrate-exam-review"
    && arg != "--migrate-grade-history").ToArray());

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add MVC services to the container
builder.Services.AddControllersWithViews();
builder.Services.AddMemoryCache();

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

builder.Services.AddScoped<CNPM_EnglishLearning.Services.Lessons.LessonFileStore>();
builder.Services.AddScoped<CNPM_EnglishLearning.Services.Lessons.SupabaseLessonStorage>();
builder.Services.AddHttpClient("LessonStorage", client => client.Timeout = TimeSpan.FromMinutes(10))
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
static bool IsApiRequest(HttpRequest request) =>
    request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase)
    || request.Path.StartsWithSegments("/Admin/api", StringComparison.OrdinalIgnoreCase)
    || request.Headers.Accept.Any(a => a is not null && a.Contains("application/json", StringComparison.OrdinalIgnoreCase));

static void ConfigurePortalCookie(CookieAuthenticationOptions options, string cookieName)
{
    options.Cookie.Name = cookieName;
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.LoginPath = "/login.html";
    options.AccessDeniedPath = "/login.html";
    options.Events.OnRedirectToLogin = context =>
    {
        if (IsApiRequest(context.Request))
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        else
            context.Response.Redirect(context.RedirectUri);

        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        if (IsApiRequest(context.Request))
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
        else
            context.Response.Redirect(context.RedirectUri);

        return Task.CompletedTask;
    };
}

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = PortalAuthSchemes.PortalSelector;
        options.DefaultAuthenticateScheme = PortalAuthSchemes.PortalSelector;
        options.DefaultChallengeScheme = PortalAuthSchemes.PortalSelector;
        options.DefaultForbidScheme = PortalAuthSchemes.PortalSelector;
    })
    .AddPolicyScheme(PortalAuthSchemes.PortalSelector, "Portal Cookie Selector", options =>
    {
        options.ForwardDefaultSelector = context =>
        {
            var path = context.Request.Path;

            if (path.StartsWithSegments("/Admin", StringComparison.OrdinalIgnoreCase)
                || path.StartsWithSegments("/Areas/Admin", StringComparison.OrdinalIgnoreCase)
                || path.StartsWithSegments("/api/admin", StringComparison.OrdinalIgnoreCase))
                return PortalAuthSchemes.AdminCookie;

            if (path.StartsWithSegments("/Teacher", StringComparison.OrdinalIgnoreCase)
                || path.StartsWithSegments("/Areas/Teacher", StringComparison.OrdinalIgnoreCase)
                || path.StartsWithSegments("/api/teacher", StringComparison.OrdinalIgnoreCase))
                return PortalAuthSchemes.TeacherCookie;

            if (path.StartsWithSegments("/Student", StringComparison.OrdinalIgnoreCase)
                || path.StartsWithSegments("/Areas/Student", StringComparison.OrdinalIgnoreCase)
                || path.StartsWithSegments("/api/v1/student", StringComparison.OrdinalIgnoreCase)
                || path.StartsWithSegments("/api/student", StringComparison.OrdinalIgnoreCase))
                return PortalAuthSchemes.StudentCookie;

            if (path.StartsWithSegments("/api/lessons", StringComparison.OrdinalIgnoreCase))
            {
                var referer = context.Request.Headers.Referer.ToString();
                if (referer.Contains("/Teacher", StringComparison.OrdinalIgnoreCase))
                    return PortalAuthSchemes.TeacherCookie;
                if (referer.Contains("/Student", StringComparison.OrdinalIgnoreCase))
                    return PortalAuthSchemes.StudentCookie;
                if (context.Request.Cookies.ContainsKey(PortalAuthSchemes.StudentCookieName))
                    return PortalAuthSchemes.StudentCookie;
                return PortalAuthSchemes.TeacherCookie;
            }

            return PortalAuthSchemes.TeacherCookie;
        };
    })
    .AddCookie(PortalAuthSchemes.AdminCookie, options => ConfigurePortalCookie(options, PortalAuthSchemes.AdminCookieName))
    .AddCookie(PortalAuthSchemes.TeacherCookie, options => ConfigurePortalCookie(options, PortalAuthSchemes.TeacherCookieName))
    .AddCookie(PortalAuthSchemes.StudentCookie, options => ConfigurePortalCookie(options, PortalAuthSchemes.StudentCookieName));

// Register HtmlViewEngine to render static .html views seamlessly alongside .cshtml
builder.Services.Configure<MvcViewOptions>(options =>
{
    options.ViewEngines.Insert(0, new HtmlViewEngine());
});

// Register application services (Dependency Injection)
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<IExamService, ExamService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAutomaticGradingService, AutomaticGradingService>();
builder.Services.AddHostedService<AutomaticGradingWorker>();

var app = builder.Build();

if (migrateLessonClasses || migrateExamReview || migrateGradeHistory)
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        var filename = migrateGradeHistory ? "20261122_grade_changes.sql" : migrateExamReview ? "20261122_exam_review.sql" : "20261122_lesson_class_scope.sql";
        var sql = await File.ReadAllTextAsync(Path.Combine(app.Environment.ContentRootPath, "Database", filename));
        await db.Database.ExecuteSqlRawAsync(sql);
        Console.WriteLine(migrateGradeHistory ? "Đã tạo lịch sử chấm và sửa điểm." : migrateExamReview ? "Đã cập nhật trạng thái duyệt đề thi." : "Đã chuyển liên kết bài giảng và lớp sang class_lessons.");
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine("Không cập nhật được schema: " + ex.GetType().Name + ". Kiểm tra kết nối hoặc chạy file SQL trong Supabase SQL Editor.");
        Environment.ExitCode = 1;
    }
    return;
}

if (seedTeacherTest || inspectDatabase)
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.SetCommandTimeout(20);
    try
    {
        await TeacherTestDataSeeder.RunAsync(db, inspectDatabase);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine("Không hoàn tất nhập dữ liệu; các thay đổi trong giao dịch đã được hủy.");
        Console.Error.WriteLine(ex.GetBaseException() is Npgsql.PostgresException pg
            ? $"PostgreSQL {pg.SqlState}: {pg.MessageText}"
            : ex is InvalidOperationException ? ex.Message : ex.GetBaseException().GetType().Name);
        Environment.ExitCode = 1;
    }
    return;
}

if (resetTeacherPassword)
{
    await using var scope = app.Services.CreateAsyncScope();
    try
    {
        await TeacherPasswordReset.RunAsync(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>());
    }
    catch (InvalidOperationException ex)
    {
        Console.Error.WriteLine(ex.Message);
        Environment.ExitCode = 1;
    }
    catch (Exception)
    {
        Console.Error.WriteLine("Không đặt lại được mật khẩu. Kiểm tra kết nối database rồi thử lại.");
        Environment.ExitCode = 1;
    }
    return;
}

if (seedTeacherDemo || seedAll)
{
    var password = Environment.GetEnvironmentVariable("TEACHER_DEMO_PASSWORD");
    if (string.IsNullOrWhiteSpace(password) || password.Length < 12)
    {
        Console.Error.WriteLine("Đặt biến TEACHER_DEMO_PASSWORD với mật khẩu ít nhất 12 ký tự.");
        Environment.ExitCode = 1;
        return;
    }

    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        var inserted = seedAll
            ? await ApplicationDataSeeder.SeedAsync(db, password)
            : await TeacherDemoSeeder.SeedAsync(db, password);
        if (seedAll)
        {
            Console.WriteLine(inserted ? "Đã nhập và kiểm tra đủ 22 bảng nghiệp vụ." : "Bộ dữ liệu đã tồn tại.");
            return;
        }
        Console.WriteLine(inserted
            ? $"Đã nhập bộ dữ liệu demo Teacher. Tài khoản: {TeacherDemoSeeder.Email}"
            : $"Tài khoản {TeacherDemoSeeder.Email} đã tồn tại. Không tạo thêm dữ liệu.");
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine("Không nhập được dữ liệu demo. Giao dịch đã được hủy.");
        Console.Error.WriteLine(ex.GetBaseException() is Npgsql.PostgresException postgres
            ? $"PostgreSQL {postgres.SqlState}: {postgres.MessageText}"
            : ex.GetBaseException().GetType().Name);
        Environment.ExitCode = 1;
    }
    return;
}

// Đảm bảo schema PostgreSQL có cột phone_number trong bảng users (Student profile/register)
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.ExecuteSqlRaw("ALTER TABLE users ADD COLUMN IF NOT EXISTS phone_number VARCHAR(50);");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Database Migration Note] {ex.Message}");
    }
}

app.UseCors("AllowAll");

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Uploaded files must only be served through the authorized, range-enabled media endpoint.
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/App_Data", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.StatusCode = 404;
        return;
    }
    await next(context);
});

// Keep Teacher entry links canonical before legacy static Views can be served.
var teacherAssets = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{
    "/css/teacher.css", "/js/teacher.js", "/js/teacher-exams.js", "/wwwroot/js/teacher-exams.js",
    "/js/teacher-questions.js", "/wwwroot/js/teacher-questions.js", "/js/teacher-lessons.js", "/wwwroot/js/teacher-lessons.js",
    "/js/teacher-gradebook.js", "/wwwroot/js/teacher-gradebook.js", "/wwwroot/css/teacher.css", "/wwwroot/js/teacher.js"
};
var teacherPages = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{
    "/dashboard.html", "/classes.html", "/class-detail.html", "/schedule.html", "/lessons.html",
    "/questions.html", "/exams.html", "/grading.html", "/results.html", "/profile.html"
};

app.Use(async (context, next) =>
{
    var path = context.Request.Path;
    var teacherPage = path.StartsWithSegments("/Teacher", StringComparison.OrdinalIgnoreCase)
        || path.StartsWithSegments("/Areas/Teacher/Views", StringComparison.OrdinalIgnoreCase);
    var teacherApi = path.StartsWithSegments("/api/teacher", StringComparison.OrdinalIgnoreCase);
    var loginPage = path.Equals(new PathString("/login.html"), StringComparison.OrdinalIgnoreCase)
        || path.Equals(new PathString("/login"), StringComparison.OrdinalIgnoreCase);
    var teacherAsset = path.Value is not null && teacherAssets.Contains(path.Value);
    if (teacherPage || teacherApi || loginPage || teacherAsset)
        context.Response.OnStarting(() =>
        {
            context.Response.Headers.CacheControl = teacherAsset ? "no-cache" : "no-store";
            return Task.CompletedTask;
        });

    if (HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method))
    {
        if (path.StartsWithSegments("/Areas/Teacher/Views", StringComparison.OrdinalIgnoreCase, out var remaining)
            || path.StartsWithSegments("/Teacher/Views", StringComparison.OrdinalIgnoreCase, out remaining))
        {
            if (remaining.Value is not null && teacherPages.Contains(remaining.Value))
            {
                context.Response.Redirect("/Teacher" + remaining.Value.ToLowerInvariant() + context.Request.QueryString);
                return;
            }
        }
    }
    await next(context);
});

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

app.UseAuthentication();
app.UseAuthorization();

// Area routing for Admin, Teacher, and Student
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

// Default fallback route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Endpoint kiểm tra nhanh trạng thái dữ liệu trong ApplicationDbContext
app.MapGet("/api/db-status", (ApplicationDbContext db) =>
{
    return Results.Ok(new
    {
        status = "Success",
        message = "Đã truy vấn dữ liệu từ database.",
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
