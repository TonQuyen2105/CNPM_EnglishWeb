using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;

namespace CNPM_EnglishLearning.Data
{
    /// <summary>
    /// Cho phép ASP.NET Core MVC tự động render các view file .html thuần
    /// giúp xem trực tiếp giao diện và dữ liệu mẫu thông qua Controller actions
    /// </summary>
    public class HtmlViewEngine : IViewEngine
    {
        public ViewEngineResult FindView(ActionContext context, string viewName, bool isMainPage)
        {
            return GetView(null, viewName, isMainPage);
        }

        public ViewEngineResult GetView(string? executingFilePath, string viewPath, bool isMainPage)
        {
            if (string.IsNullOrEmpty(viewPath) || (!viewPath.EndsWith(".html", StringComparison.OrdinalIgnoreCase) && !viewPath.EndsWith(".htm", StringComparison.OrdinalIgnoreCase)))
            {
                return ViewEngineResult.NotFound(viewPath ?? string.Empty, Enumerable.Empty<string>());
            }

            var cleanPath = viewPath.Replace("~/", "").TrimStart('/');
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), cleanPath);

            if (File.Exists(fullPath))
            {
                return ViewEngineResult.Found(viewPath, new HtmlView(fullPath));
            }

            return ViewEngineResult.NotFound(viewPath, new[] { fullPath });
        }
    }

    public class HtmlView : IView
    {
        public string Path { get; }
        public HtmlView(string path) => Path = path;

        public async Task RenderAsync(ViewContext context)
        {
            context.HttpContext.Response.ContentType = "text/html; charset=utf-8";
            var content = await File.ReadAllTextAsync(Path);
            await context.Writer.WriteAsync(content);
        }
    }
}
