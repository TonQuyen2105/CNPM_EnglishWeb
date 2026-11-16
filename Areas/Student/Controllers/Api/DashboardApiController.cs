using CNPM_EnglishLearning.Data;
using Microsoft.AspNetCore.Mvc;

namespace CNPM_EnglishLearning.Areas.Student.Controllers.Api
{
    /// <summary>
    /// Nhóm 1: API Dashboard & Tổng quan học tập (/api/v1/student/dashboard).
    /// </summary>
    public class DashboardApiController : BaseStudentApiController
    {
        public DashboardApiController(ApplicationDbContext db) : base(db)
        {
        }

        /// <summary>
        /// 1.1. Lấy dữ liệu tổng quan trang chủ học sinh.
        /// Endpoint: GET /api/v1/student/dashboard
        /// </summary>
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboardOverview()
        {
            var studentId = await ResolveStudentIdAsync();
            var result = await Services.Dashboard.GetDashboardOverviewAsync(studentId);
            return ToActionResult(result);
        }
    }
}
