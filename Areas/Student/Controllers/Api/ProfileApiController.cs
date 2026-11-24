using CNPM_EnglishLearning.Areas.Student.DTOs;
using CNPM_EnglishLearning.Data;
using Microsoft.AspNetCore.Mvc;

namespace CNPM_EnglishLearning.Areas.Student.Controllers.Api
{
    /// <summary>
    /// Nhóm 9: API Quản lý Hồ sơ Cá nhân Học sinh (Profile).
    /// </summary>
    public class ProfileApiController : BaseStudentApiController
    {
        public ProfileApiController(ApplicationDbContext db) : base(db)
        {
        }

        /// <summary>
        /// 9.1. Xem thông tin hồ sơ học sinh.
        /// Endpoint: GET /api/v1/student/profile
        /// </summary>
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var studentId = await ResolveStudentIdAsync();
            var result = await Services.Profile.GetProfileAsync(studentId);
            return ToActionResult(result);
        }

        /// <summary>
        /// 9.2. Cập nhật thông tin hồ sơ cá nhân.
        /// Endpoint: PUT /api/v1/student/profile
        /// </summary>
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateStudentProfileRequestDto request)
        {
            var studentId = await ResolveStudentIdAsync();
            var result = await Services.Profile.UpdateProfileAsync(studentId, request);
            return ToActionResult(result);
        }
    }
}
