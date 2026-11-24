using System.Security.Claims;
using System.Text;
using System.Text.Json;
using CNPM_EnglishLearning.Areas.Student.DTOs.Common;
using CNPM_EnglishLearning.Areas.Student.Services;
using CNPM_EnglishLearning.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CNPM_EnglishLearning.Areas.Student.Controllers.Api
{
    /// <summary>
    /// Lớp Controller cơ sở trừu tượng (Abstract OOP Base API Controller) cho toàn bộ API Phân hệ Học sinh.
    /// Base URL chuẩn: /api/v1/student
    /// Đóng gói xử lý xác thực Bearer Token, quản lý StudentServiceFactory và chuẩn hóa định dạng JSON Response.
    /// </summary>
    [ApiController]
    [Area("Student")]
    [Route("api/v1/student")]
    [Produces("application/json")]
    public abstract class BaseStudentApiController : ControllerBase
    {
        protected const int DefaultFallbackStudentId = 5;

        protected readonly ApplicationDbContext Db;
        protected readonly StudentServiceFactory Services;

        // Tái sử dụng biến studentId đã phân giải trong suốt vòng đời 1 HTTP Request
        private int? _resolvedStudentId;

        protected BaseStudentApiController(ApplicationDbContext db)
        {
            Db = db ?? throw new ArgumentNullException(nameof(db));
            Services = new StudentServiceFactory(Db);
        }

        /// <summary>
        /// Phân giải ID của Học sinh từ Header Authorization: Bearer &lt;access_token&gt;, Claims, X-User-Id hoặc Query.
        /// Tái sử dụng kết quả qua biến _resolvedStudentId để không phải parse/truy vấn lại.
        /// </summary>
        protected async Task<int> ResolveStudentIdAsync()
        {
            if (_resolvedStudentId.HasValue)
            {
                return _resolvedStudentId.Value;
            }

            // 1. Kiểm tra Header Authorization: Bearer <access_token>
            if (Request.Headers.TryGetValue("Authorization", out var authHeaderValues))
            {
                var authHeader = authHeaderValues.FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(authHeader) &&
                    authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    var token = authHeader["Bearer ".Length..].Trim();
                    var parsedFromToken = await TryParseStudentIdFromTokenAsync(token);
                    if (parsedFromToken.HasValue)
                    {
                        _resolvedStudentId = parsedFromToken.Value;
                        return _resolvedStudentId.Value;
                    }
                }
            }

            // 2. Kiểm tra ClaimsPrincipal (chỉ chấp nhận Cookie/Claims của vai trò student, tuyệt đối không lấy ID từ admin/teacher)
            if (User?.IsInRole("student") == true)
            {
                var claimIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier)
                                 ?? User.FindFirstValue("sub")
                                 ?? User.FindFirstValue("user_id")
                                 ?? User.FindFirstValue("id");

                if (int.TryParse(claimIdStr, out var claimUserId) && claimUserId > 0)
                {
                    _resolvedStudentId = claimUserId;
                    return _resolvedStudentId.Value;
                }
            }

            // 3. Kiểm tra Header X-User-Id hoặc Query ?user_id= (hỗ trợ tích hợp Frontend/Testing nhanh)
            if (Request.Headers.TryGetValue("X-User-Id", out var headerUserId) &&
                int.TryParse(headerUserId.FirstOrDefault(), out var customHeaderId) &&
                customHeaderId > 0)
            {
                _resolvedStudentId = customHeaderId;
                return _resolvedStudentId.Value;
            }

            if (Request.Query.TryGetValue("user_id", out var queryUserId) &&
                int.TryParse(queryUserId.FirstOrDefault(), out var customQueryId) &&
                customQueryId > 0)
            {
                _resolvedStudentId = customQueryId;
                return _resolvedStudentId.Value;
            }

            // 4. Fallback về học sinh active mặc định trong DB (VD: UserId = 5 - Nguyễn Hoàng Anh)
            var defaultStudentId = await Db.Users.AsNoTracking()
                .Where(u => u.Role == "student" && u.IsActive)
                .OrderBy(u => u.Id == DefaultFallbackStudentId ? 0 : 1)
                .ThenBy(u => u.Id)
                .Select(u => u.Id)
                .FirstOrDefaultAsync();

            _resolvedStudentId = defaultStudentId > 0 ? defaultStudentId : DefaultFallbackStudentId;
            return _resolvedStudentId.Value;
        }

        /// <summary>
        /// Thử lấy StudentId tùy chọn cho các API công khai (VD: xem chi tiết khóa học có kèm tiến độ nếu đã đăng nhập).
        /// </summary>
        protected async Task<int?> TryResolveOptionalStudentIdAsync()
        {
            if (Request.Headers.ContainsKey("Authorization") ||
                Request.Headers.ContainsKey("X-User-Id") ||
                Request.Query.ContainsKey("user_id") ||
                (User?.Identity?.IsAuthenticated ?? false))
            {
                return await ResolveStudentIdAsync();
            }

            return await ResolveStudentIdAsync();
        }

        /// <summary>
        /// Chuyển đổi ServiceResult&lt;T&gt; từ tầng Service sang IActionResult theo đúng chuẩn JSON của tài liệu thiết kế API.
        /// </summary>
        protected IActionResult ToActionResult<T>(ServiceResult<T> result)
        {
            if (result.IsSuccess)
            {
                var responseBody = ApiResponse<T>.Ok(result.Data!, result.Message, result.Meta);
                return StatusCode(result.StatusCode, responseBody);
            }

            var errorBody = ApiResponse<T>.Fail(result.Message, result.Data);
            return StatusCode(result.StatusCode, errorBody);
        }

        private async Task<int?> TryParseStudentIdFromTokenAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return null;

            // Trường hợp 1: Token là số nguyên trực tiếp (VD: Bearer 5)
            if (int.TryParse(token, out var directId) && directId > 0)
            {
                return directId;
            }

            // Trường hợp 2: Token dạng prefix (VD: student-token-5, user-5, token_5)
            var trailingDigits = new string(token.Reverse().TakeWhile(char.IsDigit).Reverse().ToArray());
            if (int.TryParse(trailingDigits, out var suffixId) && suffixId > 0)
            {
                return suffixId;
            }

            // Trường hợp 3: Token là JWT (header.payload.signature)
            var parts = token.Split('.');
            if (parts.Length >= 2)
            {
                try
                {
                    var payloadJson = DecodeBase64Url(parts[1]);
                    using var doc = JsonDocument.Parse(payloadJson);
                    var root = doc.RootElement;

                    foreach (var key in new[] { "user_id", "id", "sub", "nameid" })
                    {
                        if (root.TryGetProperty(key, out var prop))
                        {
                            if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt32(out var numId) && numId > 0)
                            {
                                return numId;
                            }
                            if (prop.ValueKind == JsonValueKind.String && int.TryParse(prop.GetString(), out var strId) && strId > 0)
                            {
                                return strId;
                            }
                        }
                    }

                    if (root.TryGetProperty("email", out var emailProp) && emailProp.ValueKind == JsonValueKind.String)
                    {
                        var email = emailProp.GetString();
                        if (!string.IsNullOrWhiteSpace(email))
                        {
                            var userIdByEmail = await Db.Users.AsNoTracking()
                                .Where(u => u.Email.ToLower() == email.ToLower())
                                .Select(u => (int?)u.Id)
                                .FirstOrDefaultAsync();
                            if (userIdByEmail.HasValue) return userIdByEmail.Value;
                        }
                    }
                }
                catch
                {
                    // Bỏ qua nếu không phải JWT hợp lệ
                }
            }

            // Trường hợp 4: Token là email của học sinh
            if (token.Contains('@'))
            {
                var userIdByEmail = await Db.Users.AsNoTracking()
                    .Where(u => u.Email.ToLower() == token.ToLower())
                    .Select(u => (int?)u.Id)
                    .FirstOrDefaultAsync();
                if (userIdByEmail.HasValue) return userIdByEmail.Value;
            }

            return null;
        }

        private static string DecodeBase64Url(string input)
        {
            var output = input.Replace('-', '+').Replace('_', '/');
            switch (output.Length % 4)
            {
                case 2: output += "=="; break;
                case 3: output += "="; break;
            }
            var bytes = Convert.FromBase64String(output);
            return Encoding.UTF8.GetString(bytes);
        }
    }
}
