using Microsoft.AspNetCore.Mvc;

namespace CNPM_EnglishLearning.Areas.Admin.Controllers
{
    /// <summary>
    /// Helper chuẩn hóa response lỗi của các API Admin theo cấu trúc JSON đồng nhất:
    /// { success = false, message = "..." }
    /// </summary>
    public static class AdminApiResponses
    {
        /// <summary>HTTP 499: client đã hủy request (người dùng đóng tab / chuyển trang).</summary>
        public const int ClientClosedRequest = 499;

        public static IActionResult ApiBadRequest(this ControllerBase controller, string message)
            => new BadRequestObjectResult(new { success = false, message });

        public static IActionResult ApiNotFound(this ControllerBase controller, string message)
            => new NotFoundObjectResult(new { success = false, message });

        public static IActionResult ApiServerError(this ControllerBase controller, string message)
            => new ObjectResult(new { success = false, message }) { StatusCode = StatusCodes.Status500InternalServerError };

        public static IActionResult ApiCancelled(this ControllerBase controller)
            => new StatusCodeResult(ClientClosedRequest);
    }
}
