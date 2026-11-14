using CNPM_EnglishLearning.Services.Interfaces;
using CNPM_EnglishLearning.Services.Implementations;
using CNPM_EnglishLearning.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Authentication.Cookies;

var seedTeacherDemo = args.Contains("--seed-teacher-demo", StringComparer.Ordinal);
var seedTeacherTest = args.Contains("--seed-teacher-test", StringComparer.Ordinal);
var inspectDatabase = args.Contains("--inspect-database", StringComparer.Ordinal);
var seedAll = args.Contains("--seed-all", StringComparer.Ordinal);
var resetTeacherPassword = args.Contains("--reset-teacher-password", StringComparer.Ordinal);
var builder = WebApplication.CreateBuilder(args.Where(arg => arg != "--seed-teacher-demo"
    && arg != "--seed-teacher-test" && arg != "--inspect-database"
    && arg != "--seed-all" && arg != "--reset-teacher-password").ToArray());

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add MVC services to the container
builder.Services.AddControllersWithViews();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login.html";
        options.Events.OnRedirectToLogin = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api/teacher"))
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            else
                context.Response.Redirect(context.RedirectUri);

            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api/teacher"))
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
            else
                context.Response.Redirect(context.RedirectUri);

            return Task.CompletedTask;
        };
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

// Endpoint kiểm tra nhanh trạng thái dữ liệu mẫu trong ApplicationDbContext
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
