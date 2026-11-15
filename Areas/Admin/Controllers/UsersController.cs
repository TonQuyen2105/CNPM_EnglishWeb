using System.ComponentModel.DataAnnotations;
using System.Net.Mail;
using CNPM_EnglishLearning.Data;
using CNPM_EnglishLearning.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CNPM_EnglishLearning.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class UsersController : Controller
    {
        private const int MaxFullNameLength = 100;
        private const int MaxEmailLength = 150;
        private const int MinPasswordLength = 6;
        private const int MaxPasswordLength = 255;

        private static readonly HashSet<string> ValidRoles = new(StringComparer.OrdinalIgnoreCase)
        {
            "admin", "teacher", "student"
        };

        private readonly ApplicationDbContext _context;
        private readonly ILogger<UsersController> _logger;

        public UsersController(ApplicationDbContext context, ILogger<UsersController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: /Admin/Users
        public IActionResult Index()
        {
            return View("~/Areas/Admin/Views/users.html");
        }

        [HttpGet("/Admin/api/Users/List")]
        public async Task<IActionResult> List(
            string? search,
            string? role,
            string? status,
            CancellationToken cancellationToken,
            int page = 1,
            int pageSize = 10)
        {
            try
            {
                page = Math.Max(1, page);
                pageSize = Math.Clamp(pageSize, 1, 100);

                var statusStats = await _context.Users
                    .AsNoTracking()
                    .GroupBy(u => u.IsActive)
                    .Select(g => new { IsActive = g.Key, Count = g.Count() })
                    .ToListAsync(cancellationToken);

                var totalUsers = statusStats.Sum(x => x.Count);
                var activeUsers = statusStats.Where(x => x.IsActive).Sum(x => x.Count);
                var lockedUsers = statusStats.Where(x => !x.IsActive).Sum(x => x.Count);

                var query = _context.Users.AsNoTracking().AsQueryable();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var term = search.Trim().ToLower();
                    query = query.Where(u =>
                        u.FullName.ToLower().Contains(term) ||
                        u.Email.ToLower().Contains(term));
                }

                if (!string.IsNullOrWhiteSpace(role) && !role.Equals("all", StringComparison.OrdinalIgnoreCase))
                {
                    var normalizedRole = role.Trim().ToLowerInvariant();
                    if (ValidRoles.Contains(normalizedRole))
                    {
                        query = query.Where(u => u.Role == normalizedRole);
                    }
                }

                if (!string.IsNullOrWhiteSpace(status) && !status.Equals("all", StringComparison.OrdinalIgnoreCase))
                {
                    var normalizedStatus = status.Trim().ToLowerInvariant();
                    if (normalizedStatus is "1" or "true" or "active")
                    {
                        query = query.Where(u => u.IsActive);
                    }
                    else if (normalizedStatus is "0" or "false" or "locked")
                    {
                        query = query.Where(u => !u.IsActive);
                    }
                }

                var totalCount = await query.CountAsync(cancellationToken);
                var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
                if (page > totalPages)
                {
                    page = totalPages;
                }

                var items = await query
                    .OrderByDescending(u => u.Id)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(u => new
                    {
                        id = u.Id,
                        fullName = u.FullName,
                        email = u.Email,
                        role = u.Role,
                        isActive = u.IsActive,
                        createdAt = u.CreatedAt,
                        updatedAt = u.UpdatedAt
                    })
                    .ToListAsync(cancellationToken);

                return Ok(new
                {
                    success = true,
                    items,
                    page,
                    pageSize,
                    totalCount,
                    totalPages,
                    stats = new
                    {
                        totalUsers,
                        activeUsers,
                        lockedUsers
                    }
                });
            }
            catch (OperationCanceledException)
            {
                return this.ApiCancelled();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể tải danh sách người dùng.");
                return this.ApiServerError("Không thể tải danh sách người dùng.");
            }
        }

        [HttpPost("/Admin/api/Users/Create")]
        public async Task<IActionResult> Create([FromBody] UserUpsertDto? dto, CancellationToken cancellationToken)
        {
            var error = NormalizeAndValidate(dto, isCreate: true, out var fullName, out var email, out var role, out var password);
            if (error != null)
            {
                return this.ApiBadRequest(error);
            }

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var emailLower = email.ToLowerInvariant();
                var duplicate = await _context.Users
                    .AsNoTracking()
                    .AnyAsync(u => u.Email.ToLower() == emailLower, cancellationToken);
                if (duplicate)
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiBadRequest("Email này đã tồn tại trong hệ thống.");
                }

                var now = DateTime.UtcNow;
                var user = new User
                {
                    FullName = fullName,
                    Email = email,
                    Password = password!,
                    Role = role,
                    IsActive = dto!.IsActive ?? true,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                _context.Users.Add(user);
                try
                {
                    await _context.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiBadRequest("Email này đã tồn tại trong hệ thống.");
                }

                if (role == "teacher")
                {
                    _context.Teachers.Add(new Models.Entities.Teacher
                    {
                        UserId = user.Id,
                        IsVerified = false,
                        CreatedAt = now,
                        UpdatedAt = now
                    });
                    await _context.SaveChangesAsync(cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);

                return Ok(new
                {
                    success = true,
                    message = "Tạo tài khoản mới thành công.",
                    item = new
                    {
                        id = user.Id,
                        fullName = user.FullName,
                        email = user.Email,
                        role = user.Role,
                        isActive = user.IsActive,
                        createdAt = user.CreatedAt
                    }
                });
            }
            catch (OperationCanceledException)
            {
                return this.ApiCancelled();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể tạo tài khoản người dùng.");
                await transaction.RollbackAsync(CancellationToken.None);
                return this.ApiServerError("Không thể tạo tài khoản người dùng.");
            }
        }

        [HttpPost("/Admin/api/Users/Update/{id}")]
        [HttpPut("/Admin/api/Users/Update/{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UserUpsertDto? dto, CancellationToken cancellationToken)
        {
            var error = NormalizeAndValidate(dto, isCreate: false, out var fullName, out var email, out var role, out var password);
            if (error != null)
            {
                return this.ApiBadRequest(error);
            }

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
                if (user == null)
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiNotFound("Không tìm thấy tài khoản người dùng.");
                }

                var emailLower = email.ToLowerInvariant();
                var duplicate = await _context.Users
                    .AsNoTracking()
                    .AnyAsync(u => u.Id != id && u.Email.ToLower() == emailLower, cancellationToken);
                if (duplicate)
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiBadRequest("Email này đã được sử dụng bởi tài khoản khác.");
                }

                var now = DateTime.UtcNow;
                user.FullName = fullName;
                user.Email = email;
                user.Role = role;
                if (dto!.IsActive.HasValue)
                {
                    user.IsActive = dto.IsActive.Value;
                }
                if (!string.IsNullOrEmpty(password))
                {
                    user.Password = password;
                }
                user.UpdatedAt = now;

                try
                {
                    await _context.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiBadRequest("Email này đã tồn tại trong hệ thống.");
                }

                if (role == "teacher")
                {
                    var hasTeacherProfile = await _context.Teachers
                        .AsNoTracking()
                        .AnyAsync(t => t.UserId == user.Id, cancellationToken);
                    if (!hasTeacherProfile)
                    {
                        _context.Teachers.Add(new Models.Entities.Teacher
                        {
                            UserId = user.Id,
                            IsVerified = false,
                            CreatedAt = now,
                            UpdatedAt = now
                        });
                        await _context.SaveChangesAsync(cancellationToken);
                    }
                }

                await transaction.CommitAsync(cancellationToken);

                return Ok(new
                {
                    success = true,
                    message = "Cập nhật thông tin tài khoản thành công.",
                    item = new
                    {
                        id = user.Id,
                        fullName = user.FullName,
                        email = user.Email,
                        role = user.Role,
                        isActive = user.IsActive,
                        createdAt = user.CreatedAt,
                        updatedAt = user.UpdatedAt
                    }
                });
            }
            catch (OperationCanceledException)
            {
                return this.ApiCancelled();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể cập nhật tài khoản {UserId}.", id);
                await transaction.RollbackAsync(CancellationToken.None);
                return this.ApiServerError("Không thể cập nhật tài khoản.");
            }
        }

        [HttpPost("/Admin/api/Users/ToggleStatus/{id}")]
        public async Task<IActionResult> ToggleStatus(int id, CancellationToken cancellationToken)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
                if (user == null)
                {
                    return this.ApiNotFound("Không tìm thấy tài khoản người dùng.");
                }

                user.IsActive = !user.IsActive;
                user.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);

                return Ok(new
                {
                    success = true,
                    message = user.IsActive ? $"Đã mở khóa tài khoản {user.FullName}." : $"Đã khóa tài khoản {user.FullName}.",
                    id = user.Id,
                    isActive = user.IsActive
                });
            }
            catch (OperationCanceledException)
            {
                return this.ApiCancelled();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể thay đổi trạng thái tài khoản {UserId}.", id);
                return this.ApiServerError("Không thể thay đổi trạng thái tài khoản.");
            }
        }

        private static string? NormalizeAndValidate(
            UserUpsertDto? dto,
            bool isCreate,
            out string fullName,
            out string email,
            out string role,
            out string? password)
        {
            fullName = string.Empty;
            email = string.Empty;
            role = "student";
            password = null;

            if (dto == null)
            {
                return "Dữ liệu gửi lên không hợp lệ.";
            }

            fullName = (dto.FullName ?? string.Empty).Trim();
            email = (dto.Email ?? string.Empty).Trim();
            role = (dto.Role ?? "student").Trim().ToLowerInvariant();
            password = string.IsNullOrWhiteSpace(dto.Password) ? null : dto.Password.Trim();

            if (fullName.Length == 0)
            {
                return "Họ và tên không được để trống.";
            }
            if (fullName.Length > MaxFullNameLength)
            {
                return $"Họ và tên không được vượt quá {MaxFullNameLength} ký tự.";
            }

            if (email.Length == 0)
            {
                return "Địa chỉ Email không được để trống.";
            }
            if (email.Length > MaxEmailLength)
            {
                return $"Email không được vượt quá {MaxEmailLength} ký tự.";
            }
            if (!IsValidEmail(email))
            {
                return "Địa chỉ Email không đúng định dạng.";
            }

            if (!ValidRoles.Contains(role))
            {
                return "Vai trò phải là admin, teacher hoặc student.";
            }

            if (isCreate && string.IsNullOrEmpty(password))
            {
                return "Mật khẩu ban đầu không được để trống.";
            }

            if (password != null && (password.Length < MinPasswordLength || password.Length > MaxPasswordLength))
            {
                return $"Mật khẩu phải từ {MinPasswordLength} đến {MaxPasswordLength} ký tự.";
            }

            return null;
        }

        private static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new MailAddress(email);
                return string.Equals(addr.Address, email, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        public class UserUpsertDto
        {
            public string? FullName { get; set; }
            public string? Email { get; set; }
            public string? Password { get; set; }
            public string? Role { get; set; }
            public bool? IsActive { get; set; }
        }
    }
}
