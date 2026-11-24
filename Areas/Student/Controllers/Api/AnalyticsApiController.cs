using CNPM_EnglishLearning.Data;
using Microsoft.AspNetCore.Mvc;

namespace CNPM_EnglishLearning.Areas.Student.Controllers.Api
{
    /// <summary>
    /// Nhóm 7: API Thống kê Năng lực 4 Kỹ năng & Chỉ số Học tập Tích lũy.
    /// </summary>
    public class AnalyticsApiController : BaseStudentApiController
    {
        public AnalyticsApiController(ApplicationDbContext db) : base(db)
        {
        }

        /// <summary>
        /// 7.1. Lấy báo cáo phân tích 4 kỹ năng (Listening, Reading, Writing, Speaking) & Chỉ số tích lũy.
        /// Endpoint: GET /api/v1/student/analytics/overview
        /// </summary>
        [HttpGet("analytics/overview")]
        public async Task<IActionResult> GetAnalyticsOverview()
        {
            var studentId = await ResolveStudentIdAsync();
            var result = await Services.Analytics.GetAnalyticsOverviewAsync(studentId);
            return ToActionResult(result);
        }

        /// <summary>
        /// 7.2. Lấy dữ liệu toàn diện tiến độ học tập của học sinh (4 kỹ năng, chỉ số tích lũy, tiến độ từng lớp và hoạt động gần đây).
        /// Endpoint: GET /api/v1/student/progress
        /// </summary>
        [HttpGet("progress")]
        public async Task<IActionResult> GetProgress()
        {
            var studentId = await ResolveStudentIdAsync();
            var result = await Services.Analytics.GetAnalyticsOverviewAsync(studentId);
            return ToActionResult(result);
        }
    }
}
