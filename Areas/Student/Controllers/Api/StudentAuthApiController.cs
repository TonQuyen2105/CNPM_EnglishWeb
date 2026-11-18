using CNPM_EnglishLearning.Areas.Student.DTOs;
using CNPM_EnglishLearning.Data;
using Microsoft.AspNetCore.Mvc;

namespace CNPM_EnglishLearning.Areas.Student.Controllers.Api
{
    /// <summary>
    /// API Xác thực & Đăng nhập phân hệ Học sinh.
    /// URL: /api/v1/student/auth/login
    /// </summary>
    public class StudentAuthApiController : BaseStudentApiController
    {
        public StudentAuthApiController(ApplicationDbContext db) : base(db)
        {
        }

        /// <summary>
        /// Đăng nhập dành riêng cho Học sinh.
        /// Kiểm tra: Đúng email + password, role == "student", is_active == TRUE.
        /// Endpoint: POST /api/v1/student/auth/login
        /// </summary>
        [HttpPost("auth/login")]
        public async Task<IActionResult> Login([FromBody] StudentLoginRequestDto request)
        {
            var result = await Services.Auth.LoginAsync(request);
            return ToActionResult(result);
        }
    }
}
