using CNPM_EnglishLearning.Services.Interfaces;
using CNPM_EnglishLearning.Services.Implementations;
using CNPM_EnglishLearning.Data;
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

// Đảm bảo schema PostgreSQL có cột phone_number trong bảng users
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
