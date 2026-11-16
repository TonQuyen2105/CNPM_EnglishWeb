using CNPM_EnglishLearning.Data;
using CNPM_EnglishLearning.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CNPM_EnglishLearning.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ExamsController : Controller
    {
        private const int MaxTitleLength = 200;
        private const int MaxDescriptionLength = 2000;
        private const int MaxDurationMinutes = 600;

        private readonly ApplicationDbContext _context;
        private readonly ILogger<ExamsController> _logger;

        public ExamsController(ApplicationDbContext context, ILogger<ExamsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: /Admin/Exams
        public IActionResult Index()
        {
            return View("~/Areas/Admin/Views/exams.html");
        }

        [HttpGet("/Admin/api/Exams/List")]
        public async Task<IActionResult> List(
            string? search,
            int? courseId,
            string? published,
            CancellationToken cancellationToken,
            bool lookups = true)
        {
            try
            {
                var query =
                    from e in _context.Exams.AsNoTracking()
                    join c in _context.Courses.AsNoTracking() on e.CourseId equals c.Id into courseGroup
                    from course in courseGroup.DefaultIfEmpty()
                    join s in _context.Subjects.AsNoTracking() on course.SubjectId equals s.Id into subjGroup
                    from subj in subjGroup.DefaultIfEmpty()
                    join t in _context.Teachers.AsNoTracking() on e.CreatedByTeacherId equals t.Id into teacherGroup
                    from teacher in teacherGroup.DefaultIfEmpty()
                    join u in _context.Users.AsNoTracking() on teacher.UserId equals u.Id into userGroup
                    from teacherUser in userGroup.DefaultIfEmpty()
                    select new
                    {
                        id = e.Id,
                        title = e.Title,
                        description = e.Description,
                        courseId = e.CourseId,
                        courseTitle = course != null ? course.Title : "Đánh giá chung",
                        subjectName = subj != null ? subj.Name : "Chung",
                        createdByTeacherId = e.CreatedByTeacherId,
                        teacherName = teacherUser != null ? teacherUser.FullName : "Quản trị viên",
                        durationMinutes = e.DurationMinutes,
                        passScore = e.PassScore,
                        maxAttempts = e.MaxAttempts,
                        isPublished = e.IsPublished,
                        createdAt = e.CreatedAt,
                        updatedAt = e.UpdatedAt,
                        questionCount = _context.ExamQuestions.Count(eq => eq.ExamId == e.Id),
                        attemptCount = _context.UserExams.Count(ue => ue.ExamId == e.Id)
                    };

                if (courseId.HasValue && courseId.Value > 0)
                {
                    query = query.Where(x => x.courseId == courseId.Value);
                }

                if (!string.IsNullOrWhiteSpace(published) && !published.Equals("all", StringComparison.OrdinalIgnoreCase))
                {
                    var norm = published.Trim().ToLowerInvariant();
                    if (norm is "1" or "true" or "published")
                    {
                        query = query.Where(x => x.isPublished);
                    }
                    else if (norm is "0" or "false" or "draft")
                    {
                        query = query.Where(x => !x.isPublished);
                    }
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var term = search.Trim().ToLower();
                    query = query.Where(x =>
                        x.title.ToLower().Contains(term) ||
                        x.courseTitle.ToLower().Contains(term) ||
                        x.subjectName.ToLower().Contains(term) ||
                        x.teacherName.ToLower().Contains(term));
                }

                var items = await query
                    .OrderByDescending(x => x.id)
                    .ToListAsync(cancellationToken);

                var courses = lookups
                    ? await (
                        from c in _context.Courses.AsNoTracking()
                        join s in _context.Subjects.AsNoTracking() on c.SubjectId equals s.Id into sg
                        from subj in sg.DefaultIfEmpty()
                        orderby c.Title
                        select new
                        {
                            id = c.Id,
                            title = c.Title,
                            subjectName = subj != null ? subj.Name : null
                        }
                    ).ToListAsync(cancellationToken)
                    : null;

                var teachers = lookups
                    ? await (
                        from t in _context.Teachers.AsNoTracking()
                        join u in _context.Users.AsNoTracking() on t.UserId equals u.Id
                        orderby u.FullName
                        select new
                        {
                            id = t.Id,
                            fullName = u.FullName,
                            isVerified = t.IsVerified
                        }
                    ).ToListAsync(cancellationToken)
                    : null;

                return Ok(new { success = true, items, courses, teachers });
            }
            catch (OperationCanceledException)
            {
                return this.ApiCancelled();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể tải danh sách đề thi.");
                return this.ApiServerError("Không thể tải danh sách đề thi.");
            }
        }

        [HttpPost("/Admin/api/Exams/Create")]
        public async Task<IActionResult> Create([FromBody] ExamUpsertDto? dto, CancellationToken cancellationToken)
        {
            var error = ValidateExam(dto, out var title, out var description);
            if (error != null)
            {
                return this.ApiBadRequest(error);
            }

            try
            {
                if (dto!.CourseId.HasValue && dto.CourseId.Value > 0)
                {
                    var courseExists = await _context.Courses
                        .AsNoTracking()
                        .AnyAsync(c => c.Id == dto.CourseId.Value, cancellationToken);
                    if (!courseExists)
                    {
                        return this.ApiBadRequest("Khóa học được chọn không tồn tại.");
                    }
                }

                var teacherId = dto.CreatedByTeacherId;
                if (teacherId <= 0)
                {
                    teacherId = await _context.Teachers
                        .AsNoTracking()
                        .OrderByDescending(t => t.IsVerified)
                        .ThenBy(t => t.Id)
                        .Select(t => t.Id)
                        .FirstOrDefaultAsync(cancellationToken);
                }
                else
                {
                    var teacherExists = await _context.Teachers
                        .AsNoTracking()
                        .AnyAsync(t => t.Id == teacherId, cancellationToken);
                    if (!teacherExists)
                    {
                        return this.ApiBadRequest("Giảng viên phụ trách đề thi không tồn tại.");
                    }
                }

                if (teacherId <= 0)
                {
                    return this.ApiBadRequest("Hệ thống chưa có hồ sơ giảng viên nào để gắn người tạo đề.");
                }

                var now = DateTime.UtcNow;
                var exam = new Exam
                {
                    Title = title,
                    CourseId = dto.CourseId.HasValue && dto.CourseId.Value > 0 ? dto.CourseId.Value : null,
                    CreatedByTeacherId = teacherId,
                    Description = description,
                    DurationMinutes = dto.DurationMinutes,
                    PassScore = dto.PassScore,
                    MaxAttempts = dto.MaxAttempts.HasValue && dto.MaxAttempts.Value > 0 ? dto.MaxAttempts.Value : null,
                    IsPublished = dto.IsPublished ?? false,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                _context.Exams.Add(exam);
                await _context.SaveChangesAsync(cancellationToken);

                return Ok(new
                {
                    success = true,
                    message = exam.IsPublished ? "Đã tạo và công bố đề thi thành công." : "Đã lưu bản nháp đề thi thành công.",
                    id = exam.Id
                });
            }
            catch (OperationCanceledException)
            {
                return this.ApiCancelled();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể tạo đề thi mới.");
                return this.ApiServerError("Không thể tạo đề thi mới.");
            }
        }

        [HttpPost("/Admin/api/Exams/Update/{id}")]
        [HttpPut("/Admin/api/Exams/Update/{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] ExamUpsertDto? dto, CancellationToken cancellationToken)
        {
            if (id <= 0)
            {
                return this.ApiBadRequest("Mã đề thi không hợp lệ.");
            }

            var error = ValidateExam(dto, out var title, out var description);
            if (error != null)
            {
                return this.ApiBadRequest(error);
            }

            try
            {
                var exam = await _context.Exams.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
                if (exam == null)
                {
                    return this.ApiNotFound("Không tìm thấy đề thi.");
                }

                if (dto!.CourseId.HasValue && dto.CourseId.Value > 0)
                {
                    var courseExists = await _context.Courses
                        .AsNoTracking()
                        .AnyAsync(c => c.Id == dto.CourseId.Value, cancellationToken);
                    if (!courseExists)
                    {
                        return this.ApiBadRequest("Khóa học được chọn không tồn tại.");
                    }
                }

                if (dto.CreatedByTeacherId > 0)
                {
                    var teacherExists = await _context.Teachers
                        .AsNoTracking()
                        .AnyAsync(t => t.Id == dto.CreatedByTeacherId, cancellationToken);
                    if (!teacherExists)
                    {
                        return this.ApiBadRequest("Giảng viên phụ trách không tồn tại.");
                    }
                    exam.CreatedByTeacherId = dto.CreatedByTeacherId;
                }

                exam.Title = title;
                exam.CourseId = dto.CourseId.HasValue && dto.CourseId.Value > 0 ? dto.CourseId.Value : null;
                exam.Description = description;
                exam.DurationMinutes = dto.DurationMinutes;
                exam.PassScore = dto.PassScore;
                exam.MaxAttempts = dto.MaxAttempts.HasValue && dto.MaxAttempts.Value > 0 ? dto.MaxAttempts.Value : null;
                if (dto.IsPublished.HasValue)
                {
                    exam.IsPublished = dto.IsPublished.Value;
                }
                exam.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync(cancellationToken);

                return Ok(new
                {
                    success = true,
                    message = "Cập nhật đề thi thành công.",
                    id = exam.Id
                });
            }
            catch (OperationCanceledException)
            {
                return this.ApiCancelled();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể cập nhật đề thi {ExamId}.", id);
                return this.ApiServerError("Không thể cập nhật đề thi.");
            }
        }

        [HttpPost("/Admin/api/Exams/TogglePublish/{id}")]
        public async Task<IActionResult> TogglePublish(int id, CancellationToken cancellationToken)
        {
            if (id <= 0)
            {
                return this.ApiBadRequest("Mã đề thi không hợp lệ.");
            }

            try
            {
                var exam = await _context.Exams.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
                if (exam == null)
                {
                    return this.ApiNotFound("Không tìm thấy đề thi.");
                }

                exam.IsPublished = !exam.IsPublished;
                exam.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);

                return Ok(new
                {
                    success = true,
                    message = exam.IsPublished ? $"Đã công bố đề thi \"{exam.Title}\"." : $"Đã chuyển đề thi \"{exam.Title}\" về bản nháp.",
                    id = exam.Id,
                    isPublished = exam.IsPublished
                });
            }
            catch (OperationCanceledException)
            {
                return this.ApiCancelled();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể đổi trạng thái công bố đề thi {ExamId}.", id);
                return this.ApiServerError("Không thể đổi trạng thái công bố đề thi.");
            }
        }

        private static string? ValidateExam(ExamUpsertDto? dto, out string title, out string? description)
        {
            title = string.Empty;
            description = null;

            if (dto == null)
            {
                return "Dữ liệu gửi lên không hợp lệ.";
            }

            title = (dto.Title ?? string.Empty).Trim();
            description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();

            if (title.Length == 0 || title.Length > MaxTitleLength)
            {
                return $"Tiêu đề bài thi không hợp lệ (1 - {MaxTitleLength} ký tự).";
            }

            if (description != null && description.Length > MaxDescriptionLength)
            {
                return $"Mô tả bài thi không được vượt quá {MaxDescriptionLength} ký tự.";
            }

            if (dto.DurationMinutes < 1 || dto.DurationMinutes > MaxDurationMinutes)
            {
                return $"Thời gian làm bài phải từ 1 đến {MaxDurationMinutes} phút.";
            }

            if (dto.PassScore < 0 || dto.PassScore > 10000)
            {
                return "Điểm qua không hợp lệ (0 - 10.000).";
            }

            if (dto.MaxAttempts.HasValue && dto.MaxAttempts.Value < 1)
            {
                return "Số lượt thi tối đa phải >= 1 (hoặc để trống nếu không giới hạn).";
            }

            return null;
        }

        public class ExamUpsertDto
        {
            public string? Title { get; set; }
            public int? CourseId { get; set; }
            public int CreatedByTeacherId { get; set; }
            public string? Description { get; set; }
            public int DurationMinutes { get; set; } = 45;
            public int PassScore { get; set; } = 50;
            public int? MaxAttempts { get; set; }
            public bool? IsPublished { get; set; }
        }
    }
}
