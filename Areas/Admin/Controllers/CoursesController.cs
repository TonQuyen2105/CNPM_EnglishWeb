using CNPM_EnglishLearning.Data;
using CNPM_EnglishLearning.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CNPM_EnglishLearning.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class CoursesController : Controller
    {
        private const int MaxTitleLength = 200;
        private const int MaxDescriptionLength = 4000;
        private const int MaxLessonContentLength = 20000;
        private const decimal MaxPrice = 99_999_999.99m;
        private const int MaxInitialLessons = 100;

        private static readonly HashSet<string> ValidLevels = new(StringComparer.OrdinalIgnoreCase)
        {
            "beginner", "intermediate", "advanced"
        };

        private static readonly HashSet<string> ValidLessonTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "vocabulary", "grammar", "listening", "reading", "speaking", "writing", "quiz"
        };

        private readonly ApplicationDbContext _context;
        private readonly ILogger<CoursesController> _logger;

        public CoursesController(ApplicationDbContext context, ILogger<CoursesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: /Admin/Courses
        public IActionResult Index()
        {
            return View("~/Areas/Admin/Views/courses.html");
        }

        // GET: /Admin/Courses/Detail/1
        public IActionResult Detail(int id)
        {
            return View("~/Areas/Admin/Views/course-detail.html");
        }

        // GET: /Admin/Courses/Objectives
        public IActionResult Objectives()
        {
            return View("~/Areas/Admin/Views/course-objectives.html");
        }

        [HttpGet("/Admin/api/Courses/List")]
        public async Task<IActionResult> List(
            string? search,
            int? subjectId,
            string? level,
            string? published,
            CancellationToken cancellationToken,
            bool lookups = true)
        {
            try
            {
                var query =
                    from c in _context.Courses.AsNoTracking()
                    join s in _context.Subjects.AsNoTracking() on c.SubjectId equals s.Id into subjGroup
                    from subj in subjGroup.DefaultIfEmpty()
                    select new
                    {
                        id = c.Id,
                        subjectId = c.SubjectId,
                        subjectName = subj != null ? subj.Name : "Chưa phân môn",
                        title = c.Title,
                        description = c.Description,
                        thumbnailUrl = c.ThumbnailUrl,
                        level = c.Level ?? "beginner",
                        price = c.Price,
                        isPublished = c.IsPublished,
                        createdAt = c.CreatedAt,
                        updatedAt = c.UpdatedAt,
                        lessonCount = _context.Lessons.Count(l => l.CourseId == c.Id),
                        classCount = _context.CourseClasses.Count(cc => cc.CourseId == c.Id && cc.Status != "cancelled")
                    };

                if (subjectId.HasValue && subjectId.Value > 0)
                {
                    query = query.Where(x => x.subjectId == subjectId.Value);
                }

                if (!string.IsNullOrWhiteSpace(level) && !level.Equals("all", StringComparison.OrdinalIgnoreCase))
                {
                    var normLevel = level.Trim().ToLowerInvariant();
                    if (ValidLevels.Contains(normLevel))
                    {
                        query = query.Where(x => x.level.ToLower() == normLevel);
                    }
                }

                if (!string.IsNullOrWhiteSpace(published) && !published.Equals("all", StringComparison.OrdinalIgnoreCase))
                {
                    var normPub = published.Trim().ToLowerInvariant();
                    if (normPub is "1" or "true" or "published")
                    {
                        query = query.Where(x => x.isPublished);
                    }
                    else if (normPub is "0" or "false" or "draft")
                    {
                        query = query.Where(x => !x.isPublished);
                    }
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var term = search.Trim().ToLower();
                    query = query.Where(x =>
                        x.title.ToLower().Contains(term) ||
                        x.subjectName.ToLower().Contains(term) ||
                        (x.description != null && x.description.ToLower().Contains(term)));
                }

                var items = await query
                    .OrderByDescending(x => x.id)
                    .ToListAsync(cancellationToken);

                var subjects = lookups
                    ? await _context.Subjects
                        .AsNoTracking()
                        .OrderBy(s => s.Name)
                        .Select(s => new
                        {
                            id = s.Id,
                            name = s.Name,
                            isActive = s.IsActive
                        })
                        .ToListAsync(cancellationToken)
                    : null;

                return Ok(new { success = true, items, subjects });
            }
            catch (OperationCanceledException)
            {
                return this.ApiCancelled();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể tải danh sách khóa học.");
                return this.ApiServerError("Không thể tải danh sách khóa học.");
            }
        }

        [HttpPost("/Admin/api/Courses/Create")]
        public async Task<IActionResult> Create([FromBody] CourseUpsertDto? dto, CancellationToken cancellationToken)
        {
            var error = NormalizeAndValidateCourse(dto, out var title, out var level, out var description, out var thumbnailUrl);
            if (error != null)
            {
                return this.ApiBadRequest(error);
            }

            var lessonCount = dto!.LessonCount ?? 0;
            if (lessonCount < 0 || lessonCount > MaxInitialLessons)
            {
                return this.ApiBadRequest($"Số bài học khởi tạo phải từ 0 đến {MaxInitialLessons}.");
            }

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var subjectExists = await _context.Subjects
                    .AsNoTracking()
                    .AnyAsync(s => s.Id == dto.SubjectId, cancellationToken);
                if (!subjectExists)
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiBadRequest("Môn học được chọn không tồn tại.");
                }

                var duplicateTitle = await _context.Courses
                    .AsNoTracking()
                    .AnyAsync(c => c.SubjectId == dto.SubjectId && c.Title.ToLower() == title.ToLower(), cancellationToken);
                if (duplicateTitle)
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiBadRequest("Môn học này đã có khóa học cùng tên.");
                }

                var now = DateTime.UtcNow;
                var course = new Course
                {
                    SubjectId = dto.SubjectId,
                    Title = title,
                    Description = description,
                    ThumbnailUrl = thumbnailUrl,
                    Level = level,
                    Price = dto.Price,
                    IsPublished = dto.IsPublished ?? true,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                _context.Courses.Add(course);
                try
                {
                    await _context.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiBadRequest("Khóa học này đã tồn tại trong hệ thống.");
                }

                if (lessonCount > 0)
                {
                    var initialLessons = Enumerable.Range(1, lessonCount)
                        .Select(idx => new Lesson
                        {
                            CourseId = course.Id,
                            Title = $"Bài {idx}: Chuyên đề bài học {idx}",
                            OrderIndex = idx,
                            ContentType = "vocabulary",
                            Content = $"Nội dung bài {idx} thuộc khóa học {course.Title}.",
                            CreatedAt = now,
                            UpdatedAt = now
                        })
                        .ToList();

                    _context.Lessons.AddRange(initialLessons);
                    await _context.SaveChangesAsync(cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);

                return Ok(new
                {
                    success = true,
                    message = "Tạo khóa học mới thành công.",
                    id = course.Id
                });
            }
            catch (OperationCanceledException)
            {
                return this.ApiCancelled();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể tạo khóa học mới.");
                await transaction.RollbackAsync(CancellationToken.None);
                return this.ApiServerError("Không thể tạo khóa học mới.");
            }
        }

        [HttpGet("/Admin/api/Courses/Detail/{id}")]
        public async Task<IActionResult> DetailApi(int id, CancellationToken cancellationToken)
        {
            if (id <= 0)
            {
                return this.ApiBadRequest("Mã khóa học không hợp lệ.");
            }

            try
            {
                var course = await (
                    from c in _context.Courses.AsNoTracking()
                    join s in _context.Subjects.AsNoTracking() on c.SubjectId equals s.Id into subjGroup
                    from subj in subjGroup.DefaultIfEmpty()
                    where c.Id == id
                    select new
                    {
                        id = c.Id,
                        code = $"COURSE-{c.Id:D3}",
                        subjectId = c.SubjectId,
                        subjectName = subj != null ? subj.Name : "Chưa phân môn",
                        title = c.Title,
                        description = c.Description,
                        thumbnailUrl = c.ThumbnailUrl,
                        level = c.Level ?? "beginner",
                        price = c.Price,
                        isPublished = c.IsPublished,
                        createdAt = c.CreatedAt,
                        updatedAt = c.UpdatedAt
                    }
                ).FirstOrDefaultAsync(cancellationToken);

                if (course == null)
                {
                    return this.ApiNotFound("Không tìm thấy khóa học yêu cầu.");
                }

                var lessons = await _context.Lessons
                    .AsNoTracking()
                    .Where(l => l.CourseId == id)
                    .OrderBy(l => l.OrderIndex)
                    .ThenBy(l => l.Id)
                    .Select(l => new
                    {
                        id = l.Id,
                        courseId = l.CourseId,
                        title = l.Title,
                        orderIndex = l.OrderIndex,
                        contentType = l.ContentType,
                        content = l.Content,
                        createdAt = l.CreatedAt,
                        updatedAt = l.UpdatedAt
                    })
                    .ToListAsync(cancellationToken);

                var openClassCount = await _context.CourseClasses
                    .AsNoTracking()
                    .CountAsync(cc => cc.CourseId == id && (cc.Status == "upcoming" || cc.Status == "ongoing"), cancellationToken);

                var totalClassCount = await _context.CourseClasses
                    .AsNoTracking()
                    .CountAsync(cc => cc.CourseId == id, cancellationToken);

                var activeStudentCount = await (
                    from e in _context.Enrollments.AsNoTracking()
                    join cc in _context.CourseClasses.AsNoTracking() on e.ClassId equals cc.Id
                    where cc.CourseId == id && e.Status == "active"
                    select e.UserId
                ).Distinct().CountAsync(cancellationToken);

                var examCount = await _context.Exams
                    .AsNoTracking()
                    .CountAsync(e => e.CourseId == id, cancellationToken);

                var subjects = await _context.Subjects
                    .AsNoTracking()
                    .OrderBy(s => s.Name)
                    .Select(s => new { id = s.Id, name = s.Name, isActive = s.IsActive })
                    .ToListAsync(cancellationToken);

                return Ok(new
                {
                    success = true,
                    course = new
                    {
                        course.id,
                        course.code,
                        course.subjectId,
                        course.subjectName,
                        course.title,
                        course.description,
                        course.thumbnailUrl,
                        course.level,
                        course.price,
                        course.isPublished,
                        course.createdAt,
                        course.updatedAt,
                        lessonCount = lessons.Count,
                        openClassCount,
                        totalClassCount,
                        activeStudentCount,
                        examCount
                    },
                    lessons,
                    subjects
                });
            }
            catch (OperationCanceledException)
            {
                return this.ApiCancelled();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể tải chi tiết khóa học {CourseId}.", id);
                return this.ApiServerError("Không thể tải chi tiết khóa học.");
            }
        }

        [HttpPost("/Admin/api/Courses/Update/{id}")]
        [HttpPut("/Admin/api/Courses/Update/{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] CourseUpsertDto? dto, CancellationToken cancellationToken)
        {
            if (id <= 0)
            {
                return this.ApiBadRequest("Mã khóa học không hợp lệ.");
            }

            var error = NormalizeAndValidateCourse(dto, out var title, out var level, out var description, out var thumbnailUrl);
            if (error != null)
            {
                return this.ApiBadRequest(error);
            }

            try
            {
                var course = await _context.Courses.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
                if (course == null)
                {
                    return this.ApiNotFound("Không tìm thấy khóa học.");
                }

                var subjectExists = await _context.Subjects
                    .AsNoTracking()
                    .AnyAsync(s => s.Id == dto!.SubjectId, cancellationToken);
                if (!subjectExists)
                {
                    return this.ApiBadRequest("Môn học được chọn không tồn tại.");
                }

                var duplicateTitle = await _context.Courses
                    .AsNoTracking()
                    .AnyAsync(c => c.Id != id && c.SubjectId == dto!.SubjectId && c.Title.ToLower() == title.ToLower(), cancellationToken);
                if (duplicateTitle)
                {
                    return this.ApiBadRequest("Môn học này đã có khóa học khác trùng tên.");
                }

                course.SubjectId = dto!.SubjectId;
                course.Title = title;
                course.Description = description;
                course.ThumbnailUrl = thumbnailUrl;
                course.Level = level;
                course.Price = dto.Price;
                if (dto.IsPublished.HasValue)
                {
                    course.IsPublished = dto.IsPublished.Value;
                }
                course.UpdatedAt = DateTime.UtcNow;

                try
                {
                    await _context.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
                {
                    return this.ApiBadRequest("Tên khóa học đã tồn tại trong môn học này.");
                }

                return Ok(new
                {
                    success = true,
                    message = "Cập nhật thông tin khóa học thành công.",
                    id = course.Id
                });
            }
            catch (OperationCanceledException)
            {
                return this.ApiCancelled();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể cập nhật khóa học {CourseId}.", id);
                return this.ApiServerError("Không thể cập nhật khóa học.");
            }
        }

        [HttpPost("/Admin/api/Courses/{courseId}/Lessons/Create")]
        public async Task<IActionResult> CreateLesson(int courseId, [FromBody] LessonUpsertDto? dto, CancellationToken cancellationToken)
        {
            if (courseId <= 0)
            {
                return this.ApiBadRequest("Mã khóa học không hợp lệ.");
            }

            var error = ValidateLesson(dto, out var title, out var contentType, out var content);
            if (error != null)
            {
                return this.ApiBadRequest(error);
            }

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                long lockKey = 400_000_000L + courseId;
                await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({lockKey})", cancellationToken);

                var courseExists = await _context.Courses
                    .AsNoTracking()
                    .AnyAsync(c => c.Id == courseId, cancellationToken);
                if (!courseExists)
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiNotFound("Không tìm thấy khóa học.");
                }

                var nextOrder = dto!.OrderIndex.HasValue && dto.OrderIndex.Value > 0
                    ? dto.OrderIndex.Value
                    : ((await _context.Lessons.AsNoTracking()
                        .Where(l => l.CourseId == courseId)
                        .Select(l => (int?)l.OrderIndex)
                        .MaxAsync(cancellationToken)) ?? 0) + 1;

                var now = DateTime.UtcNow;
                var lesson = new Lesson
                {
                    CourseId = courseId,
                    Title = title,
                    OrderIndex = nextOrder,
                    ContentType = contentType,
                    Content = content,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                _context.Lessons.Add(lesson);
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return Ok(new { success = true, message = "Thêm bài học vào đề cương thành công.", id = lesson.Id });
            }
            catch (OperationCanceledException)
            {
                return this.ApiCancelled();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể thêm bài học cho khóa {CourseId}.", courseId);
                await transaction.RollbackAsync(CancellationToken.None);
                return this.ApiServerError("Không thể thêm bài học vào đề cương.");
            }
        }

        [HttpPost("/Admin/api/Courses/Lessons/Update/{lessonId}")]
        [HttpPut("/Admin/api/Courses/Lessons/Update/{lessonId}")]
        public async Task<IActionResult> UpdateLesson(int lessonId, [FromBody] LessonUpsertDto? dto, CancellationToken cancellationToken)
        {
            if (lessonId <= 0)
            {
                return this.ApiBadRequest("Mã bài học không hợp lệ.");
            }

            var error = ValidateLesson(dto, out var title, out var contentType, out var content);
            if (error != null)
            {
                return this.ApiBadRequest(error);
            }

            try
            {
                var lesson = await _context.Lessons.FirstOrDefaultAsync(l => l.Id == lessonId, cancellationToken);
                if (lesson == null)
                {
                    return this.ApiNotFound("Không tìm thấy bài học.");
                }

                lesson.Title = title;
                if (dto!.OrderIndex.HasValue && dto.OrderIndex.Value > 0)
                {
                    lesson.OrderIndex = dto.OrderIndex.Value;
                }
                lesson.ContentType = contentType;
                lesson.Content = content;
                lesson.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync(cancellationToken);

                return Ok(new { success = true, message = "Cập nhật bài học thành công.", id = lesson.Id });
            }
            catch (OperationCanceledException)
            {
                return this.ApiCancelled();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể cập nhật bài học {LessonId}.", lessonId);
                return this.ApiServerError("Không thể cập nhật bài học.");
            }
        }

        [HttpPost("/Admin/api/Courses/Lessons/Delete/{lessonId}")]
        [HttpDelete("/Admin/api/Courses/Lessons/Delete/{lessonId}")]
        public async Task<IActionResult> DeleteLesson(int lessonId, CancellationToken cancellationToken)
        {
            if (lessonId <= 0)
            {
                return this.ApiBadRequest("Mã bài học không hợp lệ.");
            }

            try
            {
                var lesson = await _context.Lessons.FirstOrDefaultAsync(l => l.Id == lessonId, cancellationToken);
                if (lesson == null)
                {
                    return this.ApiNotFound("Không tìm thấy bài học.");
                }

                var inUse = await _context.ClassLessons.AsNoTracking().AnyAsync(cl => cl.LessonId == lessonId, cancellationToken)
                    || await _context.Questions.AsNoTracking().AnyAsync(q => q.LessonId == lessonId, cancellationToken)
                    || await _context.UserProgresses.AsNoTracking().AnyAsync(up => up.LessonId == lessonId, cancellationToken);

                if (inUse)
                {
                    return this.ApiBadRequest("Không thể xóa bài học đã gắn lịch dạy, câu hỏi hoặc tiến độ học viên.");
                }

                _context.Lessons.Remove(lesson);
                await _context.SaveChangesAsync(cancellationToken);

                return Ok(new { success = true, message = "Đã xóa bài học khỏi đề cương." });
            }
            catch (OperationCanceledException)
            {
                return this.ApiCancelled();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể xóa bài học {LessonId}.", lessonId);
                return this.ApiServerError("Không thể xóa bài học.");
            }
        }

        private static string? NormalizeAndValidateCourse(
            CourseUpsertDto? dto,
            out string title,
            out string level,
            out string? description,
            out string? thumbnailUrl)
        {
            title = string.Empty;
            level = "beginner";
            description = null;
            thumbnailUrl = null;

            if (dto == null)
            {
                return "Dữ liệu gửi lên không hợp lệ.";
            }

            if (dto.SubjectId <= 0)
            {
                return "Vui lòng chọn môn học trực thuộc.";
            }

            title = (dto.Title ?? string.Empty).Trim();
            level = (dto.Level ?? "beginner").Trim().ToLowerInvariant();
            description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
            thumbnailUrl = string.IsNullOrWhiteSpace(dto.ThumbnailUrl) ? null : dto.ThumbnailUrl.Trim();

            if (title.Length == 0 || title.Length > MaxTitleLength)
            {
                return $"Tên khóa học không hợp lệ (1 - {MaxTitleLength} ký tự).";
            }

            if (!ValidLevels.Contains(level))
            {
                return "Cấp độ khóa học phải là beginner, intermediate hoặc advanced.";
            }

            if (dto.Price < 0 || dto.Price > MaxPrice)
            {
                return $"Học phí phải nằm trong khoảng 0 - {MaxPrice:N0} ₫.";
            }

            if (description != null && description.Length > MaxDescriptionLength)
            {
                return $"Mô tả và mục tiêu khóa học không được vượt quá {MaxDescriptionLength} ký tự.";
            }

            return null;
        }

        private static string? ValidateLesson(
            LessonUpsertDto? dto,
            out string title,
            out string contentType,
            out string? content)
        {
            title = string.Empty;
            contentType = "vocabulary";
            content = null;

            if (dto == null)
            {
                return "Dữ liệu bài học không hợp lệ.";
            }

            title = (dto.Title ?? string.Empty).Trim();
            contentType = (dto.ContentType ?? "vocabulary").Trim().ToLowerInvariant();
            content = string.IsNullOrWhiteSpace(dto.Content) ? null : dto.Content.Trim();

            if (title.Length == 0 || title.Length > MaxTitleLength)
            {
                return $"Tiêu đề bài học không hợp lệ (1 - {MaxTitleLength} ký tự).";
            }

            if (!ValidLessonTypes.Contains(contentType))
            {
                return "Loại nội dung bài học không hợp lệ.";
            }

            if (dto.OrderIndex.HasValue && (dto.OrderIndex.Value < 1 || dto.OrderIndex.Value > 1000))
            {
                return "Thứ tự bài học phải từ 1 đến 1000.";
            }

            if (content != null && content.Length > MaxLessonContentLength)
            {
                return $"Nội dung tóm tắt bài học không được vượt quá {MaxLessonContentLength} ký tự.";
            }

            return null;
        }

        public class CourseUpsertDto
        {
            public int SubjectId { get; set; }
            public string? Title { get; set; }
            public string? Level { get; set; }
            public decimal Price { get; set; }
            public int? LessonCount { get; set; }
            public string? Description { get; set; }
            public string? ThumbnailUrl { get; set; }
            public bool? IsPublished { get; set; }
        }

        public class LessonUpsertDto
        {
            public string? Title { get; set; }
            public int? OrderIndex { get; set; }
            public string? ContentType { get; set; }
            public string? Content { get; set; }
        }
    }
}
