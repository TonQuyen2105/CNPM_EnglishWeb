using CNPM_EnglishLearning.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CNPM_EnglishLearning.Controllers
{
    [ApiController]
    [AllowAnonymous]
    [Route("api/public")]
    public class PublicLandingController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PublicLandingController> _logger;

        public PublicLandingController(
            ApplicationDbContext context,
            ILogger<PublicLandingController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// GET /api/public/landing
        /// Trả về dữ liệu công khai trực tiếp từ PostgreSQL cho Landing Page (index.html):
        /// chỉ số tổng quan, khóa học đang mở, giảng viên tiêu biểu và đánh giá học viên.
        /// </summary>
        [HttpGet("landing")]
        public async Task<IActionResult> GetLandingData(CancellationToken cancellationToken)
        {
            try
            {
                var totalStudents = await _context.Users.AsNoTracking()
                    .CountAsync(u => u.Role == "student" && u.IsActive, cancellationToken);

                var totalCourses = await _context.Courses.AsNoTracking()
                    .CountAsync(c => c.IsPublished, cancellationToken);

                var totalTeachers = await (
                    from t in _context.Teachers.AsNoTracking()
                    join u in _context.Users.AsNoTracking() on t.UserId equals u.Id
                    where t.IsVerified && u.IsActive && u.Role == "teacher"
                    select t.Id
                ).CountAsync(cancellationToken);

                var activeClasses = await _context.CourseClasses.AsNoTracking()
                    .CountAsync(cc => cc.Status == "upcoming" || cc.Status == "ongoing", cancellationToken);

                var totalReviews = await _context.CourseReviews.AsNoTracking()
                    .CountAsync(cancellationToken);

                var avgRating = totalReviews > 0
                    ? Math.Round(await _context.CourseReviews.AsNoTracking()
                        .AverageAsync(r => (double)r.Rating, cancellationToken), 1)
                    : 5.0;

                // Danh sách khóa học đang phát hành
                var publishedCourses = await (
                    from c in _context.Courses.AsNoTracking()
                    join s in _context.Subjects.AsNoTracking() on c.SubjectId equals s.Id into sGroup
                    from s in sGroup.DefaultIfEmpty()
                    where c.IsPublished
                    orderby c.CreatedAt descending, c.Id descending
                    select new
                    {
                        c.Id,
                        c.Title,
                        c.Description,
                        c.Level,
                        c.Price,
                        SubjectName = s != null ? s.Name : "Tiếng Anh"
                    }
                ).ToListAsync(cancellationToken);

                var courseIds = publishedCourses.Select(c => c.Id).ToList();

                var lessonCounts = courseIds.Count == 0
                    ? new Dictionary<int, int>()
                    : await _context.Lessons.AsNoTracking()
                        .Where(l => courseIds.Contains(l.CourseId))
                        .GroupBy(l => l.CourseId)
                        .Select(g => new { CourseId = g.Key, Count = g.Count() })
                        .ToDictionaryAsync(x => x.CourseId, x => x.Count, cancellationToken);

                var openClassCounts = courseIds.Count == 0
                    ? new Dictionary<int, int>()
                    : await _context.CourseClasses.AsNoTracking()
                        .Where(cc => courseIds.Contains(cc.CourseId) && (cc.Status == "upcoming" || cc.Status == "ongoing"))
                        .GroupBy(cc => cc.CourseId)
                        .Select(g => new { CourseId = g.Key, Count = g.Count() })
                        .ToDictionaryAsync(x => x.CourseId, x => x.Count, cancellationToken);

                var featuredCourses = publishedCourses
                    .OrderByDescending(c => openClassCounts.GetValueOrDefault(c.Id, 0))
                    .ThenByDescending(c => lessonCounts.GetValueOrDefault(c.Id, 0))
                    .ThenBy(c => c.Id)
                    .Take(6)
                    .Select(c => new
                    {
                        id = c.Id,
                        title = c.Title,
                        description = c.Description,
                        level = string.IsNullOrWhiteSpace(c.Level) ? "beginner" : c.Level,
                        price = c.Price,
                        subjectName = c.SubjectName,
                        lessonCount = lessonCounts.GetValueOrDefault(c.Id, 0),
                        openClassCount = openClassCounts.GetValueOrDefault(c.Id, 0)
                    })
                    .ToList();

                // Giảng viên tiêu biểu (đã xác minh & tài khoản hoạt động)
                var teachersRaw = await (
                    from t in _context.Teachers.AsNoTracking()
                    join u in _context.Users.AsNoTracking() on t.UserId equals u.Id
                    where t.IsVerified && u.IsActive && u.Role == "teacher"
                    select new
                    {
                        t.Id,
                        u.FullName,
                        u.AvatarUrl,
                        t.Qualification,
                        t.Bio,
                        ClassCount = _context.ClassTeachers.Count(ct => ct.TeacherId == t.Id)
                    }
                )
                .OrderByDescending(x => x.ClassCount)
                .ThenBy(x => x.FullName)
                .Take(4)
                .ToListAsync(cancellationToken);

                var teachers = teachersRaw.Select(t => new
                {
                    id = t.Id,
                    fullName = t.FullName,
                    avatarUrl = t.AvatarUrl,
                    qualification = string.IsNullOrWhiteSpace(t.Qualification) ? "Giảng viên tiếng Anh" : t.Qualification,
                    bio = string.IsNullOrWhiteSpace(t.Bio) ? "Giảng viên giàu kinh nghiệm tại hệ thống EnglishLearning." : t.Bio,
                    classCount = t.ClassCount
                }).ToList();

                // Cảm nhận học viên tiêu biểu (ưu tiên đánh giá có nội dung & điểm cao, mới nhất)
                var reviewsRaw = await (
                    from r in _context.CourseReviews.AsNoTracking()
                    join u in _context.Users.AsNoTracking() on r.UserId equals u.Id
                    join c in _context.Courses.AsNoTracking() on r.CourseId equals c.Id
                    where r.Comment != null && r.Comment != ""
                    orderby r.Rating descending, r.CreatedAt descending, r.Id descending
                    select new
                    {
                        r.Id,
                        StudentName = u.FullName,
                        CourseTitle = c.Title,
                        r.Rating,
                        r.Comment,
                        r.CreatedAt
                    }
                )
                .Take(3)
                .ToListAsync(cancellationToken);

                var reviews = reviewsRaw.Select(r => new
                {
                    id = r.Id,
                    studentName = r.StudentName,
                    courseTitle = r.CourseTitle,
                    rating = r.Rating,
                    comment = r.Comment,
                    createdAt = r.CreatedAt
                }).ToList();

                return Ok(new
                {
                    success = true,
                    stats = new
                    {
                        totalStudents,
                        totalCourses,
                        totalTeachers,
                        activeClasses,
                        avgRating,
                        totalReviews
                    },
                    courses = featuredCourses,
                    teachers,
                    reviews
                });
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499, new { success = false, message = "Yêu cầu đã bị hủy." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể tải dữ liệu Landing Page công khai.");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Không thể tải dữ liệu trang chủ từ máy chủ. Vui lòng thử lại sau."
                });
            }
        }
    }
}
