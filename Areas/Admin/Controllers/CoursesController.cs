using System.Data;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CNPM_EnglishLearning.Data;
using CNPM_EnglishLearning.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;

namespace CNPM_EnglishLearning.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class CoursesController : Controller
    {
        private const int MaxTitleLength = 200;
        private const int MaxDescriptionLength = 4000;
        private const decimal MaxPrice = 99_999_999.99m;
        private const string LessonMediaPrefix = "EL_LESSON_MEDIA_V1";
        private const string DefaultSupabaseUrl = "https://wymdewnklhrnmirbvgxi.supabase.co";
        private const string DefaultStorageBucket = "Lesson_media";
        private static readonly HttpClient StorageHttpClient = new() { Timeout = TimeSpan.FromSeconds(10) };

        private static readonly HashSet<string> ValidLevels = new(StringComparer.OrdinalIgnoreCase)
        {
            "beginner", "intermediate", "advanced"
        };

        private readonly ApplicationDbContext _context;
        private readonly ILogger<CoursesController> _logger;
        private readonly IConfiguration? _configuration;

        public CoursesController(ApplicationDbContext context, ILogger<CoursesController> logger, IConfiguration? configuration = null)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
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
                        subjectName = subj != null ? subj.Name : "Chưa phân khóa",
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

                var rawItems = await query
                    .OrderByDescending(x => x.id)
                    .ToListAsync(cancellationToken);

                var allCoursesForTotals = await _context.Courses
                    .AsNoTracking()
                    .OrderBy(c => c.Id)
                    .Select(c => new
                    {
                        c.Id,
                        c.SubjectId,
                        c.Title,
                        c.Price,
                        c.Level,
                        c.IsPublished
                    })
                    .ToListAsync(cancellationToken);

                var coursesBySubject = allCoursesForTotals
                    .GroupBy(c => c.SubjectId)
                    .ToDictionary(g => g.Key, g => g.ToList());

                var items = rawItems.Select(x =>
                {
                    var subjCourses = coursesBySubject.TryGetValue(x.subjectId, out var cList) ? cList : [];
                    var subjectTotalPrice = subjCourses.Sum(c => c.Price);
                    return new
                    {
                        x.id,
                        x.subjectId,
                        x.subjectName,
                        x.title,
                        x.description,
                        x.thumbnailUrl,
                        x.level,
                        x.price,
                        subjectTotalPrice,
                        subjectCourseCount = subjCourses.Count,
                        x.isPublished,
                        x.createdAt,
                        x.updatedAt,
                        x.lessonCount,
                        x.classCount
                    };
                }).ToList();

                var rawSubjects = await _context.Subjects
                    .AsNoTracking()
                    .OrderBy(s => s.Name)
                    .Select(s => new
                    {
                        s.Id,
                        s.Name,
                        s.IsActive
                    })
                    .ToListAsync(cancellationToken);

                var subjects = rawSubjects.Select(s =>
                {
                    var subjCourses = coursesBySubject.TryGetValue(s.Id, out var cList) ? cList : [];
                    return new
                    {
                        id = s.Id,
                        name = s.Name,
                        isActive = s.IsActive,
                        courseCount = subjCourses.Count,
                        totalPrice = subjCourses.Sum(c => c.Price),
                        courses = subjCourses.Select(c => new
                        {
                            id = c.Id,
                            title = c.Title,
                            price = c.Price,
                            level = c.Level,
                            isPublished = c.IsPublished
                        }).ToList()
                    };
                }).ToList();

                return Ok(new { success = true, items, subjects });
            }
            catch (OperationCanceledException)
            {
                return this.ApiCancelled();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể tải danh sách môn học.");
                return this.ApiServerError("Không thể tải danh sách môn học.");
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

            var targetDto = dto!;

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({400_000_000L})", cancellationToken);

                var subjectExists = await _context.Subjects
                    .AsNoTracking()
                    .AnyAsync(s => s.Id == targetDto.SubjectId, cancellationToken);
                if (!subjectExists)
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiBadRequest("Khóa học được chọn không tồn tại.");
                }

                var duplicateTitle = await _context.Courses
                    .AsNoTracking()
                    .AnyAsync(c => c.SubjectId == targetDto.SubjectId && c.Title.ToLower() == title.ToLower(), cancellationToken);
                if (duplicateTitle)
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiBadRequest("Khóa học này đã có môn học cùng tên.");
                }

                var now = DateTime.UtcNow;
                var course = new Course
                {
                    SubjectId = targetDto.SubjectId,
                    Title = title,
                    Description = description,
                    ThumbnailUrl = thumbnailUrl,
                    Level = level,
                    Price = targetDto.Price,
                    IsPublished = targetDto.IsPublished ?? true,
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
                    return this.ApiBadRequest("Môn học này đã tồn tại trong hệ thống.");
                }
                catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation })
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiBadRequest("Khóa học được chọn không tồn tại.");
                }

                await transaction.CommitAsync(cancellationToken);

                return Ok(new
                {
                    success = true,
                    message = "Tạo môn học mới thành công.",
                    id = course.Id
                });
            }
            catch (OperationCanceledException)
            {
                return this.ApiCancelled();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể tạo môn học mới.");
                await transaction.RollbackAsync(CancellationToken.None);
                return this.ApiServerError("Không thể tạo môn học mới.");
            }
        }

        [HttpGet("/Admin/api/Courses/Detail/{id}")]
        public async Task<IActionResult> DetailApi(int id, CancellationToken cancellationToken)
        {
            if (id <= 0)
            {
                return this.ApiBadRequest("Mã môn học không hợp lệ.");
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
                        subjectName = subj != null ? subj.Name : "Chưa phân khóa",
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
                    return this.ApiNotFound("Không tìm thấy môn học yêu cầu.");
                }

                var rawLessons = await _context.Lessons
                    .AsNoTracking()
                    .Where(l => l.CourseId == id)
                    .OrderBy(l => l.OrderIndex)
                    .ThenBy(l => l.Id)
                    .Select(l => new
                    {
                        l.Id,
                        l.CourseId,
                        l.Title,
                        l.OrderIndex,
                        l.ContentType,
                        l.Content,
                        l.CreatedAt,
                        l.UpdatedAt
                    })
                    .ToListAsync(cancellationToken);

                var parsedLessons = rawLessons
                    .Select(l => new { Row = l, Media = ParseRawLessonMedia(l.Content) })
                    .ToList();

                var storageLookup = await ResolveStorageObjectsAsync(
                    parsedLessons.Select(x => x.Media).ToList(),
                    cancellationToken);

                var supabaseBaseUrl = ResolveSupabaseBaseUrl();
                var serviceRoleKey = ResolveServiceRoleKey();

                var lessons = new List<object>(parsedLessons.Count);
                foreach (var x in parsedLessons)
                {
                    var pdfUrl = await ResolveAccessibleMediaUrlAsync(
                        x.Media.Pdf, storageLookup, supabaseBaseUrl, serviceRoleKey, cancellationToken);
                    var videoUrl = await ResolveAccessibleMediaUrlAsync(
                        x.Media.Video, storageLookup, supabaseBaseUrl, serviceRoleKey, cancellationToken);

                    lessons.Add(new
                    {
                        id = x.Row.Id,
                        courseId = x.Row.CourseId,
                        title = x.Row.Title,
                        orderIndex = x.Row.OrderIndex,
                        contentType = x.Row.ContentType,
                        content = x.Row.Content,
                        media = new
                        {
                            isStructured = x.Media.IsStructured,
                            text = x.Media.Text,
                            pdfUrl = pdfUrl.Url,
                            pdfName = x.Media.Pdf?.FileName,
                            pdfBucket = pdfUrl.Bucket,
                            videoUrl = videoUrl.Url,
                            videoName = x.Media.Video?.FileName,
                            videoBucket = videoUrl.Bucket,
                            requiresPublicBucket = pdfUrl.RequiresPublicBucket || videoUrl.RequiresPublicBucket
                        },
                        createdAt = x.Row.CreatedAt,
                        updatedAt = x.Row.UpdatedAt
                    });
                }

                var openClassCount = await _context.CourseClasses
                    .AsNoTracking()
                    .CountAsync(cc => cc.CourseId == id && (cc.Status == "upcoming" || cc.Status == "ongoing"), cancellationToken);

                var totalClassCount = await _context.CourseClasses
                    .AsNoTracking()
                    .CountAsync(cc => cc.CourseId == id && cc.Status != "cancelled", cancellationToken);

                var activeStudentCount = await (
                    from e in _context.Enrollments.AsNoTracking()
                    join cc in _context.CourseClasses.AsNoTracking() on e.ClassId equals cc.Id
                    where cc.CourseId == id && e.Status == "active" && cc.Status != "cancelled"
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
                _logger.LogError(ex, "Không thể tải chi tiết môn học {CourseId}.", id);
                return this.ApiServerError("Không thể tải chi tiết môn học.");
            }
        }

        [HttpPost("/Admin/api/Courses/Update/{id}")]
        [HttpPut("/Admin/api/Courses/Update/{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] CourseUpsertDto? dto, CancellationToken cancellationToken)
        {
            if (id <= 0)
            {
                return this.ApiBadRequest("Mã môn học không hợp lệ.");
            }

            var error = NormalizeAndValidateCourse(dto, out var title, out var level, out var description, out var thumbnailUrl);
            if (error != null)
            {
                return this.ApiBadRequest(error);
            }

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({400_000_000L})", cancellationToken);

                var course = await _context.Courses.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
                if (course == null)
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiNotFound("Không tìm thấy môn học.");
                }

                var subjectExists = await _context.Subjects
                    .AsNoTracking()
                    .AnyAsync(s => s.Id == dto!.SubjectId, cancellationToken);
                if (!subjectExists)
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiBadRequest("Khóa học được chọn không tồn tại.");
                }

                var duplicateTitle = await _context.Courses
                    .AsNoTracking()
                    .AnyAsync(c => c.Id != id && c.SubjectId == dto!.SubjectId && c.Title.ToLower() == title.ToLower(), cancellationToken);
                if (duplicateTitle)
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiBadRequest("Khóa học này đã có môn học khác trùng tên.");
                }

                course.SubjectId = dto!.SubjectId;
                course.Title = title;
                course.Description = description;
                if (dto.ThumbnailUrl != null)
                {
                    course.ThumbnailUrl = thumbnailUrl;
                }
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
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiBadRequest("Tên môn học đã tồn tại trong khóa học này.");
                }
                catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation })
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiBadRequest("Khóa học được chọn không tồn tại.");
                }

                await transaction.CommitAsync(cancellationToken);

                return Ok(new
                {
                    success = true,
                    message = "Cập nhật thông tin môn học thành công.",
                    id = course.Id
                });
            }
            catch (OperationCanceledException)
            {
                return this.ApiCancelled();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể cập nhật môn học {CourseId}.", id);
                await transaction.RollbackAsync(CancellationToken.None);
                return this.ApiServerError("Không thể cập nhật môn học.");
            }
        }

        [HttpPost("/Admin/api/Courses/TogglePublish/{id}")]
        public async Task<IActionResult> TogglePublish(int id, CancellationToken cancellationToken)
        {
            if (id <= 0)
            {
                return this.ApiBadRequest("Mã môn học không hợp lệ.");
            }

            try
            {
                var course = await _context.Courses.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
                if (course == null)
                {
                    return this.ApiNotFound("Không tìm thấy môn học.");
                }

                course.IsPublished = !course.IsPublished;
                course.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);

                return Ok(new
                {
                    success = true,
                    message = course.IsPublished
                        ? $"Đã mở xuất bản môn học \"{course.Title}\"."
                        : $"Đã chuyển môn học \"{course.Title}\" về bản nháp.",
                    id = course.Id,
                    isPublished = course.IsPublished
                });
            }
            catch (OperationCanceledException)
            {
                return this.ApiCancelled();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể đổi trạng thái xuất bản môn học {CourseId}.", id);
                return this.ApiServerError("Không thể đổi trạng thái xuất bản môn học.");
            }
        }

        [HttpPost("/Admin/api/Courses/{courseId}/Lessons/Create")]
        public IActionResult CreateLesson(int courseId, [FromBody] LessonUpsertDto? dto)
            => AdminLessonReadOnlyForbidden();

        [HttpPost("/Admin/api/Courses/Lessons/Update/{lessonId}")]
        [HttpPut("/Admin/api/Courses/Lessons/Update/{lessonId}")]
        public IActionResult UpdateLesson(int lessonId, [FromBody] LessonUpsertDto? dto)
            => AdminLessonReadOnlyForbidden();

        [HttpPost("/Admin/api/Courses/Lessons/Delete/{lessonId}")]
        [HttpDelete("/Admin/api/Courses/Lessons/Delete/{lessonId}")]
        public IActionResult DeleteLesson(int lessonId)
            => AdminLessonReadOnlyForbidden();

        private IActionResult AdminLessonReadOnlyForbidden()
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                success = false,
                message = "Bài học thuộc môn học do giảng viên tạo và cập nhật. Quản trị viên chỉ có quyền xem nội dung bài học."
            });
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
                return "Vui lòng chọn khóa học trực thuộc.";
            }

            title = (dto.Title ?? string.Empty).Trim();
            level = (dto.Level ?? "beginner").Trim().ToLowerInvariant();
            description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
            thumbnailUrl = string.IsNullOrWhiteSpace(dto.ThumbnailUrl) ? null : dto.ThumbnailUrl.Trim();

            if (title.Length == 0 || title.Length > MaxTitleLength)
            {
                return $"Tên môn học không hợp lệ (1 - {MaxTitleLength} ký tự).";
            }

            if (!ValidLevels.Contains(level))
            {
                return "Cấp độ môn học phải là beginner, intermediate hoặc advanced.";
            }

            if (dto.Price < 0 || dto.Price > MaxPrice)
            {
                return $"Học phí phải nằm trong khoảng 0 - {MaxPrice:N0} ₫.";
            }

            if (description != null && description.Length > MaxDescriptionLength)
            {
                return $"Mô tả và mục tiêu môn học không được vượt quá {MaxDescriptionLength} ký tự.";
            }

            return null;
        }

        private string ResolveSupabaseBaseUrl()
        {
            var configuredUrl = _configuration?["Supabase:Url"];
            if (!string.IsNullOrWhiteSpace(configuredUrl))
            {
                return configuredUrl.Trim().TrimEnd('/');
            }

            var connStr = _configuration?.GetConnectionString("DefaultConnection") ?? string.Empty;
            const string marker = "Username=postgres.";
            var idx = connStr.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (idx >= 0)
            {
                var start = idx + marker.Length;
                var end = connStr.IndexOf(';', start);
                var projectRef = (end >= 0 ? connStr[start..end] : connStr[start..]).Trim();
                if (!string.IsNullOrEmpty(projectRef))
                {
                    return $"https://{projectRef}.supabase.co";
                }
            }

            return DefaultSupabaseUrl;
        }

        private string? ResolveServiceRoleKey()
        {
            var key = _configuration?["Supabase:ServiceRoleKey"]
                ?? _configuration?["Supabase:ServiceKey"]
                ?? _configuration?["Supabase:Key"]
                ?? Environment.GetEnvironmentVariable("SUPABASE_SERVICE_ROLE_KEY");
            return string.IsNullOrWhiteSpace(key) ? null : key.Trim();
        }

        private async Task<StorageLookupResult> ResolveStorageObjectsAsync(
            List<ParsedLessonMedia> mediaItems,
            CancellationToken cancellationToken)
        {
            var keys = mediaItems
                .SelectMany(m => new[] { m.Pdf?.Key, m.Video?.Key })
                .Where(k => !string.IsNullOrWhiteSpace(k))
                .Select(k => k!.Trim().TrimStart('/'))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var byKey = new Dictionary<string, (string Bucket, string Name)>(StringComparer.OrdinalIgnoreCase);
            var bucketPublic = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            string fallbackBucket = DefaultStorageBucket;

            try
            {
                var conn = _context.Database.GetDbConnection();
                var shouldClose = conn.State != ConnectionState.Open;
                if (shouldClose)
                {
                    await conn.OpenAsync(cancellationToken);
                }

                try
                {
                    if (conn is NpgsqlConnection npgsqlConn)
                    {
                        await using (var bucketCmd = new NpgsqlCommand(
                            "SELECT id, COALESCE(public, false) FROM storage.buckets ORDER BY COALESCE(public, false) DESC, created_at ASC",
                            npgsqlConn))
                        await using (var bucketReader = await bucketCmd.ExecuteReaderAsync(cancellationToken))
                        {
                            string? firstBucket = null;
                            while (await bucketReader.ReadAsync(cancellationToken))
                            {
                                var bId = bucketReader.IsDBNull(0) ? null : bucketReader.GetString(0)?.Trim();
                                var isPub = !bucketReader.IsDBNull(1) && bucketReader.GetBoolean(1);
                                if (!string.IsNullOrEmpty(bId))
                                {
                                    bucketPublic[bId] = isPub;
                                    firstBucket ??= bId;
                                }
                            }

                            if (!string.IsNullOrEmpty(firstBucket))
                            {
                                fallbackBucket = firstBucket;
                            }
                        }

                        if (keys.Length > 0)
                        {
                            await using var objCmd = new NpgsqlCommand(@"
                                SELECT o.id::text, o.bucket_id, o.name
                                FROM storage.objects o
                                WHERE NOT COALESCE(o.is_delete_marker, false)
                                  AND o.bucket_id IS NOT NULL
                                  AND o.name IS NOT NULL
                                  AND (o.name = ANY(@keys) OR o.id::text = ANY(@keys))", npgsqlConn);
                            objCmd.Parameters.Add(new NpgsqlParameter("keys", NpgsqlDbType.Array | NpgsqlDbType.Text) { Value = keys });

                            await using var reader = await objCmd.ExecuteReaderAsync(cancellationToken);
                            while (await reader.ReadAsync(cancellationToken))
                            {
                                var objId = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
                                var bucketId = reader.GetString(1);
                                var objName = reader.GetString(2);
                                if (!string.IsNullOrEmpty(objName))
                                {
                                    byKey.TryAdd(objName, (bucketId, objName));
                                }
                                if (!string.IsNullOrEmpty(objId))
                                {
                                    byKey.TryAdd(objId, (bucketId, objName));
                                }
                            }
                        }
                    }
                }
                finally
                {
                    if (shouldClose)
                    {
                        await conn.CloseAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Không thể tra cứu trực tiếp schema storage; dùng metadata từ lessons.content.");
            }

            return new StorageLookupResult(fallbackBucket, bucketPublic, byKey);
        }

        private async Task<ResolvedMediaUrl> ResolveAccessibleMediaUrlAsync(
            MediaRef? mediaRef,
            StorageLookupResult storageLookup,
            string supabaseBaseUrl,
            string? serviceRoleKey,
            CancellationToken cancellationToken)
        {
            if (mediaRef == null)
            {
                return new ResolvedMediaUrl(null, null, false);
            }

            if (!string.IsNullOrWhiteSpace(mediaRef.Url))
            {
                return new ResolvedMediaUrl(mediaRef.Url, mediaRef.Bucket, false);
            }

            var rawKey = mediaRef.Key?.Trim().TrimStart('/');
            if (string.IsNullOrEmpty(rawKey))
            {
                return new ResolvedMediaUrl(null, null, false);
            }

            if (rawKey.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                rawKey.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return new ResolvedMediaUrl(rawKey, mediaRef.Bucket, false);
            }

            string bucket;
            string objectPath;
            if (storageLookup.ByKey.TryGetValue(rawKey, out var found))
            {
                bucket = found.Bucket;
                objectPath = found.Name;
            }
            else
            {
                bucket = !string.IsNullOrWhiteSpace(mediaRef.Bucket)
                    ? mediaRef.Bucket.Trim()
                    : storageLookup.DefaultBucket;
                objectPath = rawKey.StartsWith(bucket + "/", StringComparison.OrdinalIgnoreCase)
                    ? rawKey[(bucket.Length + 1)..]
                    : rawKey;
            }

            var hasBucketInfo = storageLookup.BucketPublic.TryGetValue(bucket, out var isPublicBucket);
            if (hasBucketInfo && !isPublicBucket)
            {
                if (!string.IsNullOrWhiteSpace(serviceRoleKey))
                {
                    var signedUrl = await TryCreateSignedStorageUrlAsync(
                        supabaseBaseUrl, bucket, objectPath, serviceRoleKey, cancellationToken);
                    if (!string.IsNullOrEmpty(signedUrl))
                    {
                        return new ResolvedMediaUrl(signedUrl, bucket, false);
                    }
                }

                var fallbackPublicUrl = $"{supabaseBaseUrl}/storage/v1/object/public/{Uri.EscapeDataString(bucket)}/{EscapeStoragePath(objectPath)}";
                return new ResolvedMediaUrl(fallbackPublicUrl, bucket, true);
            }

            var publicUrl = $"{supabaseBaseUrl}/storage/v1/object/public/{Uri.EscapeDataString(bucket)}/{EscapeStoragePath(objectPath)}";
            return new ResolvedMediaUrl(publicUrl, bucket, false);
        }

        private async Task<string?> TryCreateSignedStorageUrlAsync(
            string supabaseBaseUrl,
            string bucket,
            string objectPath,
            string serviceRoleKey,
            CancellationToken cancellationToken)
        {
            try
            {
                var endpoint = $"{supabaseBaseUrl}/storage/v1/object/sign/{Uri.EscapeDataString(bucket)}/{EscapeStoragePath(objectPath)}";
                using var req = new HttpRequestMessage(HttpMethod.Post, endpoint);
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", serviceRoleKey);
                req.Headers.Add("apikey", serviceRoleKey);
                req.Content = new StringContent("{\"expiresIn\":3600}", Encoding.UTF8, "application/json");

                using var res = await StorageHttpClient.SendAsync(req, cancellationToken);
                if (!res.IsSuccessStatusCode)
                {
                    return null;
                }

                var body = await res.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(body);
                var signedPath = GetJsonString(doc.RootElement, "signedURL", "signedUrl");
                if (string.IsNullOrWhiteSpace(signedPath))
                {
                    return null;
                }

                if (signedPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                    signedPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    return signedPath;
                }

                return signedPath.StartsWith("/storage/v1", StringComparison.OrdinalIgnoreCase)
                    ? $"{supabaseBaseUrl}{signedPath}"
                    : $"{supabaseBaseUrl}/storage/v1{signedPath}";
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Không thể tạo signed URL từ Supabase Storage.");
                return null;
            }
        }

        private static string EscapeStoragePath(string path)
        {
            return string.Join("/", path.Split('/', StringSplitOptions.RemoveEmptyEntries).Select(Uri.EscapeDataString));
        }

        private static ParsedLessonMedia ParseRawLessonMedia(string? rawContent)
        {
            var raw = (rawContent ?? string.Empty).Trim();
            if (raw.Length == 0)
            {
                return new ParsedLessonMedia(false, string.Empty, null, null);
            }

            if (!raw.StartsWith(LessonMediaPrefix, StringComparison.Ordinal))
            {
                return new ParsedLessonMedia(false, raw, null, null);
            }

            var jsonPart = raw[LessonMediaPrefix.Length..].Trim();
            if (jsonPart.Length == 0)
            {
                return new ParsedLessonMedia(true, string.Empty, null, null);
            }

            try
            {
                using var doc = JsonDocument.Parse(jsonPart);
                if (doc.RootElement.ValueKind != JsonValueKind.Object)
                {
                    return new ParsedLessonMedia(false, raw, null, null);
                }

                var root = doc.RootElement;
                var text = GetJsonString(root, "Text", "text")?.Trim() ?? string.Empty;
                var pdf = ParseMediaRef(root, "Pdf", "pdf", "Tài liệu PDF");
                var video = ParseMediaRef(root, "Video", "video", "Video bài giảng");
                return new ParsedLessonMedia(true, text, pdf, video);
            }
            catch
            {
                return new ParsedLessonMedia(false, raw, null, null);
            }
        }

        private static MediaRef? ParseMediaRef(JsonElement root, string primaryProp, string altProp, string fallbackName)
        {
            if (!TryGetProperty(root, primaryProp, altProp, out var el) || el.ValueKind == JsonValueKind.Null || el.ValueKind == JsonValueKind.Undefined)
            {
                return null;
            }

            if (el.ValueKind == JsonValueKind.String)
            {
                var str = el.GetString()?.Trim();
                if (string.IsNullOrEmpty(str)) return null;
                var fileName = Path.GetFileName(str);
                return new MediaRef(str, null, null, string.IsNullOrEmpty(fileName) ? fallbackName : fileName);
            }

            if (el.ValueKind == JsonValueKind.Object)
            {
                var url = GetJsonString(el, "Url", "url");
                var key = GetJsonString(el, "Key", "key") ?? GetJsonString(el, "Path", "path");
                var bucket = GetJsonString(el, "Bucket", "bucket");
                var name = GetJsonString(el, "FileName", "fileName") ?? GetJsonString(el, "Name", "name");
                if (string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(key))
                {
                    name = Path.GetFileName(key);
                }
                if (string.IsNullOrWhiteSpace(url) && string.IsNullOrWhiteSpace(key))
                {
                    return null;
                }
                return new MediaRef(key, bucket, url, string.IsNullOrWhiteSpace(name) ? fallbackName : name.Trim());
            }

            return null;
        }

        private static bool TryGetProperty(JsonElement obj, string prop1, string prop2, out JsonElement value)
        {
            if (obj.TryGetProperty(prop1, out value)) return true;
            if (obj.TryGetProperty(prop2, out value)) return true;
            value = default;
            return false;
        }

        private static string? GetJsonString(JsonElement obj, string prop1, string prop2)
        {
            if (TryGetProperty(obj, prop1, prop2, out var el) && el.ValueKind == JsonValueKind.String)
            {
                return el.GetString();
            }
            return null;
        }

        private sealed record MediaRef(string? Key, string? Bucket, string? Url, string FileName);
        private sealed record ParsedLessonMedia(bool IsStructured, string Text, MediaRef? Pdf, MediaRef? Video);
        private sealed record StorageLookupResult(
            string DefaultBucket,
            Dictionary<string, bool> BucketPublic,
            Dictionary<string, (string Bucket, string Name)> ByKey);
        private sealed record ResolvedMediaUrl(string? Url, string? Bucket, bool RequiresPublicBucket);

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
