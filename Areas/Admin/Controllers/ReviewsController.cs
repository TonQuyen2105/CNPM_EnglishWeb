using CNPM_EnglishLearning.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CNPM_EnglishLearning.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ReviewsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReviewsController> _logger;

        public ReviewsController(ApplicationDbContext context, ILogger<ReviewsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: /Admin/Reviews
        public IActionResult Index()
        {
            return View("~/Areas/Admin/Views/reviews.html");
        }

        [HttpGet("/Admin/api/Reviews/List")]
        public async Task<IActionResult> List(
            string? search,
            int? courseId,
            int? rating,
            int page = 1,
            int pageSize = 8,
            CancellationToken cancellationToken = default,
            bool lookups = true)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 8;
                if (pageSize > 50) pageSize = 50;

                var ratingStats = await _context.CourseReviews
                    .AsNoTracking()
                    .GroupBy(r => r.Rating)
                    .Select(g => new { Rating = (int)g.Key, Count = g.Count() })
                    .ToListAsync(cancellationToken);

                var totalReviews = ratingStats.Sum(x => x.Count);
                var weightedSum = ratingStats.Sum(x => x.Rating * x.Count);
                var averageRating = totalReviews > 0 ? Math.Round(weightedSum / (double)totalReviews, 1) : 0.0;
                var fiveStarCount = ratingStats.Where(x => x.Rating == 5).Sum(x => x.Count);
                var lowRatingCount = ratingStats.Where(x => x.Rating <= 2).Sum(x => x.Count);

                var query =
                    from r in _context.CourseReviews.AsNoTracking()
                    join u in _context.Users.AsNoTracking() on r.UserId equals u.Id
                    join c in _context.Courses.AsNoTracking() on r.CourseId equals c.Id
                    join s in _context.Subjects.AsNoTracking() on c.SubjectId equals s.Id into subjGroup
                    from subj in subjGroup.DefaultIfEmpty()
                    select new
                    {
                        id = r.Id,
                        userId = u.Id,
                        studentName = u.FullName,
                        studentEmail = u.Email,
                        courseId = c.Id,
                        courseTitle = c.Title,
                        subjectName = subj != null ? subj.Name : null,
                        rating = (int)r.Rating,
                        comment = r.Comment,
                        createdAt = r.CreatedAt
                    };

                if (courseId.HasValue && courseId.Value > 0)
                {
                    query = query.Where(x => x.courseId == courseId.Value);
                }

                if (rating.HasValue && rating.Value >= 1 && rating.Value <= 5)
                {
                    query = query.Where(x => x.rating == rating.Value);
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var term = search.Trim().ToLower();
                    query = query.Where(x =>
                        x.studentName.ToLower().Contains(term) ||
                        x.studentEmail.ToLower().Contains(term) ||
                        x.courseTitle.ToLower().Contains(term) ||
                        (x.comment != null && x.comment.ToLower().Contains(term)));
                }

                var totalFiltered = await query.CountAsync(cancellationToken);
                var totalPages = totalFiltered > 0 ? (int)Math.Ceiling(totalFiltered / (double)pageSize) : 1;
                if (page > totalPages) page = totalPages;

                var items = await query
                    .OrderByDescending(x => x.createdAt)
                    .ThenByDescending(x => x.id)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken);

                var courses = lookups
                    ? await _context.Courses
                        .AsNoTracking()
                        .OrderBy(c => c.Title)
                        .Select(c => new
                        {
                            id = c.Id,
                            title = c.Title
                        })
                        .ToListAsync(cancellationToken)
                    : null;

                return Ok(new
                {
                    success = true,
                    items,
                    courses,
                    pagination = new
                    {
                        page,
                        pageSize,
                        totalCount = totalFiltered,
                        totalPages
                    },
                    stats = new
                    {
                        totalReviews,
                        averageRating,
                        fiveStarCount,
                        lowRatingCount
                    }
                });
            }
            catch (OperationCanceledException)
            {
                return this.ApiCancelled();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể tải danh sách đánh giá khóa học.");
                return this.ApiServerError("Không thể tải danh sách đánh giá khóa học.");
            }
        }

        [HttpPost("/Admin/api/Reviews/Delete/{id}")]
        [HttpDelete("/Admin/api/Reviews/Delete/{id}")]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            if (id <= 0)
            {
                return this.ApiBadRequest("Mã đánh giá không hợp lệ.");
            }

            try
            {
                var review = await _context.CourseReviews.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
                if (review == null)
                {
                    return this.ApiNotFound("Không tìm thấy đánh giá yêu cầu.");
                }

                _context.CourseReviews.Remove(review);
                await _context.SaveChangesAsync(cancellationToken);

                return Ok(new
                {
                    success = true,
                    message = "Đã xóa đánh giá vi phạm/spam thành công.",
                    id
                });
            }
            catch (OperationCanceledException)
            {
                return this.ApiCancelled();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể xóa đánh giá {ReviewId}.", id);
                return this.ApiServerError("Không thể xóa đánh giá.");
            }
        }
    }
}
