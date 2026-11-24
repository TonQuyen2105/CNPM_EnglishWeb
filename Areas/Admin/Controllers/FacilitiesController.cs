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
    public class FacilitiesController : Controller
    {
        // Khớp [MaxLength] của Entity Classroom (Name = 50, Location = 100). Description không giới hạn ở DB nên đặt trần hợp lý.
        private const int MaxNameLength = 50;
        private const int MaxLocationLength = 100;
        private const int MaxDescriptionLength = 1000;
        private const int MaxCapacity = 1000;
        private const long ClassroomLockOffset = 200_000_000L;
        private const long TeacherLockOffset = 300_000_000L;

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
                await AdminCoreHelpers.SyncExpiredClassesAsync(_context, cancellationToken);

                var rooms = await _context.Classrooms
                    .AsNoTracking()
                    .OrderBy(c => c.Id)
                    .Select(c => new
                    {
                        c.Id,
                        c.Name,
                        c.Location,
                        c.Capacity,
                        c.Description,
                        c.IsActive,
                        c.CreatedAt
                    })
                    .ToListAsync(cancellationToken);

                var timeSlots = await _context.TimeSlots
                    .AsNoTracking()
                    .OrderBy(ts => ts.StartTime)
                    .Select(ts => new
                    {
                        id = ts.Id,
                        name = ts.Name,
                        startTime = AdminCoreHelpers.FormatTime24(ts.StartTime),
                        endTime = AdminCoreHelpers.FormatTime24(ts.EndTime),
                        timeRange = AdminCoreHelpers.FormatTimeRange(ts.StartTime, ts.EndTime),
                        isActive = ts.IsActive
                    })
                    .ToListAsync(cancellationToken);

                var roomIds = rooms.Select(r => r.Id).ToList();
                var scheduleCounts = roomIds.Count == 0
                    ? new Dictionary<int, int>()
                    : await (
                        from cs in _context.ClassSchedules.AsNoTracking()
                        join cc in _context.CourseClasses.AsNoTracking() on cs.ClassId equals cc.Id
                        where roomIds.Contains(cs.ClassroomId) && (cc.Status == "upcoming" || cc.Status == "ongoing")
                        group cs by cs.ClassroomId into g
                        select new { ClassroomId = g.Key, Count = g.Count() }
                    ).ToDictionaryAsync(x => x.ClassroomId, x => x.Count, cancellationToken);

                var scheduledClasses = roomIds.Count == 0
                    ? []
                    : await (
                        from cs in _context.ClassSchedules.AsNoTracking()
                        join cc in _context.CourseClasses.AsNoTracking() on cs.ClassId equals cc.Id
                        join ts in _context.TimeSlots.AsNoTracking() on cs.TimeSlotId equals ts.Id into tsJoin
                        from slot in tsJoin.DefaultIfEmpty()
                        where roomIds.Contains(cs.ClassroomId) && (cc.Status == "upcoming" || cc.Status == "ongoing")
                        select new
                        {
                            RoomId = cs.ClassroomId,
                            cc.Id,
                            cc.ClassCode,
                            cc.ClassName,
                            cc.Status,
                            cc.StartDate,
                            cc.EndDate,
                            cs.DayOfWeek,
                            TimeSlotId = cs.TimeSlotId,
                            SlotName = slot != null ? slot.Name : null,
                            StartTime = slot != null ? (TimeSpan?)slot.StartTime : null,
                            EndTime = slot != null ? (TimeSpan?)slot.EndTime : null
                        }
                    ).ToListAsync(cancellationToken);

                var activeClassesByRoom = scheduledClasses
                    .GroupBy(x => (x.RoomId, x.Id, x.TimeSlotId))
                    .Select(g =>
                    {
                        var first = g.First();
                        var daysLabel = string.Join(" - ", g.Select(r => r.DayOfWeek).Distinct().OrderBy(d => d).Select(AdminCoreHelpers.FormatDayShort));
                        return new
                        {
                            first.RoomId,
                            first.Id,
                            first.ClassCode,
                            first.ClassName,
                            first.Status,
                            first.StartDate,
                            first.EndDate,
                            first.TimeSlotId,
                            first.SlotName,
                            first.StartTime,
                            first.EndTime,
                            DaysLabel = daysLabel
                        };
                    })
                    .GroupBy(x => x.RoomId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.OrderBy(x => x.StartTime ?? TimeSpan.MaxValue).ThenBy(x => x.ClassCode).Select(x => new
                        {
                            id = x.Id,
                            classCode = x.ClassCode,
                            className = x.ClassName,
                            status = x.Status,
                            startDate = x.StartDate,
                            endDate = x.EndDate,
                            timeSlotId = x.TimeSlotId,
                            timeSlotName = x.SlotName,
                            startTime = x.StartTime.HasValue ? AdminCoreHelpers.FormatTime24(x.StartTime.Value) : null,
                            endTime = x.EndTime.HasValue ? AdminCoreHelpers.FormatTime24(x.EndTime.Value) : null,
                            timeRange = x.StartTime.HasValue && x.EndTime.HasValue
                                ? AdminCoreHelpers.FormatTimeRange(x.StartTime.Value, x.EndTime.Value)
                                : null,
                            daysLabel = x.DaysLabel
                        }).ToList());

                var items = rooms.Select(c =>
                {
                    var roomActiveClasses = activeClassesByRoom.TryGetValue(c.Id, out var list) ? list : [];
                    var activeSchedCount = scheduleCounts.GetValueOrDefault(c.Id);

                    return new
                    {
                        id = c.Id,
                        name = c.Name,
                        location = c.Location,
                        capacity = c.Capacity,
                        description = c.Description,
                        isActive = c.IsActive,
                        createdAt = c.CreatedAt,
                        activeScheduleCount = activeSchedCount,
                        hasConflict = false,
                        conflictMessages = Array.Empty<string>(),
                        isOccupied = roomActiveClasses.Count > 0,
                        activeClasses = roomActiveClasses
                    };
                }).ToList();

                return Ok(new
                {
                    success = true,
                    items,
                    timeSlots = timeSlots.Where(ts => ts.isActive).ToList()
                });
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

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                if (await IsDuplicateNameAsync(name, null, cancellationToken))
                {
                    await transaction.RollbackAsync(CancellationToken.None);
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
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiBadRequest("Tên phòng học đã tồn tại.");
                }

                await transaction.CommitAsync(cancellationToken);
                return Ok(new { success = true, message = "Thêm phòng học thành công.", item = classroom, warnings = Array.Empty<string>() });
            }
            catch (OperationCanceledException)
            {
                return this.ApiCancelled();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể thêm phòng học.");
                await transaction.RollbackAsync(CancellationToken.None);
                return this.ApiServerError("Không thể thêm phòng học.");
            }
        }

        [HttpPost("/Admin/api/Facilities/Update/{id}")]
        [HttpPut("/Admin/api/Facilities/Update/{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] FacilityUpsertDto? dto, CancellationToken cancellationToken)
        {
            if (id <= 0)
            {
                return this.ApiBadRequest("Mã phòng học không hợp lệ.");
            }

            var error = Normalize(dto, out var name, out var location, out var description);
            if (error != null)
            {
                return this.ApiBadRequest(error);
            }

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                long roomLockKey = ClassroomLockOffset + id;
                await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({roomLockKey})", cancellationToken);

                var classroom = await _context.Classrooms.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
                if (classroom == null)
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiNotFound("Không tìm thấy phòng học.");
                }

                if (await IsDuplicateNameAsync(name, id, cancellationToken))
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiBadRequest("Tên phòng học đã tồn tại trong hệ thống.");
                }

                var isDeactivating = dto!.IsActive.HasValue && !dto.IsActive.Value && classroom.IsActive;
                var isReducingCapacity = dto.Capacity < classroom.Capacity;

                if (isDeactivating || isReducingCapacity)
                {
                    var activeClassesUsingRoom = await (
                        from cs in _context.ClassSchedules.AsNoTracking()
                        join cc in _context.CourseClasses.AsNoTracking() on cs.ClassId equals cc.Id
                        where cs.ClassroomId == id && (cc.Status == "upcoming" || cc.Status == "ongoing")
                        select new { cc.Id, cc.ClassCode, cc.ClassName, cc.MaxStudents }
                    ).Distinct().ToListAsync(cancellationToken);

                    if (isDeactivating && activeClassesUsingRoom.Count > 0)
                    {
                        await transaction.RollbackAsync(CancellationToken.None);
                        var codes = string.Join(", ", activeClassesUsingRoom.Select(c => c.ClassCode).OrderBy(x => x));
                        return this.ApiBadRequest(
                            $"Không thể tạm ngưng phòng {classroom.Name} vì đang có {activeClassesUsingRoom.Count} lớp hoạt động mở tại đây ({codes}).");
                    }

                    if (isReducingCapacity)
                    {
                        var oversized = activeClassesUsingRoom
                            .OrderByDescending(c => c.MaxStudents)
                            .FirstOrDefault(c => c.MaxStudents > dto.Capacity);
                        if (oversized != null)
                        {
                            await transaction.RollbackAsync(CancellationToken.None);
                            return this.ApiBadRequest(
                                $"Không thể giảm sức chứa phòng {classroom.Name} xuống {dto.Capacity} chỗ vì lớp {oversized.ClassCode} ({oversized.ClassName}) đang mở tại đây có sĩ số tối đa là {oversized.MaxStudents} học viên.");
                        }
                    }
                }

                classroom.Name = name;
                classroom.Location = location;
                classroom.Capacity = dto.Capacity;
                classroom.Description = description;
                if (dto.IsActive.HasValue)
                {
                    classroom.IsActive = dto.IsActive.Value;
                }

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return Ok(new { success = true, message = "Cập nhật phòng học thành công.", item = classroom, warnings = Array.Empty<string>() });
            }
            catch (OperationCanceledException)
            {
                return this.ApiCancelled();
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                await transaction.RollbackAsync(CancellationToken.None);
                return this.ApiBadRequest("Tên phòng học đã tồn tại.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể cập nhật phòng học {ClassroomId}.", id);
                await transaction.RollbackAsync(CancellationToken.None);
                return this.ApiServerError("Không thể cập nhật phòng học.");
            }
        }

        [HttpPost("/Admin/api/Facilities/ToggleStatus/{id}")]
        public async Task<IActionResult> ToggleStatus(int id, CancellationToken cancellationToken)
        {
            if (id <= 0)
            {
                return this.ApiBadRequest("Mã phòng học không hợp lệ.");
            }

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                long roomLockKey = ClassroomLockOffset + id;
                await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({roomLockKey})", cancellationToken);

                var classroom = await _context.Classrooms.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
                if (classroom == null)
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiNotFound("Không tìm thấy phòng học.");
                }

                if (classroom.IsActive)
                {
                    var activeClassCodes = await (
                        from cs in _context.ClassSchedules.AsNoTracking()
                        join cc in _context.CourseClasses.AsNoTracking() on cs.ClassId equals cc.Id
                        where cs.ClassroomId == id && (cc.Status == "upcoming" || cc.Status == "ongoing")
                        select cc.ClassCode
                    ).Distinct().OrderBy(code => code).ToListAsync(cancellationToken);

                    if (activeClassCodes.Count > 0)
                    {
                        await transaction.RollbackAsync(CancellationToken.None);
                        return this.ApiBadRequest(
                            $"Không thể tạm ngưng phòng {classroom.Name} vì đang có {activeClassCodes.Count} lớp hoạt động mở tại đây ({string.Join(", ", activeClassCodes)}).");
                    }
                }

                classroom.IsActive = !classroom.IsActive;
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

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
                await transaction.RollbackAsync(CancellationToken.None);
                return this.ApiServerError("Không thể thay đổi trạng thái phòng học.");
            }
        }

        private Task<bool> IsDuplicateNameAsync(string name, int? excludeId, CancellationToken cancellationToken)
        {
            var pattern = AdminCoreHelpers.EscapeLikePattern(name.Trim());
            return _context.Classrooms
                .AsNoTracking()
                .AnyAsync(c => (!excludeId.HasValue || c.Id != excludeId.Value) && EF.Functions.ILike(c.Name, pattern, "\\"), cancellationToken);
        }

        /// <summary>
        /// Chuẩn hóa (Trim) và kiểm tra dữ liệu đầu vào. Trả về thông báo lỗi (HTTP 400) hoặc null nếu hợp lệ.
        /// </summary>
        private static string? Normalize(
            FacilityUpsertDto? dto,
            out string name,
            out string? location,
            out string? description)
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
