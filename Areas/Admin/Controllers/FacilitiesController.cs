using CNPM_EnglishLearning.Data;
using CNPM_EnglishLearning.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CNPM_EnglishLearning.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class FacilitiesController : Controller
    {
        // Khớp [MaxLength] của Entity Classroom (Name = 50, Location = 100). Description không giới hạn ở DB nên đặt trần hợp lý.
        private const int MaxNameLength = 50;
        private const int MaxLocationLength = 100;
        private const int MaxDescriptionLength = 1000;
        private const int MaxCapacity = 1000;

        private readonly ApplicationDbContext _context;
        private readonly ILogger<FacilitiesController> _logger;

        public FacilitiesController(ApplicationDbContext context, ILogger<FacilitiesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View("~/Areas/Admin/Views/facilities.html");
        }

        [HttpGet("/Admin/api/Facilities/List")]
        public async Task<IActionResult> List(CancellationToken cancellationToken)
        {
            try
            {
                // 1 truy vấn duy nhất: đếm số ca/tuần bằng subquery tương quan ngay trên PostgreSQL
                var items = await _context.Classrooms
                    .AsNoTracking()
                    .OrderBy(c => c.Id)
                    .Select(c => new
                    {
                        id = c.Id,
                        name = c.Name,
                        location = c.Location,
                        capacity = c.Capacity,
                        description = c.Description,
                        isActive = c.IsActive,
                        createdAt = c.CreatedAt,
                        activeScheduleCount = (
                            from cs in _context.ClassSchedules
                            join cc in _context.CourseClasses on cs.ClassId equals cc.Id
                            where cs.ClassroomId == c.Id && cc.Status != "cancelled"
                            select cs.Id
                        ).Count()
                    })
                    .ToListAsync(cancellationToken);

                return Ok(new { success = true, items });
            }
            catch (OperationCanceledException)
            {
                return this.ApiCancelled();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể tải danh sách phòng học.");
                return this.ApiServerError("Không thể tải danh sách phòng học.");
            }
        }

        [HttpPost("/Admin/api/Facilities/Create")]
        public async Task<IActionResult> Create([FromBody] FacilityUpsertDto? dto, CancellationToken cancellationToken)
        {
            var error = Normalize(dto, out var name, out var location, out var description);
            if (error != null)
            {
                return this.ApiBadRequest(error);
            }

            try
            {
                if (await IsDuplicateNameAsync(name, null, cancellationToken))
                {
                    return this.ApiBadRequest("Tên phòng học đã tồn tại trong hệ thống.");
                }

                var classroom = new Classroom
                {
                    Name = name,
                    Location = location,
                    Capacity = dto!.Capacity,
                    Description = description,
                    IsActive = dto.IsActive ?? true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Classrooms.Add(classroom);
                try
                {
                    await _context.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
                {
                    return this.ApiBadRequest("Tên phòng học đã tồn tại.");
                }

                return Ok(new { success = true, message = "Thêm phòng học thành công.", item = classroom });
            }
            catch (OperationCanceledException)
            {
                return this.ApiCancelled();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể thêm phòng học.");
                return this.ApiServerError("Không thể thêm phòng học.");
            }
        }

        [HttpPost("/Admin/api/Facilities/Update/{id}")]
        [HttpPut("/Admin/api/Facilities/Update/{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] FacilityUpsertDto? dto, CancellationToken cancellationToken)
        {
            var error = Normalize(dto, out var name, out var location, out var description);
            if (error != null)
            {
                return this.ApiBadRequest(error);
            }

            try
            {
                var classroom = await _context.Classrooms.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
                if (classroom == null)
                {
                    return this.ApiNotFound("Không tìm thấy phòng học.");
                }

                if (await IsDuplicateNameAsync(name, id, cancellationToken))
                {
                    return this.ApiBadRequest("Tên phòng học đã tồn tại trong hệ thống.");
                }

                classroom.Name = name;
                classroom.Location = location;
                classroom.Capacity = dto!.Capacity;
                classroom.Description = description;
                if (dto.IsActive.HasValue)
                {
                    classroom.IsActive = dto.IsActive.Value;
                }

                try
                {
                    await _context.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
                {
                    return this.ApiBadRequest("Tên phòng học đã tồn tại.");
                }

                return Ok(new { success = true, message = "Cập nhật phòng học thành công.", item = classroom });
            }
            catch (OperationCanceledException)
            {
                return this.ApiCancelled();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể cập nhật phòng học {ClassroomId}.", id);
                return this.ApiServerError("Không thể cập nhật phòng học.");
            }
        }

        [HttpPost("/Admin/api/Facilities/ToggleStatus/{id}")]
        public async Task<IActionResult> ToggleStatus(int id, CancellationToken cancellationToken)
        {
            try
            {
                var classroom = await _context.Classrooms.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
                if (classroom == null)
                {
                    return this.ApiNotFound("Không tìm thấy phòng học.");
                }

                classroom.IsActive = !classroom.IsActive;
                await _context.SaveChangesAsync(cancellationToken);

                return Ok(new
                {
                    success = true,
                    message = classroom.IsActive ? "Đã kích hoạt lại phòng học." : "Đã tạm ngưng sử dụng phòng học.",
                    id = classroom.Id,
                    isActive = classroom.IsActive
                });
            }
            catch (OperationCanceledException)
            {
                return this.ApiCancelled();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể thay đổi trạng thái phòng học {ClassroomId}.", id);
                return this.ApiServerError("Không thể thay đổi trạng thái phòng học.");
            }
        }

        private Task<bool> IsDuplicateNameAsync(string name, int? excludeId, CancellationToken cancellationToken)
        {
            var normalizedName = name.Trim().ToLower();
            return _context.Classrooms
                .AsNoTracking()
                .AnyAsync(c => (!excludeId.HasValue || c.Id != excludeId.Value) && c.Name.ToLower() == normalizedName, cancellationToken);
        }

        /// <summary>
        /// Chuẩn hóa (Trim) và kiểm tra dữ liệu đầu vào. Trả về thông báo lỗi (HTTP 400) hoặc null nếu hợp lệ.
        /// </summary>
        private static string? Normalize(FacilityUpsertDto? dto, out string name, out string? location, out string? description)
        {
            name = string.Empty;
            location = null;
            description = null;

            if (dto == null)
            {
                return "Dữ liệu gửi lên không hợp lệ.";
            }

            name = (dto.Name ?? string.Empty).Trim();
            location = string.IsNullOrWhiteSpace(dto.Location) ? null : dto.Location.Trim();
            description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();

            if (name.Length == 0)
            {
                return "Tên phòng học không được để trống.";
            }

            if (name.Length > MaxNameLength)
            {
                return $"Tên phòng học không được vượt quá {MaxNameLength} ký tự.";
            }

            if (location != null && location.Length > MaxLocationLength)
            {
                return $"Vị trí không được vượt quá {MaxLocationLength} ký tự.";
            }

            if (description != null && description.Length > MaxDescriptionLength)
            {
                return $"Mô tả không được vượt quá {MaxDescriptionLength} ký tự.";
            }

            if (dto.Capacity <= 0)
            {
                return "Sức chứa phải lớn hơn 0.";
            }

            if (dto.Capacity > MaxCapacity)
            {
                return $"Sức chứa không được vượt quá {MaxCapacity} chỗ.";
            }

            return null;
        }

        public class FacilityUpsertDto
        {
            public string Name { get; set; } = string.Empty;
            public string? Location { get; set; }
            public int Capacity { get; set; }
            public string? Description { get; set; }
            public bool? IsActive { get; set; }
        }
    }
}
