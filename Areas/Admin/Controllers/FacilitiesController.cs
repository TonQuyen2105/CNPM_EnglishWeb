using CNPM_EnglishLearning.Data;
using CNPM_EnglishLearning.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CNPM_EnglishLearning.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class FacilitiesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FacilitiesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View("~/Areas/Admin/Views/facilities.html");
        }

        [HttpGet("/Admin/api/Facilities/List")]
        public async Task<IActionResult> List()
        {
            try
            {
                var scheduleCounts = await (
                    from cs in _context.ClassSchedules.AsNoTracking()
                    join cc in _context.CourseClasses.AsNoTracking() on cs.ClassId equals cc.Id
                    where cc.Status != "cancelled"
                    group cs by cs.ClassroomId into g
                    select new { ClassroomId = g.Key, Count = g.Count() }
                ).ToDictionaryAsync(x => x.ClassroomId, x => x.Count);

                var classrooms = await _context.Classrooms
                    .AsNoTracking()
                    .OrderBy(c => c.Id)
                    .ToListAsync();

                var result = classrooms.Select(c => new
                {
                    id = c.Id,
                    name = c.Name,
                    location = c.Location,
                    capacity = c.Capacity,
                    description = c.Description,
                    isActive = c.IsActive,
                    createdAt = c.CreatedAt,
                    activeScheduleCount = scheduleCounts.TryGetValue(c.Id, out var cnt) ? cnt : 0
                });

                return Ok(new { items = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Không thể tải danh sách phòng học.", detail = ex.Message });
            }
        }

        [HttpPost("/Admin/api/Facilities/Create")]
        public async Task<IActionResult> Create([FromBody] FacilityUpsertDto? dto)
        {
            if (dto == null)
            {
                return BadRequest(new { message = "Dữ liệu gửi lên không hợp lệ." });
            }

            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                return BadRequest(new { message = "Tên phòng học không được để trống." });
            }

            if (dto.Name.Trim().Length > 50)
            {
                return BadRequest(new { message = "Tên phòng học không được vượt quá 50 ký tự." });
            }

            if (dto.Capacity <= 0)
            {
                return BadRequest(new { message = "Sức chứa phải lớn hơn 0." });
            }

            try
            {
                var classroom = new Classroom
                {
                    Name = dto.Name.Trim(),
                    Location = string.IsNullOrWhiteSpace(dto.Location) ? null : dto.Location.Trim(),
                    Capacity = dto.Capacity,
                    Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
                    IsActive = dto.IsActive ?? true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Classrooms.Add(classroom);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Thêm phòng học thành công.", item = classroom });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Không thể thêm phòng học.", detail = ex.Message });
            }
        }

        [HttpPost("/Admin/api/Facilities/Update/{id}")]
        [HttpPut("/Admin/api/Facilities/Update/{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] FacilityUpsertDto? dto)
        {
            if (dto == null)
            {
                return BadRequest(new { message = "Dữ liệu gửi lên không hợp lệ." });
            }

            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                return BadRequest(new { message = "Tên phòng học không được để trống." });
            }

            if (dto.Name.Trim().Length > 50)
            {
                return BadRequest(new { message = "Tên phòng học không được vượt quá 50 ký tự." });
            }

            if (dto.Capacity <= 0)
            {
                return BadRequest(new { message = "Sức chứa phải lớn hơn 0." });
            }

            try
            {
                var classroom = await _context.Classrooms.FirstOrDefaultAsync(c => c.Id == id);
                if (classroom == null)
                {
                    return NotFound(new { message = "Không tìm thấy phòng học." });
                }

                classroom.Name = dto.Name.Trim();
                classroom.Location = string.IsNullOrWhiteSpace(dto.Location) ? null : dto.Location.Trim();
                classroom.Capacity = dto.Capacity;
                classroom.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
                if (dto.IsActive.HasValue)
                {
                    classroom.IsActive = dto.IsActive.Value;
                }

                await _context.SaveChangesAsync();

                return Ok(new { message = "Cập nhật phòng học thành công.", item = classroom });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Không thể cập nhật phòng học.", detail = ex.Message });
            }
        }

        [HttpPost("/Admin/api/Facilities/ToggleStatus/{id}")]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            try
            {
                var classroom = await _context.Classrooms.FirstOrDefaultAsync(c => c.Id == id);
                if (classroom == null)
                {
                    return NotFound(new { message = "Không tìm thấy phòng học." });
                }

                classroom.IsActive = !classroom.IsActive;
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = classroom.IsActive ? "Đã kích hoạt lại phòng học." : "Đã tạm ngưng sử dụng phòng học.",
                    id = classroom.Id,
                    isActive = classroom.IsActive
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Không thể thay đổi trạng thái phòng học.", detail = ex.Message });
            }
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
