using CNPM_EnglishLearning.Data;
using Microsoft.AspNetCore.Mvc;

namespace CNPM_EnglishLearning.Areas.Student.Controllers.Api
{
    /// <summary>
    /// Nhóm 3: API Lớp học của tôi & Lịch học cá nhân theo tuần.
    /// </summary>
    public class MyClassesApiController : BaseStudentApiController
    {
        public MyClassesApiController(ApplicationDbContext db) : base(db)
        {
        }

        /// <summary>
        /// 3.1. Lấy danh sách lớp học của học sinh (lọc theo trạng thái: ongoing, complete; và tìm kiếm theo từ khóa).
        /// Endpoint: GET /api/v1/student/my-classes?status=ongoing&search=giao+tiếp
        /// </summary>
        [HttpGet("my-classes")]
        public async Task<IActionResult> GetMyClasses(
            [FromQuery(Name = "status")] string? status = null,
            [FromQuery(Name = "search")] string? search = null)
        {
            var studentId = await ResolveStudentIdAsync();
            var result = await Services.ClassSchedule.GetMyClassesAsync(studentId, status, search);
            return ToActionResult(result);
        }

        /// <summary>
        /// 3.2. Xem chi tiết lớp học & Lộ trình các buổi học.
        /// Endpoint: GET /api/v1/student/my-classes/{classId}
        /// </summary>
        [HttpGet("my-classes/{classId:int}")]
        public async Task<IActionResult> GetMyClassDetail([FromRoute] int classId)
        {
            var studentId = await ResolveStudentIdAsync();
            var result = await Services.ClassSchedule.GetMyClassDetailAsync(studentId, classId);
            return ToActionResult(result);
        }

        /// <summary>
        /// 3.3. Xem chi tiết buổi học tiếp theo (hoặc buổi học bất kỳ).
        /// Endpoint: GET /api/v1/student/my-classes/{classId}/lessons/{classLessonId}
        /// </summary>
        [HttpGet("my-classes/{classId:int}/lessons/{classLessonId:int}")]
        public async Task<IActionResult> GetClassLessonDetail([FromRoute] int classId, [FromRoute] int classLessonId)
        {
            var studentId = await ResolveStudentIdAsync();
            var result = await Services.ClassSchedule.GetClassLessonDetailAsync(studentId, classId, classLessonId);
            return ToActionResult(result);
        }

        /// <summary>
        /// 3.4. Xem lịch học cá nhân theo tuần.
        /// Endpoint: GET /api/v1/student/schedule?start_date=2026-11-16&end_date=2026-11-22
        /// </summary>
        [HttpGet("schedule")]
        public async Task<IActionResult> GetWeeklySchedule(
            [FromQuery(Name = "start_date")] DateTime? startDate = null,
            [FromQuery(Name = "end_date")] DateTime? endDate = null)
        {
            var studentId = await ResolveStudentIdAsync();
            var result = await Services.ClassSchedule.GetWeeklyScheduleAsync(studentId, startDate, endDate);
            return ToActionResult(result);
        }
    }
}
