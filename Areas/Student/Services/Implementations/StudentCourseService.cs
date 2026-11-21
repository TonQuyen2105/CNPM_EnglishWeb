using CNPM_EnglishLearning.Areas.Student.DTOs;
using CNPM_EnglishLearning.Areas.Student.DTOs.Common;
using CNPM_EnglishLearning.Areas.Student.Models;
using CNPM_EnglishLearning.Areas.Student.Services.Interfaces;
using CNPM_EnglishLearning.Data;
using CNPM_EnglishLearning.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace CNPM_EnglishLearning.Areas.Student.Services.Implementations
{
    /// <summary>
    /// Nhóm 2 & Nhóm 8: Nghiệp vụ Khám phá Khóa học, Đăng ký Tư vấn, Đăng ký Lớp học & Đánh giá Khóa học.
    /// </summary>
    public class StudentCourseService : BaseStudentService, IStudentCourseService
    {
        public StudentCourseService(ApplicationDbContext db) : base(db)
        {
        }

        /// <summary>
        /// 2.1. Lấy danh sách khóa học công khai (có lọc theo môn, trình độ, từ khóa và phân trang).
        /// </summary>
        public async Task<ServiceResult<List<CourseListItemDto>>> GetPublishedCoursesAsync(CourseFilterQueryDto query)
        {
            var (page, limit, skip) = NormalizePagination(query.Page, query.Limit);

            var coursesQuery = Db.Courses.AsNoTracking().Where(c => c.IsPublished);

            if (query.SubjectId.HasValue && query.SubjectId.Value > 0)
            {
                var subjectId = query.SubjectId.Value;
                coursesQuery = coursesQuery.Where(c => c.SubjectId == subjectId);
            }

            if (!string.IsNullOrWhiteSpace(query.Level))
            {
                var normalizedLevel = query.Level.Trim().ToLower();
                coursesQuery = coursesQuery.Where(c => c.Level.ToLower() == normalizedLevel);
            }

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var keyword = query.Search.Trim().ToLower();
                coursesQuery = coursesQuery.Where(c =>
                    c.Title.ToLower().Contains(keyword) ||
                    (c.Description != null && c.Description.ToLower().Contains(keyword)));
            }

            var totalCount = await coursesQuery.CountAsync();

            var pagedCourses = await (
                from c in coursesQuery
                join s in Db.Subjects.AsNoTracking() on c.SubjectId equals s.Id into sGroup
                from s in sGroup.DefaultIfEmpty()
                orderby c.CreatedAt descending, c.Id
                select new
                {
                    c.Id,
                    c.SubjectId,
                    SubjectName = s != null ? s.Name : "Tiếng Anh Tổng Quát",
                    c.Title,
                    c.Description,
                    c.ThumbnailUrl,
                    c.Level,
                    c.Price
                }
            ).Skip(skip).Take(limit).ToListAsync();

            // Tái sử dụng danh sách CourseId của trang hiện tại để truy vấn gộp (Batch Aggregation)
            var courseIds = pagedCourses.Select(c => c.Id).ToList();

            var reviewStatsByCourse = courseIds.Count == 0
                ? new Dictionary<int, (double AvgRating, int Count)>()
                : (await Db.CourseReviews.AsNoTracking()
                    .Where(r => courseIds.Contains(r.CourseId))
                    .GroupBy(r => r.CourseId)
                    .Select(g => new
                    {
                        CourseId = g.Key,
                        AvgRating = g.Average(x => (double)x.Rating),
                        Count = g.Count()
                    })
                    .ToListAsync())
                    .ToDictionary(x => x.CourseId, x => (AvgRating: Math.Round(x.AvgRating, 1), Count: x.Count));

            var lessonCountByCourse = courseIds.Count == 0
                ? new Dictionary<int, int>()
                : (await Db.Lessons.AsNoTracking()
                    .Where(l => courseIds.Contains(l.CourseId))
                    .GroupBy(l => l.CourseId)
                    .Select(g => new { CourseId = g.Key, Count = g.Count() })
                    .ToListAsync())
                    .ToDictionary(x => x.CourseId, x => x.Count);

            var openClassCountByCourse = courseIds.Count == 0
                ? new Dictionary<int, int>()
                : (await Db.CourseClasses.AsNoTracking()
                    .Where(cc => courseIds.Contains(cc.CourseId) && cc.Status != "cancelled")
                    .GroupBy(cc => cc.CourseId)
                    .Select(g => new { CourseId = g.Key, Count = g.Count() })
                    .ToListAsync())
                    .ToDictionary(x => x.CourseId, x => x.Count);

            var resultList = pagedCourses.Select(c =>
            {
                var reviewStat = reviewStatsByCourse.GetValueOrDefault(c.Id, (AvgRating: 5.0, Count: 0));
                return new CourseListItemDto
                {
                    Id = c.Id,
                    SubjectId = c.SubjectId,
                    SubjectName = c.SubjectName,
                    Title = c.Title,
                    Description = c.Description,
                    ThumbnailUrl = c.ThumbnailUrl,
                    Level = c.Level,
                    Price = c.Price,
                    AverageRating = reviewStat.Count > 0 ? reviewStat.AvgRating : 5.0,
                    ReviewsCount = reviewStat.Count,
                    TotalLessons = lessonCountByCourse.GetValueOrDefault(c.Id, 0),
                    OpenClassesCount = openClassCountByCourse.GetValueOrDefault(c.Id, 0)
                };
            }).ToList();

            var meta = new PaginationMeta(page, limit, totalCount);
            return ServiceResult<List<CourseListItemDto>>.Ok(resultList, "Lấy danh sách khóa học thành công.", meta);
        }

        /// <summary>
        /// 2.2. Xem chi tiết khóa học, đề cương (syllabus) & danh sách lớp đang mở đăng ký.
        /// </summary>
        public async Task<ServiceResult<CourseDetailDto>> GetCourseDetailAsync(int courseId, int? studentId)
        {
            var courseData = await (
                from c in Db.Courses.AsNoTracking()
                join s in Db.Subjects.AsNoTracking() on c.SubjectId equals s.Id into sGroup
                from s in sGroup.DefaultIfEmpty()
                where c.Id == courseId && c.IsPublished
                select new
                {
                    c.Id,
                    c.SubjectId,
                    SubjectName = s != null ? s.Name : "Tiếng Anh Tổng Quát",
                    c.Title,
                    c.Description,
                    c.ThumbnailUrl,
                    c.Level,
                    c.Price
                }
            ).FirstOrDefaultAsync();

            if (courseData == null)
            {
                return ServiceResult<CourseDetailDto>.NotFound($"Không tìm thấy khóa học có ID = {courseId}.");
            }

            // 1. Thống kê đánh giá của khóa học
            var reviewSummary = await Db.CourseReviews.AsNoTracking()
                .Where(r => r.CourseId == courseId)
                .GroupBy(r => r.CourseId)
                .Select(g => new
                {
                    AvgRating = g.Average(x => (double)x.Rating),
                    Count = g.Count()
                })
                .FirstOrDefaultAsync();

            // 2. Lấy danh sách bài học (syllabus) kèm trạng thái hoàn thành của học sinh (nếu có)
            var lessons = await Db.Lessons.AsNoTracking()
                .Where(l => l.CourseId == courseId)
                .OrderBy(l => l.OrderIndex)
                .ThenBy(l => l.Id)
                .ToListAsync();

            var lessonIds = lessons.Select(l => l.Id).ToList();
            var progressByLessonId = (studentId.HasValue && lessonIds.Count > 0)
                ? (await Db.UserProgresses.AsNoTracking()
                    .Where(p => p.UserId == studentId.Value && lessonIds.Contains(p.LessonId))
                    .ToListAsync())
                    .GroupBy(p => p.LessonId)
                    .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.CompletedAt).First())
                : new Dictionary<int, UserProgress>();

            var syllabusDtos = lessons.Select(l =>
            {
                progressByLessonId.TryGetValue(l.Id, out var prog);
                return new CourseSyllabusLessonItemDto
                {
                    LessonId = l.Id,
                    Title = l.Title,
                    OrderIndex = l.OrderIndex,
                    ContentType = l.ContentType,
                    IsCompleted = prog?.IsCompleted ?? false,
                    Score = prog?.Score,
                    CompletedAt = prog?.CompletedAt
                };
            }).ToList();

            // 3. Lấy danh sách lớp của khóa học (bao gồm đầy đủ tất cả các lớp thuộc khóa học trong cơ sở dữ liệu)
            var openClasses = await Db.CourseClasses.AsNoTracking()
                .Where(cc => cc.CourseId == courseId)
                .OrderBy(cc => cc.StartDate)
                .ToListAsync();

            var classIds = openClasses.Select(cc => cc.Id).ToList();

            // Truy vấn gộp số lượng học sinh đang active trong từng lớp
            var enrollmentCountsByClass = classIds.Count == 0
                ? new Dictionary<int, int>()
                : (await Db.Enrollments.AsNoTracking()
                    .Where(e => classIds.Contains(e.ClassId) && e.Status == EnrollmentActive)
                    .GroupBy(e => e.ClassId)
                    .Select(g => new { ClassId = g.Key, Count = g.Count() })
                    .ToListAsync())
                    .ToDictionary(x => x.ClassId, x => x.Count);

            // Kiểm tra các lớp học sinh hiện tại đã đăng ký
            var studentEnrolledClassIds = (studentId.HasValue && classIds.Count > 0)
                ? (await Db.Enrollments.AsNoTracking()
                    .Where(e => e.UserId == studentId.Value && classIds.Contains(e.ClassId) && e.Status == EnrollmentActive)
                    .Select(e => e.ClassId)
                    .ToListAsync())
                    .ToHashSet()
                : new HashSet<int>();

            // Tái sử dụng phương thức lấy lịch học theo danh sách ClassId từ BaseStudentService
            var schedulesByClass = await GetSchedulesByClassIdsAsync(classIds);

            var availableClassesDto = openClasses.Select(cc =>
            {
                var enrolledCount = enrollmentCountsByClass.GetValueOrDefault(cc.Id, 0);
                var availableSlots = Math.Max(0, cc.MaxStudents - enrolledCount);
                return new CourseOpenClassDto
                {
                    ClassId = cc.Id,
                    ClassCode = cc.ClassCode,
                    ClassName = cc.ClassName,
                    StartDate = cc.StartDate,
                    EndDate = cc.EndDate,
                    Price = cc.Price,
                    Status = cc.Status,
                    MaxStudents = cc.MaxStudents,
                    EnrolledStudents = enrolledCount,
                    AvailableSlots = availableSlots,
                    IsEnrolled = studentEnrolledClassIds.Contains(cc.Id),
                    Schedules = schedulesByClass.GetValueOrDefault(cc.Id, new List<ClassScheduleItemDto>())
                };
            }).ToList();

            var detailDto = new CourseDetailDto
            {
                Id = courseData.Id,
                SubjectId = courseData.SubjectId,
                SubjectName = courseData.SubjectName,
                Title = courseData.Title,
                Description = courseData.Description,
                ThumbnailUrl = courseData.ThumbnailUrl,
                Level = courseData.Level,
                Price = courseData.Price,
                AverageRating = reviewSummary != null ? Math.Round(reviewSummary.AvgRating, 1) : 5.0,
                ReviewsCount = reviewSummary?.Count ?? 0,
                Syllabus = syllabusDtos,
                AvailableClasses = availableClassesDto
            };

            return ServiceResult<CourseDetailDto>.Ok(detailDto, "Lấy chi tiết khóa học thành công.");
        }

        /// <summary>
        /// 2.3. Gửi yêu cầu đăng ký tư vấn khóa học.
        /// </summary>
        public async Task<ServiceResult<ConsultationRecord>> CreateConsultationAsync(int? studentId, CreateConsultationRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.FullName) || string.IsNullOrWhiteSpace(request.PhoneNumber))
            {
                return ServiceResult<ConsultationRecord>.BadRequest("Họ tên và số điện thoại là bắt buộc khi đăng ký tư vấn.");
            }

            string? courseTitle = null;
            if (request.CourseId > 0)
            {
                var course = await Db.Courses.AsNoTracking()
                    .Where(c => c.Id == request.CourseId)
                    .Select(c => new { c.Id, c.Title })
                    .FirstOrDefaultAsync();

                if (course == null)
                {
                    return ServiceResult<ConsultationRecord>.NotFound($"Không tìm thấy khóa học có ID = {request.CourseId}.");
                }
                courseTitle = course.Title;
            }

            var record = StudentExtendedStore.AddConsultation(new ConsultationRecord
            {
                UserId = studentId,
                CourseId = request.CourseId,
                CourseTitle = courseTitle,
                FullName = request.FullName.Trim(),
                PhoneNumber = request.PhoneNumber.Trim(),
                Email = request.Email?.Trim() ?? string.Empty,
                Note = request.Note?.Trim(),
                Status = "pending"
            });

            return ServiceResult<ConsultationRecord>.Created(record, "Gửi yêu cầu đăng ký tư vấn khóa học thành công.");
        }

        /// <summary>
        /// 2.4. Đăng ký tham gia lớp học (Kiểm tra sĩ số tối đa & kiểm tra trùng lịch học cá nhân).
        /// </summary>
        public async Task<ServiceResult<EnrollmentResultDto>> EnrollClassAsync(int studentId, CreateEnrollmentRequestDto request)
        {
            // 1. Lấy thông tin lớp cần đăng ký kèm khóa học
            var targetClassInfo = await (
                from cc in Db.CourseClasses.AsNoTracking()
                join c in Db.Courses.AsNoTracking() on cc.CourseId equals c.Id
                where cc.Id == request.ClassId
                select new
                {
                    Class = cc,
                    CourseTitle = c.Title
                }
            ).FirstOrDefaultAsync();

            if (targetClassInfo == null)
            {
                return ServiceResult<EnrollmentResultDto>.NotFound($"Không tìm thấy lớp học có ID = {request.ClassId}.");
            }

            var targetClass = targetClassInfo.Class;
            // Ràng buộc 1: Trạng thái lớp không được là cancelled hoặc completed
            if (string.Equals(targetClass.Status, "cancelled", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(targetClass.Status, "completed", StringComparison.OrdinalIgnoreCase))
            {
                return ServiceResult<EnrollmentResultDto>.BadRequest(
                    $"Lớp học '{targetClass.ClassName}' hiện không mở đăng ký (trạng thái: {targetClass.Status}).");
            }

            // Ràng buộc 2: Kiểm tra học sinh đã đăng ký lớp này chưa
            var existingEnrollment = await Db.Enrollments
                .FirstOrDefaultAsync(e => e.UserId == studentId && e.ClassId == request.ClassId);

            if (existingEnrollment != null && (existingEnrollment.Status == EnrollmentActive || existingEnrollment.Status == "active"))
            {
                return ServiceResult<EnrollmentResultDto>.Conflict($"Bạn đã đăng ký tham gia lớp '{targetClass.ClassName}' rồi.");
            }

            // Ràng buộc 3: Kiểm tra sĩ số tối đa (dựa trên số lượng enrollment active hiện tại)
            var currentActiveCount = await Db.Enrollments.AsNoTracking()
                .CountAsync(e => e.ClassId == request.ClassId && (e.Status == EnrollmentActive || e.Status == "active"));

            if (currentActiveCount >= targetClass.MaxStudents)
            {
                return ServiceResult<EnrollmentResultDto>.BadRequest(
                    $"Lớp '{targetClass.ClassName}' đã đủ sĩ số tối đa ({currentActiveCount}/{targetClass.MaxStudents} học viên).");
            }

            // 4. Kiểm tra trùng lịch học (Schedule Conflict Check) với các lớp đang active của học sinh
            var targetSchedules = await (
                from cs in Db.ClassSchedules.AsNoTracking()
                join ts in Db.TimeSlots.AsNoTracking() on cs.TimeSlotId equals ts.Id
                where cs.ClassId == request.ClassId
                select new
                {
                    cs.DayOfWeek,
                    cs.TimeSlotId,
                    ts.Name,
                    ts.StartTime,
                    ts.EndTime
                }
            ).ToListAsync();

            if (targetSchedules.Count > 0)
            {
                var existingSchedules = await (
                    from e in Db.Enrollments.AsNoTracking()
                    join cc in Db.CourseClasses.AsNoTracking() on e.ClassId equals cc.Id
                    join cs in Db.ClassSchedules.AsNoTracking() on cc.Id equals cs.ClassId
                    join ts in Db.TimeSlots.AsNoTracking() on cs.TimeSlotId equals ts.Id
                    where e.UserId == studentId
                          && e.Status == EnrollmentActive
                          && cc.Id != request.ClassId
                          && (cc.Status == ClassUpcoming || cc.Status == ClassOngoing)
                          && cc.StartDate <= targetClass.EndDate
                          && cc.EndDate >= targetClass.StartDate
                    select new
                    {
                        cc.ClassName,
                        cs.DayOfWeek,
                        cs.TimeSlotId,
                        ts.StartTime,
                        ts.EndTime
                    }
                ).ToListAsync();

                foreach (var targetSlot in targetSchedules)
                {
                    var conflict = existingSchedules.FirstOrDefault(ex =>
                        ex.DayOfWeek == targetSlot.DayOfWeek &&
                        (ex.TimeSlotId == targetSlot.TimeSlotId ||
                         (ex.StartTime < targetSlot.EndTime && targetSlot.StartTime < ex.EndTime)));

                    if (conflict != null)
                    {
                        var dayName = GetDayOfWeekName(targetSlot.DayOfWeek);
                        var timeRange = FormatTimeRange(targetSlot.StartTime, targetSlot.EndTime);
                        return ServiceResult<EnrollmentResultDto>.Conflict(
                            $"Trùng lịch học vào {dayName} ({timeRange}) với lớp '{conflict.ClassName}' mà bạn đang theo học.");
                    }
                }
            }

            // 5. Thực hiện lưu Enrollment và Payment trong Transaction đảm bảo toàn vẹn dữ liệu
            using var transaction = await Db.Database.BeginTransactionAsync();
            try
            {
                Enrollment enrollmentEntity;
                if (existingEnrollment != null)
                {
                    existingEnrollment.Status = EnrollmentActive;
                    existingEnrollment.EnrolledAt = DateTime.UtcNow;
                    enrollmentEntity = existingEnrollment;
                }
                else
                {
                    var nextEnrollmentId = await GetNextIdAsync(Db.Enrollments, e => e.Id);
                    enrollmentEntity = new Enrollment
                    {
                        Id = nextEnrollmentId,
                        UserId = studentId,
                        ClassId = request.ClassId,
                        EnrolledAt = DateTime.UtcNow,
                        Status = EnrollmentActive
                    };
                    Db.Enrollments.Add(enrollmentEntity);
                }

                await Db.SaveChangesAsync();

                // RÀNG BUỘC TOÀN VẸN: Kiểm tra bản ghi ghi danh (Enrollment) phải được lưu thành công trước khi ghi nhận Payment
                if (enrollmentEntity.Id <= 0)
                {
                    throw new InvalidOperationException("Không thể tạo hoặc cập nhật bản ghi đăng ký lớp học.");
                }

                // 6. Lưu thông tin giao dịch thanh toán vào bảng payments (PostgreSQL)
                var transactionCode = $"PAY-{DateTime.UtcNow:yyyyMMddHHmmss}-{studentId:D4}-{targetClass.Id:D4}";
                var normalizedMethod = "bank_transfer";
                if (!string.IsNullOrWhiteSpace(request.PaymentMethod))
                {
                    var lower = request.PaymentMethod.Trim().ToLowerInvariant();
                    if (lower.Contains("cash") || lower.Contains("tien_mat") || lower.Contains("tiền mặt"))
                    {
                        normalizedMethod = "cash";
                    }
                }

                var paymentEntity = new Payment
                {
                    UserId = studentId,
                    ClassId = targetClass.Id,
                    EnrollmentId = enrollmentEntity.Id,
                    TransactionCode = transactionCode,
                    Amount = targetClass.Price,
                    PaymentMethod = normalizedMethod,
                    Status = "completed",
                    PaidAt = DateTime.UtcNow,
                    Note = request.Note ?? $"Thanh toán học phí lớp {targetClass.ClassName} ({targetClass.ClassCode})",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                Db.Payments.Add(paymentEntity);
                await Db.SaveChangesAsync();

                // RÀNG BUỘC TOÀN VẸN: Kiểm tra bản ghi ghi danh (Enrollment) phải được lưu thành công trước khi ghi nhận Payment
                if (enrollmentEntity.Id <= 0)
                {
                    throw new InvalidOperationException("Không thể tạo hoặc cập nhật bản ghi đăng ký lớp học.");
                }

                await transaction.CommitAsync();

                var paymentDto = new PaymentDto
                {
                    Id = paymentEntity.Id,
                    UserId = studentId,
                    ClassId = targetClass.Id,
                    ClassCode = targetClass.ClassCode,
                    ClassName = targetClass.ClassName,
                    CourseId = targetClass.CourseId,
                    CourseTitle = targetClassInfo.CourseTitle,
                    EnrollmentId = enrollmentEntity.Id,
                    TransactionCode = paymentEntity.TransactionCode,
                    Amount = paymentEntity.Amount,
                    PaymentMethod = paymentEntity.PaymentMethod,
                    Status = paymentEntity.Status,
                    PaidAt = paymentEntity.PaidAt,
                    Note = paymentEntity.Note,
                    CreatedAt = paymentEntity.CreatedAt
                };

                var resultDto = new EnrollmentResultDto
                {
                    EnrollmentId = enrollmentEntity.Id,
                    UserId = studentId,
                    ClassId = targetClass.Id,
                    ClassCode = targetClass.ClassCode,
                    ClassName = targetClass.ClassName,
                    CourseTitle = targetClassInfo.CourseTitle,
                    Status = enrollmentEntity.Status,
                    EnrolledAt = enrollmentEntity.EnrolledAt,
                    Payment = paymentDto
                };

                return ServiceResult<EnrollmentResultDto>.Created(resultDto, "Đăng ký tham gia lớp học thành công.");
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// 2.5. Đăng ký khóa học (Tự động chọn lớp hoặc theo ClassId được chỉ định).
        /// </summary>
        public async Task<ServiceResult<EnrollmentResultDto>> EnrollCourseAsync(
            int studentId,
            int courseId,
            CreateCourseEnrollmentRequestDto request)
        {
            var courseExists = await Db.Courses.AsNoTracking().AnyAsync(c => c.Id == courseId);
            if (!courseExists)
            {
                return ServiceResult<EnrollmentResultDto>.NotFound($"Không tìm thấy khóa học có ID = {courseId}.");
            }

            // RÀNG BUỘC BẮT BUỘC: Học sinh phải chọn một lớp học cụ thể trước khi thực hiện thanh toán
            if (!request.ClassId.HasValue || request.ClassId.Value <= 0)
            {
                return ServiceResult<EnrollmentResultDto>.BadRequest(
                    "Vui lòng chọn một lớp học cụ thể từ danh sách trước khi thực hiện thanh toán.");
            }

            var validClass = await Db.CourseClasses.AsNoTracking()
                .AnyAsync(cc => cc.Id == request.ClassId.Value && cc.CourseId == courseId);

            if (!validClass)
            {
                return ServiceResult<EnrollmentResultDto>.BadRequest(
                    $"Lớp học ID = {request.ClassId.Value} không thuộc khóa học ID = {courseId}.");
            }

            return await EnrollClassAsync(studentId, new CreateEnrollmentRequestDto
            {
                ClassId = request.ClassId.Value,
                PaymentMethod = request.PaymentMethod,
                Note = request.Note
            });
        }

        /// <summary>
        /// 2.6. Lấy lịch sử giao dịch thanh toán của học sinh.
        /// </summary>
        public async Task<ServiceResult<List<PaymentDto>>> GetStudentPaymentsAsync(
            int studentId,
            int page,
            int limit,
            string? status)
        {
            var (safePage, safeLimit, skip) = NormalizePagination(page, limit);

            var query = Db.Payments.AsNoTracking()
                .Where(p => p.UserId == studentId);

            if (!string.IsNullOrWhiteSpace(status))
            {
                var normalizedStatus = status.Trim().ToLowerInvariant();
                query = query.Where(p => p.Status.ToLower() == normalizedStatus);
            }

            var payments = await (
                from p in query
                join cc in Db.CourseClasses.AsNoTracking() on p.ClassId equals cc.Id into ccGroup
                from cc in ccGroup.DefaultIfEmpty()
                join c in Db.Courses.AsNoTracking() on cc.CourseId equals c.Id into cGroup
                from c in cGroup.DefaultIfEmpty()
                orderby p.CreatedAt descending, p.Id descending
                select new PaymentDto
                {
                    Id = p.Id,
                    UserId = p.UserId,
                    ClassId = p.ClassId,
                    ClassCode = cc != null ? cc.ClassCode : null,
                    ClassName = cc != null ? cc.ClassName : null,
                    CourseId = c != null ? c.Id : null,
                    CourseTitle = c != null ? c.Title : null,
                    EnrollmentId = p.EnrollmentId,
                    TransactionCode = p.TransactionCode,
                    Amount = p.Amount,
                    PaymentMethod = p.PaymentMethod,
                    Status = p.Status,
                    PaidAt = p.PaidAt,
                    Note = p.Note,
                    CreatedAt = p.CreatedAt
                }
            )
            .Skip(skip)
            .Take(safeLimit)
            .ToListAsync();

            return ServiceResult<List<PaymentDto>>.Ok(payments, "Lấy lịch sử thanh toán thành công.");
        }

        /// <summary>
        /// 2.7. Lấy chi tiết một giao dịch thanh toán.
        /// </summary>
        public async Task<ServiceResult<PaymentDto>> GetPaymentDetailAsync(int studentId, int paymentId)
        {
            var payment = await (
                from p in Db.Payments.AsNoTracking()
                where p.UserId == studentId && p.Id == paymentId
                join cc in Db.CourseClasses.AsNoTracking() on p.ClassId equals cc.Id into ccGroup
                from cc in ccGroup.DefaultIfEmpty()
                join c in Db.Courses.AsNoTracking() on cc.CourseId equals c.Id into cGroup
                from c in cGroup.DefaultIfEmpty()
                select new PaymentDto
                {
                    Id = p.Id,
                    UserId = p.UserId,
                    ClassId = p.ClassId,
                    ClassCode = cc != null ? cc.ClassCode : null,
                    ClassName = cc != null ? cc.ClassName : null,
                    CourseId = c != null ? c.Id : null,
                    CourseTitle = c != null ? c.Title : null,
                    EnrollmentId = p.EnrollmentId,
                    TransactionCode = p.TransactionCode,
                    Amount = p.Amount,
                    PaymentMethod = p.PaymentMethod,
                    Status = p.Status,
                    PaidAt = p.PaidAt,
                    Note = p.Note,
                    CreatedAt = p.CreatedAt
                }
            ).FirstOrDefaultAsync();

            if (payment == null)
            {
                return ServiceResult<PaymentDto>.NotFound($"Không tìm thấy giao dịch thanh toán có ID = {paymentId}.");
            }

            return ServiceResult<PaymentDto>.Ok(payment, "Lấy chi tiết giao dịch thanh toán thành công.");
        }

        /// <summary>
        /// 8.1. Lấy danh sách đánh giá gần đây của khóa học (có phân trang).
        /// </summary>
        public async Task<ServiceResult<List<CourseReviewItemDto>>> GetCourseReviewsAsync(int courseId, int page, int limit)
        {
            var courseExists = await Db.Courses.AsNoTracking().AnyAsync(c => c.Id == courseId);
            if (!courseExists)
            {
                return ServiceResult<List<CourseReviewItemDto>>.NotFound($"Không tìm thấy khóa học có ID = {courseId}.");
            }

            var (safePage, safeLimit, skip) = NormalizePagination(page, limit);

            var baseQuery = Db.CourseReviews.AsNoTracking().Where(r => r.CourseId == courseId);
            var totalCount = await baseQuery.CountAsync();

            var reviews = await (
                from r in baseQuery
                join u in Db.Users.AsNoTracking() on r.UserId equals u.Id into uGroup
                from u in uGroup.DefaultIfEmpty()
                orderby r.CreatedAt descending, r.Id descending
                select new CourseReviewItemDto
                {
                    Id = r.Id,
                    CourseId = r.CourseId,
                    UserId = r.UserId,
                    UserFullName = u != null ? u.FullName : "Học viên ẩn danh",
                    UserAvatarUrl = u != null ? u.AvatarUrl : null,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt
                }
            ).Skip(skip).Take(safeLimit).ToListAsync();

            var meta = new PaginationMeta(safePage, safeLimit, totalCount);
            return ServiceResult<List<CourseReviewItemDto>>.Ok(reviews, "Lấy danh sách đánh giá khóa học thành công.", meta);
        }

        /// <summary>
        /// 8.2. Gửi đánh giá khóa học (kiểm tra học sinh đã từng đăng ký lớp thuộc khóa học này).
        /// </summary>
        public async Task<ServiceResult<CourseReviewItemDto>> SubmitCourseReviewAsync(
            int studentId,
            int courseId,
            CreateCourseReviewRequestDto request)
        {
            if (request.Rating < 1 || request.Rating > 5)
            {
                return ServiceResult<CourseReviewItemDto>.BadRequest("Điểm đánh giá (rating) phải nằm trong khoảng từ 1 đến 5.");
            }

            var courseExists = await Db.Courses.AsNoTracking().AnyAsync(c => c.Id == courseId);
            if (!courseExists)
            {
                return ServiceResult<CourseReviewItemDto>.NotFound($"Không tìm thấy khóa học có ID = {courseId}.");
            }

            // Kiểm tra học sinh đã từng đăng ký lớp thuộc khóa học này chưa
            var hasEnrolledInCourse = await (
                from e in Db.Enrollments.AsNoTracking()
                join cc in Db.CourseClasses.AsNoTracking() on e.ClassId equals cc.Id
                where e.UserId == studentId && cc.CourseId == courseId && e.Status != EnrollmentCancelled
                select e.Id
            ).AnyAsync();

            if (!hasEnrolledInCourse)
            {
                return ServiceResult<CourseReviewItemDto>.Forbidden(
                    "Bạn cần đăng ký tham gia ít nhất một lớp học thuộc khóa học này trước khi gửi đánh giá.");
            }

            // Nếu học sinh đã đánh giá khóa học này trước đó thì cập nhật, ngược lại tạo mới
            var reviewEntity = await Db.CourseReviews
                .FirstOrDefaultAsync(r => r.UserId == studentId && r.CourseId == courseId);

            if (reviewEntity != null)
            {
                reviewEntity.Rating = request.Rating;
                reviewEntity.Comment = request.Comment?.Trim();
                reviewEntity.CreatedAt = DateTime.UtcNow;
            }
            else
            {
                var nextReviewId = await GetNextIdAsync(Db.CourseReviews, r => r.Id);
                reviewEntity = new CourseReview
                {
                    Id = nextReviewId,
                    UserId = studentId,
                    CourseId = courseId,
                    Rating = request.Rating,
                    Comment = request.Comment?.Trim(),
                    CreatedAt = DateTime.UtcNow
                };
                Db.CourseReviews.Add(reviewEntity);
            }

            await Db.SaveChangesAsync();

            var studentUser = await Db.Users.AsNoTracking()
                .Where(u => u.Id == studentId)
                .Select(u => new { u.FullName, u.AvatarUrl })
                .FirstOrDefaultAsync();

            var dto = new CourseReviewItemDto
            {
                Id = reviewEntity.Id,
                CourseId = reviewEntity.CourseId,
                UserId = reviewEntity.UserId,
                UserFullName = studentUser?.FullName ?? "Học viên",
                UserAvatarUrl = studentUser?.AvatarUrl,
                Rating = reviewEntity.Rating,
                Comment = reviewEntity.Comment,
                CreatedAt = reviewEntity.CreatedAt
            };

            return ServiceResult<CourseReviewItemDto>.Created(dto, "Gửi đánh giá khóa học thành công.");
        }
    }
}
