using CNPM_EnglishLearning.Services.Interfaces;
using CNPM_EnglishLearning.Services.Implementations;
using CNPM_EnglishLearning.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// Add MVC services to the container
builder.Services.AddControllersWithViews();

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

app.Run();
