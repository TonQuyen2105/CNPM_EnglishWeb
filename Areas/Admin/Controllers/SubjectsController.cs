using System.Text.RegularExpressions;
using CNPM_EnglishLearning.Data;
using CNPM_EnglishLearning.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CNPM_EnglishLearning.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "admin")]
    public class SubjectsController : Controller
    {
        private const int MaxNameLength = 100;
        private const int MaxCodeLength = 20;
        private const int MaxDescriptionLength = 1000;

        // Bảng public.subjects không có cột code riêng, nên quy ước lưu tiền tố [CODE] ở đầu description (nếu có).
        private static readonly Regex CodePrefixRegex = new(@"^\[([A-Za-z0-9_-]{2,20})\]\s*(.*)$", RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly Regex ValidCodeRegex = new(@"^[A-Za-z0-9_-]{2,20}$", RegexOptions.Compiled);

        private readonly ApplicationDbContext _context;
        private readonly ILogger<SubjectsController> _logger;

        public SubjectsController(ApplicationDbContext context, ILogger<SubjectsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: /Admin/Subjects
        public IActionResult Index()
        {
            return View("~/Areas/Admin/Views/subjects.html");
        }

        [HttpGet("/Admin/api/Subjects/List")]
        public async Task<IActionResult> List(string? search, string? status, CancellationToken cancellationToken)
        {
            try
            {
                var rawSubjects = await _context.Subjects
                    .AsNoTracking()
                    .OrderBy(s => s.Id)
                    .Select(s => new
                    {
                        s.Id,
                        s.Name,
                        s.Description,
                        s.IsActive,
                        s.CreatedAt,
                        s.UpdatedAt
                    })
                    .ToListAsync(cancellationToken);

                var allCourses = await _context.Courses
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

                var coursesBySubject = allCourses
                    .GroupBy(c => c.SubjectId)
                    .ToDictionary(g => g.Key, g => g.ToList());

                var parsed = rawSubjects.Select(s =>
                {
                    var code = ExtractSubjectCode(s.Id, s.Description, out var cleanDescription);
                    var subjCourses = coursesBySubject.TryGetValue(s.Id, out var cList) ? cList : [];
                    var totalPrice = subjCourses.Sum(c => c.Price);
                    return new
                    {
                        id = s.Id,
                        name = s.Name,
                        code,
                        description = cleanDescription,
                        isActive = s.IsActive,
                        courseCount = subjCourses.Count,
                        totalPrice,
                        courses = subjCourses.Select(c => new
                        {
                            id = c.Id,
                            title = c.Title,
                            price = c.Price,
                            level = c.Level,
                            isPublished = c.IsPublished
                        }).ToList(),
                        createdAt = s.CreatedAt,
                        updatedAt = s.UpdatedAt
                    };
                });

                if (!string.IsNullOrWhiteSpace(status) && !status.Equals("all", StringComparison.OrdinalIgnoreCase))
                {
                    var norm = status.Trim().ToLowerInvariant();
                    if (norm is "1" or "true" or "active")
                    {
                        parsed = parsed.Where(x => x.isActive);
                    }
                    else if (norm is "0" or "false" or "inactive")
                    {
                        parsed = parsed.Where(x => !x.isActive);
                    }
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var term = search.Trim().ToLowerInvariant();
                    parsed = parsed.Where(x =>
                        x.name.ToLowerInvariant().Contains(term) ||
                        x.code.ToLowerInvariant().Contains(term) ||
                        (x.description != null && x.description.ToLowerInvariant().Contains(term)));
                }

                var items = parsed.ToList();

                return Ok(new
                {
                    success = true,
                    items,
                    stats = new
                    {
                        totalCount = rawSubjects.Count,
                        activeCount = rawSubjects.Count(x => x.IsActive),
                        inactiveCount = rawSubjects.Count(x => !x.IsActive)
                    }
                });
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

        [HttpPost("/Admin/api/Subjects/Create")]
        public async Task<IActionResult> Create([FromBody] SubjectUpsertDto? dto, CancellationToken cancellationToken)
        {
            var error = NormalizeAndValidate(dto, out var name, out var code, out var description);
            if (error != null)
            {
                return this.ApiBadRequest(error);
            }

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({500_000_000L})", cancellationToken);

                var existing = await _context.Subjects
                    .AsNoTracking()
                    .Select(s => new { s.Id, s.Name, s.Description })
                    .ToListAsync(cancellationToken);

                if (existing.Any(s => string.Equals(s.Name.Trim(), name, StringComparison.OrdinalIgnoreCase)))
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiBadRequest("Tên khóa học đã tồn tại trong hệ thống.");
                }

                if (existing.Any(s => string.Equals(ExtractSubjectCode(s.Id, s.Description, out _), code, StringComparison.OrdinalIgnoreCase)))
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiBadRequest($"Mã khóa học '{code}' đã tồn tại trong hệ thống.");
                }

                var now = DateTime.UtcNow;
                var subject = new Subject
                {
                    Name = name,
                    Description = BuildStoredDescription(code, description),
                    IsActive = dto!.IsActive ?? true,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                _context.Subjects.Add(subject);
                try
                {
                    await _context.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiBadRequest("Tên hoặc mã khóa học đã tồn tại trong hệ thống.");
                }

                await transaction.CommitAsync(cancellationToken);

                return Ok(new
                {
                    success = true,
                    message = "Thêm khóa học mới thành công.",
                    item = new
                    {
                        id = subject.Id,
                        name = subject.Name,
                        code,
                        description,
                        isActive = subject.IsActive,
                        courseCount = 0
                    }
                });
            }
            catch (OperationCanceledException)
            {
                return this.ApiCancelled();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể thêm khóa học mới.");
                await transaction.RollbackAsync(CancellationToken.None);
                return this.ApiServerError("Không thể thêm khóa học mới.");
            }
        }

        [HttpPost("/Admin/api/Subjects/Update/{id}")]
        [HttpPut("/Admin/api/Subjects/Update/{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] SubjectUpsertDto? dto, CancellationToken cancellationToken)
        {
            if (id <= 0)
            {
                return this.ApiBadRequest("Mã định danh khóa học không hợp lệ.");
            }

            var error = NormalizeAndValidate(dto, out var name, out var code, out var description);
            if (error != null)
            {
                return this.ApiBadRequest(error);
            }

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({500_000_000L})", cancellationToken);

                var subject = await _context.Subjects.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
                if (subject == null)
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiNotFound("Không tìm thấy khóa học.");
                }

                var others = await _context.Subjects
                    .AsNoTracking()
                    .Where(s => s.Id != id)
                    .Select(s => new { s.Id, s.Name, s.Description })
                    .ToListAsync(cancellationToken);

                if (others.Any(s => string.Equals(s.Name.Trim(), name, StringComparison.OrdinalIgnoreCase)))
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiBadRequest("Tên khóa học đã tồn tại trong hệ thống.");
                }

                if (others.Any(s => string.Equals(ExtractSubjectCode(s.Id, s.Description, out _), code, StringComparison.OrdinalIgnoreCase)))
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiBadRequest($"Mã khóa học '{code}' đã được sử dụng bởi khóa học khác.");
                }

                subject.Name = name;
                subject.Description = BuildStoredDescription(code, description);
                if (dto!.IsActive.HasValue)
                {
                    subject.IsActive = dto.IsActive.Value;
                }
                subject.UpdatedAt = DateTime.UtcNow;

                try
                {
                    await _context.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiBadRequest("Tên hoặc mã khóa học đã tồn tại trong hệ thống.");
                }

                await transaction.CommitAsync(cancellationToken);

                return Ok(new
                {
                    success = true,
                    message = "Cập nhật khóa học thành công.",
                    id = subject.Id
                });
            }
            catch (OperationCanceledException)
            {
                return this.ApiCancelled();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể cập nhật khóa học {SubjectId}.", id);
                await transaction.RollbackAsync(CancellationToken.None);
                return this.ApiServerError("Không thể cập nhật khóa học.");
            }
        }

        [HttpPost("/Admin/api/Subjects/ToggleStatus/{id}")]
        public async Task<IActionResult> ToggleStatus(int id, CancellationToken cancellationToken)
        {
            if (id <= 0)
            {
                return this.ApiBadRequest("Mã định danh khóa học không hợp lệ.");
            }

            try
            {
                var subject = await _context.Subjects.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
                if (subject == null)
                {
                    return this.ApiNotFound("Không tìm thấy khóa học.");
                }

                subject.IsActive = !subject.IsActive;
                subject.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);

                return Ok(new
                {
                    success = true,
                    message = subject.IsActive ? "Đã kích hoạt khóa học." : "Đã tạm dừng khóa học.",
                    id = subject.Id,
                    isActive = subject.IsActive
                });
            }
            catch (OperationCanceledException)
            {
                return this.ApiCancelled();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể thay đổi trạng thái khóa học {SubjectId}.", id);
                return this.ApiServerError("Không thể thay đổi trạng thái khóa học.");
            }
        }

        private static string ExtractSubjectCode(int id, string? rawDescription, out string? cleanDescription)
        {
            if (!string.IsNullOrWhiteSpace(rawDescription))
            {
                var trimmed = rawDescription.Trim();
                var match = CodePrefixRegex.Match(trimmed);
                if (match.Success)
                {
                    var body = match.Groups[2].Value.Trim();
                    cleanDescription = body.Length == 0 ? null : body;
                    return match.Groups[1].Value.ToUpperInvariant();
                }
            }

            cleanDescription = string.IsNullOrWhiteSpace(rawDescription) ? null : rawDescription.Trim();
            return $"SUB-{id:D2}";
        }

        private static string BuildStoredDescription(string code, string? description)
        {
            return string.IsNullOrWhiteSpace(description)
                ? $"[{code}]"
                : $"[{code}] {description}";
        }

        private static string? NormalizeAndValidate(
            SubjectUpsertDto? dto,
            out string name,
            out string code,
            out string? description)
        {
            name = string.Empty;
            code = string.Empty;
            description = null;

            if (dto == null)
            {
                return "Dữ liệu gửi lên không hợp lệ.";
            }

            name = (dto.Name ?? string.Empty).Trim();
            code = (dto.Code ?? string.Empty).Trim().ToUpperInvariant();
            description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();

            if (name.Length == 0 || name.Length > MaxNameLength)
            {
                return $"Tên khóa học không hợp lệ (1 - {MaxNameLength} ký tự).";
            }

            if (code.Length == 0 || code.Length > MaxCodeLength || !ValidCodeRegex.IsMatch(code))
            {
                return "Mã khóa học phải dài 2 - 20 ký tự, chỉ gồm chữ cái, số, '-' hoặc '_'.";
            }

            if (description != null && description.Length > MaxDescriptionLength)
            {
                return $"Mô tả khóa học không được vượt quá {MaxDescriptionLength} ký tự.";
            }

            return null;
        }

        public class SubjectUpsertDto
        {
            public string? Name { get; set; }
            public string? Code { get; set; }
            public string? Description { get; set; }
            public bool? IsActive { get; set; }
        }
    }
}
