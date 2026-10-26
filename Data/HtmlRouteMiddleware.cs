using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace CNPM_EnglishLearning.Data
{
    /// <summary>
    /// Middleware điều hướng thông minh cho tất cả các trang giao diện HTML (.html)
    /// Đảm bảo mọi đường dẫn (dù là /Admin/classes.html, /Student/exams.html, /Teacher/lessons.html,
    /// hay /Areas/... hoa/thường) đều hiển thị chính xác nội dung HTML, KHÔNG BAO GIỜ BỊ TRANG TRẮNG.
    /// </summary>
    public class HtmlRouteMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly string _contentRoot;
        private readonly Dictionary<string, string> _viewMap = new(StringComparer.OrdinalIgnoreCase);

        public HtmlRouteMiddleware(RequestDelegate next, IWebHostEnvironment env)
        {
            _next = next;
            _contentRoot = env.ContentRootPath;

            // 1. Quét và lập chỉ mục toàn bộ các file .html trong Areas/
            var areasDir = Path.Combine(_contentRoot, "Areas");
            if (Directory.Exists(areasDir))
            {
                var areaFiles = Directory.GetFiles(areasDir, "*.html", SearchOption.AllDirectories);
                foreach (var file in areaFiles)
                {
                    var rel = Path.GetRelativePath(_contentRoot, file); // vd: Areas/Admin/Views/dashboard.html
                    var parts = rel.Split(Path.DirectorySeparatorChar);
                    if (parts.Length >= 4)
                    {
                        var area = parts[1]; // Admin
                        var fileName = parts[3]; // dashboard.html
                        var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName); // dashboard

                        // Ánh xạ linh hoạt mọi định dạng URL mà trình duyệt có thể gọi
                        _viewMap[$"/{area}/{fileName}"] = file;                        // vd: /Admin/dashboard.html, /Student/exams.html
                        _viewMap[$"/{area}/{nameWithoutExt}"] = file;                  // vd: /Admin/dashboard, /Student/exams
                        _viewMap[$"/{area}/Views/{fileName}"] = file;                  // vd: /Admin/Views/dashboard.html
                        _viewMap[$"/Areas/{area}/Views/{fileName}"] = file;            // vd: /Areas/Admin/Views/dashboard.html
                        _viewMap[$"/{fileName}"] = file;                               // fallback nếu gọi trực tiếp tên file
                    }
                }
            }

            // 2. Ánh xạ các trang mặc định của từng Cổng
            MapDefaultArea("Admin", "dashboard.html");
            MapDefaultArea("Teacher", "dashboard.html");
            MapDefaultArea("Student", "dashboard.html");

            // 3. Ánh xạ các trang gốc (Trang chủ công khai, Đăng nhập, Style Guide)
            var indexFile = Path.Combine(_contentRoot, "index.html");
            if (File.Exists(indexFile))
            {
                _viewMap["/"] = indexFile;
                _viewMap["/index.html"] = indexFile;
                _viewMap["/Home"] = indexFile;
                _viewMap["/Home/Index"] = indexFile;
            }

            var loginFile = Path.Combine(_contentRoot, "login.html");
            if (File.Exists(loginFile))
            {
                _viewMap["/login.html"] = loginFile;
                _viewMap["/login"] = loginFile;
                _viewMap["/Account/Login"] = loginFile;
                _viewMap["/Account/Logout"] = loginFile;
                _viewMap["/logout"] = loginFile;
            }

            var styleguideFile = Path.Combine(_contentRoot, "styleguide.html");
            if (File.Exists(styleguideFile))
            {
                _viewMap["/styleguide.html"] = styleguideFile;
                _viewMap["/Home/StyleGuide"] = styleguideFile;
            }
        }

        private void MapDefaultArea(string area, string defaultFile)
        {
            var key = $"/{area}/{defaultFile}";
            if (_viewMap.TryGetValue(key, out var path))
            {
                _viewMap[$"/{area}"] = path;
                _viewMap[$"/{area}/"] = path;
            }
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var rawPath = context.Request.Path.Value ?? "";

            // Bỏ qua các file tĩnh tài nguyên (css, js, ảnh, font...)
            if (rawPath.StartsWith("/css", StringComparison.OrdinalIgnoreCase) ||
                rawPath.StartsWith("/js", StringComparison.OrdinalIgnoreCase) ||
                rawPath.StartsWith("/images", StringComparison.OrdinalIgnoreCase) ||
                rawPath.StartsWith("/lib", StringComparison.OrdinalIgnoreCase) ||
                rawPath.StartsWith("/wwwroot", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            // Kiểm tra đường dẫn có khớp với trang HTML nào không
            var cleanPath = rawPath.TrimEnd('/');
            if (string.IsNullOrEmpty(cleanPath)) cleanPath = "/";

            if (_viewMap.TryGetValue(cleanPath, out var filePath) || _viewMap.TryGetValue(rawPath, out filePath))
            {
                if (File.Exists(filePath))
                {
                    context.Response.ContentType = "text/html; charset=utf-8";
                    await context.Response.SendFileAsync(filePath);
                    return;
                }
            }

            await _next(context);
        }
    }
}
