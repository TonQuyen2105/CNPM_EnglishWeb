using CNPM_EnglishLearning.Areas.Student.DTOs;
using CNPM_EnglishLearning.Data;
using Microsoft.AspNetCore.Mvc;

namespace CNPM_EnglishLearning.Areas.Student.Controllers.Api
{
    /// <summary>
    /// Nhóm 2, Nhóm 4.1 & Nhóm 8: API Khám phá Khóa học, Đề cương, Đăng ký Tư vấn, Đăng ký Lớp & Đánh giá Khóa học.
    /// </summary>
    public class CoursesApiController : BaseStudentApiController
    {
        public CoursesApiController(ApplicationDbContext db) : base(db)
        {
        }

        /// <summary>
        /// 2.1. Lấy danh sách khóa học (Có lọc & phân trang).
        /// Endpoint: GET /api/v1/student/courses?subject_id=1&level=intermediate&search=IELTS&page=1&limit=10
        /// </summary>
        [HttpGet("courses")]
        public async Task<IActionResult> GetCourses([FromQuery] CourseFilterQueryDto query)
        {
            var result = await Services.Courses.GetPublishedCoursesAsync(query);
            return ToActionResult(result);
        }

        /// <summary>
        /// 2.2. Xem chi tiết khóa học, đề cương & danh sách lớp mở đăng ký.
        /// Endpoint: GET /api/v1/student/courses/{courseId}
        /// </summary>
        [HttpGet("courses/{courseId:int}")]
        public async Task<IActionResult> GetCourseDetail([FromRoute] int courseId)
        {
            var studentId = await TryResolveOptionalStudentIdAsync();
            var result = await Services.Courses.GetCourseDetailAsync(courseId, studentId);
            return ToActionResult(result);
        }

        /// <summary>
        /// 2.3. Gửi yêu cầu đăng ký tư vấn khóa học.
        /// Endpoint: POST /api/v1/student/consultations
        /// </summary>
        [HttpPost("consultations")]
        public async Task<IActionResult> CreateConsultation([FromBody] CreateConsultationRequestDto request)
        {
            var studentId = await TryResolveOptionalStudentIdAsync();
            var result = await Services.Courses.CreateConsultationAsync(studentId, request);
            return ToActionResult(result);
        }

        /// <summary>
        /// 2.4. Đăng ký tham gia lớp học (Enrollment & Payment).
        /// Endpoint: POST /api/v1/student/enrollments
        /// </summary>
        [HttpPost("enrollments")]
        public async Task<IActionResult> EnrollClass([FromBody] CreateEnrollmentRequestDto request)
        {
            var studentId = await ResolveStudentIdAsync();
            var result = await Services.Courses.EnrollClassAsync(studentId, request);
            return ToActionResult(result);
        }

        /// <summary>
        /// 4.1. Lấy mục lục bài học của một khóa học kèm trạng thái hoàn thành.
        /// Endpoint: GET /api/v1/student/courses/{courseId}/syllabus
        /// </summary>
        [HttpGet("courses/{courseId:int}/syllabus")]
        public async Task<IActionResult> GetCourseSyllabus([FromRoute] int courseId)
        {
            var studentId = await ResolveStudentIdAsync();
            var result = await Services.Learning.GetCourseSyllabusAsync(studentId, courseId);
            return ToActionResult(result);
        }

        /// <summary>
        /// 8.1. Lấy danh sách đánh giá gần đây của khóa học.
        /// Endpoint: GET /api/v1/student/courses/{courseId}/reviews?page=1&limit=10
        /// </summary>
        [HttpGet("courses/{courseId:int}/reviews")]
        public async Task<IActionResult> GetCourseReviews(
            [FromRoute] int courseId,
            [FromQuery(Name = "page")] int page = 1,
            [FromQuery(Name = "limit")] int limit = 10)
        {
            var result = await Services.Courses.GetCourseReviewsAsync(courseId, page, limit);
            return ToActionResult(result);
        }

        /// <summary>
        /// 8.2. Gửi đánh giá khóa học.
        /// Endpoint: POST /api/v1/student/courses/{courseId}/reviews
        /// </summary>
        [HttpPost("courses/{courseId:int}/reviews")]
        public async Task<IActionResult> SubmitCourseReview(
            [FromRoute] int courseId,
            [FromBody] CreateCourseReviewRequestDto request)
        {
            var studentId = await ResolveStudentIdAsync();
            var result = await Services.Courses.SubmitCourseReviewAsync(studentId, courseId, request);
            return ToActionResult(result);
        }
    }
}
