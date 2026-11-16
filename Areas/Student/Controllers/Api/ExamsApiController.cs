using CNPM_EnglishLearning.Areas.Student.DTOs;
using CNPM_EnglishLearning.Data;
using Microsoft.AspNetCore.Mvc;

namespace CNPM_EnglishLearning.Areas.Student.Controllers.Api
{
    /// <summary>
    /// Nhóm 6: API Hệ thống Thi trực tuyến, Tự động lưu đáp án, Chấm điểm & Xem kết quả chi tiết.
    /// </summary>
    public class ExamsApiController : BaseStudentApiController
    {
        public ExamsApiController(ApplicationDbContext db) : base(db)
        {
        }

        /// <summary>
        /// 6.1. Lấy danh sách bài thi hiện có.
        /// Endpoint: GET /api/v1/student/exams?course_id=5
        /// </summary>
        [HttpGet("exams")]
        public async Task<IActionResult> GetAvailableExams([FromQuery(Name = "course_id")] int? courseId = null)
        {
            var studentId = await ResolveStudentIdAsync();
            var result = await Services.Exams.GetAvailableExamsAsync(studentId, courseId);
            return ToActionResult(result);
        }

        /// <summary>
        /// 6.2. Bắt đầu làm bài thi (Khởi tạo phiên thi hoặc tiếp tục phiên in_progress chưa hết giờ).
        /// Endpoint: POST /api/v1/student/exams/{examId}/start
        /// </summary>
        [HttpPost("exams/{examId:int}/start")]
        public async Task<IActionResult> StartExam([FromRoute] int examId)
        {
            var studentId = await ResolveStudentIdAsync();
            var result = await Services.Exams.StartExamSessionAsync(studentId, examId);
            return ToActionResult(result);
        }

        /// <summary>
        /// 6.3. Lưu nháp câu trả lời trong lúc thi (Auto-save / Chuyển câu).
        /// Endpoint: PUT /api/v1/student/user-exams/{userExamId}/answers
        /// </summary>
        [HttpPut("user-exams/{userExamId:int}/answers")]
        public async Task<IActionResult> SaveAnswer(
            [FromRoute] int userExamId,
            [FromBody] SaveExamAnswerRequestDto request)
        {
            var studentId = await ResolveStudentIdAsync();
            var result = await Services.Exams.SaveExamAnswerAsync(studentId, userExamId, request);
            return ToActionResult(result);
        }

        /// <summary>
        /// 6.4. Nộp bài thi & Chấm điểm tự động.
        /// Endpoint: POST /api/v1/student/user-exams/{userExamId}/submit
        /// </summary>
        [HttpPost("user-exams/{userExamId:int}/submit")]
        public async Task<IActionResult> SubmitExam([FromRoute] int userExamId)
        {
            var studentId = await ResolveStudentIdAsync();
            var result = await Services.Exams.SubmitExamAsync(studentId, userExamId);
            return ToActionResult(result);
        }

        /// <summary>
        /// 6.5. Xem lịch sử các bài thi đã nộp.
        /// Endpoint: GET /api/v1/student/user-exams/history
        /// </summary>
        [HttpGet("user-exams/history")]
        public async Task<IActionResult> GetExamHistory()
        {
            var studentId = await ResolveStudentIdAsync();
            var result = await Services.Exams.GetExamHistoryAsync(studentId);
            return ToActionResult(result);
        }

        /// <summary>
        /// 6.6. Xem lại bài giải và kết quả chi tiết từng câu.
        /// Endpoint: GET /api/v1/student/user-exams/{userExamId}/review
        /// </summary>
        [HttpGet("user-exams/{userExamId:int}/review")]
        public async Task<IActionResult> GetExamReview([FromRoute] int userExamId)
        {
            var studentId = await ResolveStudentIdAsync();
            var result = await Services.Exams.GetExamReviewAsync(studentId, userExamId);
            return ToActionResult(result);
        }
    }
}
