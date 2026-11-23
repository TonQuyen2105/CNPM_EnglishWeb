using CNPM_EnglishLearning.Areas.Student.DTOs;
using CNPM_EnglishLearning.Areas.Student.DTOs.Common;
using CNPM_EnglishLearning.Areas.Student.Models;
using CNPM_EnglishLearning.Areas.Student.Services.Interfaces;
using CNPM_EnglishLearning.Data;
using Microsoft.EntityFrameworkCore;

namespace CNPM_EnglishLearning.Areas.Student.Services.Implementations
{
    /// <summary>
    /// Nhóm 3: Nghiệp vụ Lớp học của tôi & Lịch học cá nhân theo tuần.
    /// Kế thừa BaseStudentService để tái sử dụng các phương thức truy vấn gộp giảng viên, lịch cố định và tiến độ.
    /// </summary>
    public class StudentClassScheduleService : BaseStudentService, IStudentClassScheduleService
    {
        public StudentClassScheduleService(ApplicationDbContext db) : base(db)
        {
        }

        /// <summary>
        /// 3.1. Lấy danh sách lớp học của học sinh.
        /// Nghiệp vụ: Chỉ chấp nhận 2 trạng thái chuẩn hóa là "ongoing" (đang học) và "complete" (đã hoàn thành).
        /// Loại bỏ hoàn toàn các trạng thái khác (upcoming, cancelled,...).
        /// Hỗ trợ tìm kiếm theo từ khóa (tên lớp, mã lớp, tên khóa học, giảng viên) và lọc theo trạng thái (all, ongoing, complete).
        /// </summary>
        public async Task<ServiceResult<List<MyClassListItemDto>>> GetMyClassesAsync(int studentId, string? status, string? search = null)
        {
            // 1. Lấy các lớp học sinh có ghi danh
            var enrollmentQuery = Db.Enrollments.AsNoTracking()
                .Where(e => e.UserId == studentId);

            var rawClasses = await (
                from e in enrollmentQuery
                join cc in Db.CourseClasses.AsNoTracking() on e.ClassId equals cc.Id
                join c in Db.Courses.AsNoTracking() on cc.CourseId equals c.Id
                orderby e.EnrolledAt descending
                select new
                {
                    EnrollmentId = e.Id,
                    RawEnrollmentStatus = e.Status,
                    e.EnrolledAt,
                    ClassId = cc.Id,
                    cc.ClassCode,
                    cc.ClassName,
                    RawClassStatus = cc.Status,
                    cc.StartDate,
                    cc.EndDate,
                    CourseId = c.Id,
                    CourseTitle = c.Title,
                    c.ThumbnailUrl
                }
            ).ToListAsync();

            if (rawClasses.Count == 0)
            {
                return ServiceResult<List<MyClassListItemDto>>.Ok(new List<MyClassListItemDto>(), "Học sinh chưa tham gia lớp học nào.");
            }

            // 2. Chuẩn hóa trạng thái: "ongoing", "complete", hoặc "cancelled"
            var normalizedClasses = rawClasses.Select(item =>
            {
                var classStatusLower = item.RawClassStatus?.Trim().ToLowerInvariant() ?? "";
                var enrollStatusLower = item.RawEnrollmentStatus?.Trim().ToLowerInvariant() ?? "";

                string? normalizedStatus = null;
                if (classStatusLower == "cancelled" || classStatusLower == "canceled" ||
                    enrollStatusLower == "cancelled" || enrollStatusLower == "canceled")
                {
                    normalizedStatus = "cancelled";
                }
                else if (classStatusLower == "completed" || classStatusLower == "complete" ||
                         enrollStatusLower == "completed" || enrollStatusLower == "complete")
                {
                    normalizedStatus = "complete";
                }
                else if (classStatusLower == "ongoing" || classStatusLower == "active" || classStatusLower == "upcoming" ||
                         enrollStatusLower == "active" || enrollStatusLower == "ongoing")
                {
                    normalizedStatus = "ongoing";
                }

                return new
                {
                    Item = item,
                    NormalizedStatus = normalizedStatus
                };
            })
            .Where(x => x.NormalizedStatus != null)
            .ToList();

            // 3. Lọc theo trạng thái yêu cầu nếu có (all, ongoing, complete, cancelled)
            if (!string.IsNullOrWhiteSpace(status) && !string.Equals(status, "all", StringComparison.OrdinalIgnoreCase))
            {
                var targetStatus = status.Trim().ToLowerInvariant();
                if (targetStatus == "completed") targetStatus = "complete";
                if (targetStatus == "active") targetStatus = "ongoing";
                if (targetStatus == "canceled") targetStatus = "cancelled";

                normalizedClasses = normalizedClasses
                    .Where(x => x.NormalizedStatus == targetStatus)
                    .ToList();
            }

            // 4. Tái sử dụng danh sách ClassId và CourseId cho các truy vấn batch tối ưu (loại bỏ N+1 query)
            var classIds = normalizedClasses.Select(x => x.Item.ClassId).Distinct().ToList();
            var classToCourseMap = normalizedClasses
                .GroupBy(x => x.Item.ClassId)
                .ToDictionary(g => g.Key, g => g.First().Item.CourseId);

            var teachersByClass = await GetTeachersByClassIdsAsync(classIds);
            var schedulesByClass = await GetSchedulesByClassIdsAsync(classIds);
            var (totalByClass, completedByClass) = await GetClassProgressDictionariesAsync(classIds, classToCourseMap, studentId);

            // 5. Ánh xạ sang DTO kết quả chuẩn hóa
            var result = normalizedClasses.Select(x =>
            {
                var item = x.Item;
                var normalizedStatus = x.NormalizedStatus!;
                var totalLessons = totalByClass.GetValueOrDefault(item.ClassId, 0);
                var completedLessons = completedByClass.GetValueOrDefault(item.ClassId, 0);
                var classTeachers = teachersByClass.GetValueOrDefault(item.ClassId, new List<ClassTeacherInfoDto>());
                var classSchedules = schedulesByClass.GetValueOrDefault(item.ClassId, new List<ClassScheduleItemDto>());

                var scheduleSummary = classSchedules.Count > 0
                    ? string.Join(", ", classSchedules.Select(s => $"{s.DayName} ({s.TimeRange})"))
                    : null;

                return new MyClassListItemDto
                {
                    EnrollmentId = item.EnrollmentId,
                    EnrollmentStatus = normalizedStatus,
                    EnrolledAt = item.EnrolledAt,
                    ClassId = item.ClassId,
                    ClassCode = item.ClassCode,
                    ClassName = item.ClassName,
                    ClassStatus = normalizedStatus,
                    StartDate = item.StartDate,
                    EndDate = item.EndDate,
                    CourseId = item.CourseId,
                    CourseTitle = item.CourseTitle,
                    ThumbnailUrl = item.ThumbnailUrl,
                    Teachers = classTeachers,
                    Schedules = classSchedules,
                    ScheduleSummary = scheduleSummary,
                    TotalLessons = totalLessons,
                    CompletedLessons = completedLessons,
                    ProgressPercentage = CalculatePercentage(completedLessons, totalLessons)
                };
            }).ToList();

            // 6. Tìm kiếm theo từ khóa nếu có (search keyword)
            if (!string.IsNullOrWhiteSpace(search))
            {
                var keyword = search.Trim().ToLowerInvariant();
                result = result.Where(c =>
                    c.ClassCode.ToLowerInvariant().Contains(keyword) ||
                    c.ClassName.ToLowerInvariant().Contains(keyword) ||
                    c.CourseTitle.ToLowerInvariant().Contains(keyword) ||
                    c.Teachers.Any(t => t.FullName.ToLowerInvariant().Contains(keyword)) ||
                    (c.ScheduleSummary != null && c.ScheduleSummary.ToLowerInvariant().Contains(keyword))
                ).ToList();
            }

            return ServiceResult<List<MyClassListItemDto>>.Ok(result, "Lấy danh sách lớp học của học sinh thành công.");
        }

        /// <summary>
        /// 3.2. Xem chi tiết lớp học & Lộ trình các buổi học (class_lessons).
        /// </summary>
        public async Task<ServiceResult<MyClassDetailDto>> GetMyClassDetailAsync(int studentId, int classId)
        {
            var classInfo = await (
                from cc in Db.CourseClasses.AsNoTracking()
                join c in Db.Courses.AsNoTracking() on cc.CourseId equals c.Id
                where cc.Id == classId
                select new
                {
                    cc.Id,
                    cc.ClassCode,
                    cc.ClassName,
                    cc.Status,
                    cc.StartDate,
                    cc.EndDate,
                    CourseId = c.Id,
                    CourseTitle = c.Title
                }
            ).FirstOrDefaultAsync();

            if (classInfo == null)
            {
                return ServiceResult<MyClassDetailDto>.NotFound($"Không tìm thấy lớp học có ID = {classId}.");
            }

            var singleClassIdList = new[] { classId };
            var teachersByClass = await GetTeachersByClassIdsAsync(singleClassIdList);
            var schedulesByClass = await GetSchedulesByClassIdsAsync(singleClassIdList);

            // Lấy lộ trình buổi học từ bảng class_lessons
            var rawClassLessons = await (
                from cl in Db.ClassLessons.AsNoTracking()
                join l in Db.Lessons.AsNoTracking() on cl.LessonId equals l.Id into lGroup
                from l in lGroup.DefaultIfEmpty()
                join cs in Db.ClassSchedules.AsNoTracking() on cl.ScheduleId equals cs.Id into csGroup
                from cs in csGroup.DefaultIfEmpty()
                join ts in Db.TimeSlots.AsNoTracking() on cs.TimeSlotId equals ts.Id into tsGroup
                from ts in tsGroup.DefaultIfEmpty()
                join cr in Db.Classrooms.AsNoTracking() on cs.ClassroomId equals cr.Id into crGroup
                from cr in crGroup.DefaultIfEmpty()
                where cl.ClassId == classId
                orderby cl.ScheduleDate ?? DateTime.MaxValue, l != null ? l.OrderIndex : 0, cl.Id
                select new
                {
                    ClassLessonId = cl.Id,
                    cl.LessonId,
                    LessonTitle = l != null ? l.Title : $"Buổi học #{cl.Id}",
                    ContentType = l != null ? l.ContentType : "vocabulary",
                    OrderIndex = l != null ? l.OrderIndex : cl.Id,
                    cl.ScheduleDate,
                    SessionStatus = cl.Status,
                    RawStartTime = (TimeSpan?)ts.StartTime,
                    RawEndTime = (TimeSpan?)ts.EndTime,
                    ClassroomName = cr != null ? cr.Name : null,
                    cl.Notes
                }
            ).ToListAsync();

            // Lấy trạng thái hoàn thành bài học của học sinh cho khóa học này
            var studentProgressMap = (await (
                from p in Db.UserProgresses.AsNoTracking()
                join l in Db.Lessons.AsNoTracking() on p.LessonId equals l.Id
                where p.UserId == studentId && l.CourseId == classInfo.CourseId
                select p
            ).ToListAsync())
                .GroupBy(p => p.LessonId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.CompletedAt).First());

            List<ClassLessonTimelineItemDto> timelineDtos;

            if (rawClassLessons.Count > 0)
            {
                timelineDtos = rawClassLessons.Select(cl =>
                {
                    studentProgressMap.TryGetValue(cl.LessonId, out var progress);
                    var hasSlot = cl.RawStartTime.HasValue && cl.RawEndTime.HasValue;
                    return new ClassLessonTimelineItemDto
                    {
                        ClassLessonId = cl.ClassLessonId,
                        LessonId = cl.LessonId,
                        LessonTitle = cl.LessonTitle,
                        ContentType = cl.ContentType,
                        OrderIndex = cl.OrderIndex,
                        ScheduleDate = cl.ScheduleDate,
                        SessionStatus = cl.SessionStatus,
                        TimeSlot = hasSlot ? FormatTimeRange(cl.RawStartTime!.Value, cl.RawEndTime!.Value) : null,
                        Classroom = cl.ClassroomName,
                        Notes = cl.Notes,
                        IsCompleted = progress?.IsCompleted ?? false,
                        StudentScore = progress?.Score
                    };
                }).ToList();
            }
            else
            {
                // Nếu lớp chưa có bản ghi trong class_lessons, tự động tổng hợp lộ trình từ danh sách bài học của khóa
                var courseLessons = await Db.Lessons.AsNoTracking()
                    .Where(l => l.CourseId == classInfo.CourseId)
                    .OrderBy(l => l.OrderIndex)
                    .ToListAsync();

                var weeklySchedules = schedulesByClass.GetValueOrDefault(classId, new List<ClassScheduleItemDto>());
                var firstSchedule = weeklySchedules.FirstOrDefault();

                timelineDtos = courseLessons.Select((l, index) =>
                {
                    studentProgressMap.TryGetValue(l.Id, out var progress);
                    return new ClassLessonTimelineItemDto
                    {
                        ClassLessonId = l.Id,
                        LessonId = l.Id,
                        LessonTitle = l.Title,
                        ContentType = l.ContentType ?? string.Empty,
                        OrderIndex = l.OrderIndex,
                        ScheduleDate = classInfo.StartDate.Date.AddDays(index * 2),
                        SessionStatus = (progress?.IsCompleted ?? false) ? SessionCompleted : SessionScheduled,
                        TimeSlot = firstSchedule?.TimeRange,
                        Classroom = firstSchedule?.ClassroomName,
                        Notes = null,
                        IsCompleted = progress?.IsCompleted ?? false,
                        StudentScore = progress?.Score
                    };
                }).ToList();
            }

            var detailDto = new MyClassDetailDto
            {
                ClassId = classInfo.Id,
                ClassCode = classInfo.ClassCode,
                ClassName = classInfo.ClassName,
                Status = classInfo.Status,
                StartDate = classInfo.StartDate,
                EndDate = classInfo.EndDate,
                CourseId = classInfo.CourseId,
                CourseTitle = classInfo.CourseTitle,
                Teachers = teachersByClass.GetValueOrDefault(classId, new List<ClassTeacherInfoDto>()),
                WeeklySchedules = schedulesByClass.GetValueOrDefault(classId, new List<ClassScheduleItemDto>()),
                ClassLessons = timelineDtos
            };

            return ServiceResult<MyClassDetailDto>.Ok(detailDto, "Lấy thông tin chi tiết lớp học thành công.");
        }

        /// <summary>
        /// 3.3. Xem chi tiết buổi học tiếp theo (hoặc buổi học bất kỳ trong lớp).
        /// </summary>
        public async Task<ServiceResult<ClassLessonSessionDetailDto>> GetClassLessonDetailAsync(
            int studentId,
            int classId,
            int classLessonId)
        {
            var sessionData = await (
                from cl in Db.ClassLessons.AsNoTracking()
                join cc in Db.CourseClasses.AsNoTracking() on cl.ClassId equals cc.Id
                join l in Db.Lessons.AsNoTracking() on cl.LessonId equals l.Id into lGroup
                from l in lGroup.DefaultIfEmpty()
                join cs in Db.ClassSchedules.AsNoTracking() on cl.ScheduleId equals cs.Id into csGroup
                from cs in csGroup.DefaultIfEmpty()
                join ts in Db.TimeSlots.AsNoTracking() on cs.TimeSlotId equals ts.Id into tsGroup
                from ts in tsGroup.DefaultIfEmpty()
                join cr in Db.Classrooms.AsNoTracking() on cs.ClassroomId equals cr.Id into crGroup
                from cr in crGroup.DefaultIfEmpty()
                join t in Db.Teachers.AsNoTracking() on cl.TeacherId equals t.Id into tGroup
                from t in tGroup.DefaultIfEmpty()
                join u in Db.Users.AsNoTracking() on t.UserId equals u.Id into uGroup
                from u in uGroup.DefaultIfEmpty()
                where cl.ClassId == classId && cl.Id == classLessonId
                select new
                {
                    ClassLessonId = cl.Id,
                    cl.ClassId,
                    cc.ClassName,
                    cl.LessonId,
                    LessonTitle = l != null ? l.Title : $"Buổi học #{cl.Id}",
                    ContentType = l != null ? l.ContentType : "vocabulary",
                    Content = l != null ? l.Content : null,
                    cl.ScheduleDate,
                    cl.Status,
                    RawStartTime = (TimeSpan?)ts.StartTime,
                    RawEndTime = (TimeSpan?)ts.EndTime,
                    ClassroomName = cr != null ? cr.Name : "Phòng học trực tuyến / Zoom",
                    Location = cr != null ? cr.Location : "Online",
                    TeacherName = u != null ? u.FullName : null,
                    cl.Notes
                }
            ).FirstOrDefaultAsync();

            if (sessionData != null)
            {
                var isCompleted = await Db.UserProgresses.AsNoTracking()
                    .AnyAsync(p => p.UserId == studentId && p.LessonId == sessionData.LessonId && p.IsCompleted);

                var startTime = sessionData.RawStartTime ?? new TimeSpan(18, 0, 0);
                var endTime = sessionData.RawEndTime ?? new TimeSpan(20, 0, 0);

                var dto = new ClassLessonSessionDetailDto
                {
                    ClassLessonId = sessionData.ClassLessonId,
                    ClassId = sessionData.ClassId,
                    ClassName = sessionData.ClassName,
                    LessonId = sessionData.LessonId,
                    LessonTitle = sessionData.LessonTitle,
                    ContentType = sessionData.ContentType,
                    Content = ParseLessonContent(sessionData.Content).TextContent ?? sessionData.Content,
                    ScheduleDate = sessionData.ScheduleDate,
                    Status = sessionData.Status,
                    TimeSlot = FormatTimeRange(startTime, endTime),
                    Classroom = sessionData.ClassroomName,
                    Location = sessionData.Location,
                    TeacherName = sessionData.TeacherName,
                    Notes = sessionData.Notes,
                    IsCompleted = isCompleted || sessionData.Status == SessionCompleted,
                    Attachments = StudentExtendedStore.GetLessonAttachments(
                        sessionData.LessonId,
                        sessionData.LessonTitle,
                        sessionData.ContentType)
                };

                return ServiceResult<ClassLessonSessionDetailDto>.Ok(dto, "Lấy chi tiết buổi học thành công.");
            }

            // Fallback: Nếu classLessonId truyền vào là LessonId thuộc khóa học của lớp đó
            var fallbackLesson = await (
                from cc in Db.CourseClasses.AsNoTracking()
                join l in Db.Lessons.AsNoTracking() on cc.CourseId equals l.CourseId
                where cc.Id == classId && l.Id == classLessonId
                select new
                {
                    cc.Id,
                    cc.ClassName,
                    cc.StartDate,
                    Lesson = l
                }
            ).FirstOrDefaultAsync();

            if (fallbackLesson == null)
            {
                return ServiceResult<ClassLessonSessionDetailDto>.NotFound(
                    $"Không tìm thấy buổi học ID = {classLessonId} trong lớp ID = {classId}.");
            }

            var schedulesByClass = await GetSchedulesByClassIdsAsync(new[] { classId });
            var teachersByClass = await GetTeachersByClassIdsAsync(new[] { classId });
            var defaultSchedule = schedulesByClass.GetValueOrDefault(classId)?.FirstOrDefault();
            var defaultTeacher = teachersByClass.GetValueOrDefault(classId)?.FirstOrDefault();

            var lessonCompleted = await Db.UserProgresses.AsNoTracking()
                .AnyAsync(p => p.UserId == studentId && p.LessonId == fallbackLesson.Lesson.Id && p.IsCompleted);

            var fallbackDto = new ClassLessonSessionDetailDto
            {
                ClassLessonId = classLessonId,
                ClassId = classId,
                ClassName = fallbackLesson.ClassName,
                LessonId = fallbackLesson.Lesson.Id,
                LessonTitle = fallbackLesson.Lesson.Title,
                ContentType = fallbackLesson.Lesson.ContentType ?? string.Empty,
                Content = ParseLessonContent(fallbackLesson.Lesson.Content).TextContent ?? fallbackLesson.Lesson.Content,
                ScheduleDate = fallbackLesson.StartDate.Date,
                Status = lessonCompleted ? SessionCompleted : SessionScheduled,
                TimeSlot = defaultSchedule?.TimeRange ?? "18:00 - 20:00",
                Classroom = defaultSchedule?.ClassroomName ?? "Phòng học trực tuyến / Zoom",
                Location = defaultSchedule?.Location ?? "Online",
                TeacherName = defaultTeacher?.FullName,
                Notes = "Học viên xem trước nội dung bài học và tài liệu đính kèm trước khi vào lớp.",
                IsCompleted = lessonCompleted,
                Attachments = StudentExtendedStore.GetLessonAttachments(
                    fallbackLesson.Lesson.Id,
                    fallbackLesson.Lesson.Title,
                    fallbackLesson.Lesson.ContentType ?? string.Empty)
            };

            return ServiceResult<ClassLessonSessionDetailDto>.Ok(fallbackDto, "Lấy chi tiết buổi học thành công.");
        }

        /// <summary>
        /// 3.4. Xem lịch học cá nhân theo tuần (?start_date=YYYY-MM-DD&end_date=YYYY-MM-DD).
        /// </summary>
        public async Task<ServiceResult<WeeklyScheduleResponseDto>> GetWeeklyScheduleAsync(
            int studentId,
            DateTime? startDate,
            DateTime? endDate)
        {
            var today = DateTime.UtcNow.Date;
            var offsetToMonday = ((int)today.DayOfWeek + 6) % 7;
            var rawStart = (startDate?.Date ?? today.AddDays(-offsetToMonday));
            var rawEnd = (endDate?.Date ?? rawStart.AddDays(6));

            if (rawEnd < rawStart)
            {
                (rawStart, rawEnd) = (rawEnd, rawStart);
            }

            var resolvedStart = DateTime.SpecifyKind(rawStart, DateTimeKind.Utc);
            var resolvedEnd = DateTime.SpecifyKind(rawEnd, DateTimeKind.Utc);
            var endExclusive = resolvedEnd.AddDays(1);

            // 1. Lấy các lớp học sinh đang theo học (loại trừ các lớp completed / complete hoặc cancelled / canceled)
            var activeClasses = await (
                from e in Db.Enrollments.AsNoTracking()
                join cc in Db.CourseClasses.AsNoTracking() on e.ClassId equals cc.Id
                join c in Db.Courses.AsNoTracking() on cc.CourseId equals c.Id
                where e.UserId == studentId 
                      && (e.Status.ToLower() == EnrollmentActive || e.Status.ToLower() == "enrolled" || e.Status.ToLower() == "ongoing")
                      && cc.Status.ToLower() != "completed" && cc.Status.ToLower() != "complete"
                      && cc.Status.ToLower() != "cancelled" && cc.Status.ToLower() != "canceled"
                      && e.Status.ToLower() != "completed" && e.Status.ToLower() != "cancelled"
                select new
                {
                    ClassId = cc.Id,
                    cc.ClassCode,
                    cc.ClassName,
                    cc.StartDate,
                    cc.EndDate,
                    CourseId = c.Id,
                    CourseTitle = c.Title
                }
            ).ToListAsync();

            var enrolledClassIds = activeClasses.Select(x => x.ClassId).Distinct().ToList();
            var sessions = new List<StudentScheduleSessionItemDto>();

            if (enrolledClassIds.Count > 0)
            {
                // Tối ưu truy vấn giáo viên theo danh sách lớp
                var teachersByClass = await GetTeachersByClassIdsAsync(enrolledClassIds);

                // 2. Lấy các buổi học cụ thể trong bảng class_lessons nằm trong tuần được chọn
                var explicitSessions = await (
                    from cl in Db.ClassLessons.AsNoTracking()
                    join cc in Db.CourseClasses.AsNoTracking() on cl.ClassId equals cc.Id
                    join c in Db.Courses.AsNoTracking() on cc.CourseId equals c.Id
                    join l in Db.Lessons.AsNoTracking() on cl.LessonId equals l.Id into lGroup
                    from l in lGroup.DefaultIfEmpty()
                    join cs in Db.ClassSchedules.AsNoTracking() on cl.ScheduleId equals cs.Id into csGroup
                    from cs in csGroup.DefaultIfEmpty()
                    join ts in Db.TimeSlots.AsNoTracking() on cs.TimeSlotId equals ts.Id into tsGroup
                    from ts in tsGroup.DefaultIfEmpty()
                    join cr in Db.Classrooms.AsNoTracking() on cs.ClassroomId equals cr.Id into crGroup
                    from cr in crGroup.DefaultIfEmpty()
                    join t in Db.Teachers.AsNoTracking() on cl.TeacherId equals t.Id into tGroup
                    from t in tGroup.DefaultIfEmpty()
                    join u in Db.Users.AsNoTracking() on t.UserId equals u.Id into uGroup
                    from u in uGroup.DefaultIfEmpty()
                    where enrolledClassIds.Contains(cl.ClassId)
                          && cl.ScheduleDate != null
                          && cl.ScheduleDate >= resolvedStart
                          && cl.ScheduleDate < endExclusive
                    orderby cl.ScheduleDate, cl.Id
                    select new
                    {
                        ClassLessonId = cl.Id,
                        cl.ScheduleId,
                        cl.ClassId,
                        cc.ClassCode,
                        cc.ClassName,
                        CourseTitle = c.Title,
                        cl.LessonId,
                        LessonTitle = l != null ? l.Title : $"Buổi học lớp {cc.ClassName}",
                        ScheduleDate = cl.ScheduleDate!.Value,
                        DayOfWeek = cs != null ? cs.DayOfWeek : 0,
                        RawStartTime = (TimeSpan?)ts.StartTime,
                        RawEndTime = (TimeSpan?)ts.EndTime,
                        ClassroomName = cr != null ? cr.Name : "Phòng học trực tuyến / Zoom",
                        Location = cr != null ? cr.Location : "Online",
                        TeacherName = u != null ? u.FullName : null,
                        cl.Status,
                        cl.Notes
                    }
                ).ToListAsync();

                foreach (var item in explicitSessions)
                {
                    var dayOfWeek = item.DayOfWeek > 0
                        ? IsoToScheduleDayOfWeek(item.DayOfWeek)
                        : ToScheduleDayOfWeek(item.ScheduleDate.DayOfWeek);

                    var startTime = item.RawStartTime ?? new TimeSpan(18, 0, 0);
                    var endTime = item.RawEndTime ?? new TimeSpan(20, 0, 0);
                    var teacherName = item.TeacherName ?? teachersByClass.GetValueOrDefault(item.ClassId)?.FirstOrDefault()?.FullName;

                    sessions.Add(new StudentScheduleSessionItemDto
                    {
                        ClassLessonId = item.ClassLessonId,
                        ScheduleId = item.ScheduleId,
                        ClassId = item.ClassId,
                        ClassCode = item.ClassCode,
                        ClassName = item.ClassName,
                        CourseTitle = item.CourseTitle,
                        LessonId = item.LessonId,
                        LessonTitle = item.LessonTitle,
                        ScheduleDate = item.ScheduleDate.ToString("yyyy-MM-dd"),
                        DayOfWeek = dayOfWeek,
                        TimeSlot = FormatTimeRange(startTime, endTime),
                        StartTime = startTime.ToString(@"hh\:mm"),
                        EndTime = endTime.ToString(@"hh\:mm"),
                        Classroom = item.ClassroomName,
                        Location = item.Location,
                        TeacherName = teacherName,
                        Status = item.Status,
                        Notes = item.Notes
                    });
                }

                // 3. Bổ sung các buổi học định kỳ từ class_schedules trong tuần nếu ngày đó chưa có bản ghi class_lessons
                var recurringSchedules = await (
                    from cs in Db.ClassSchedules.AsNoTracking()
                    join ts in Db.TimeSlots.AsNoTracking() on cs.TimeSlotId equals ts.Id into tsGroup
                    from ts in tsGroup.DefaultIfEmpty()
                    join cr in Db.Classrooms.AsNoTracking() on cs.ClassroomId equals cr.Id into crGroup
                    from cr in crGroup.DefaultIfEmpty()
                    where enrolledClassIds.Contains(cs.ClassId)
                    select new
                    {
                        ScheduleId = cs.Id,
                        cs.ClassId,
                        cs.DayOfWeek,
                        RawStartTime = (TimeSpan?)ts.StartTime,
                        RawEndTime = (TimeSpan?)ts.EndTime,
                        ClassroomName = cr != null ? cr.Name : "Phòng học trực tuyến / Zoom",
                        Location = cr != null ? cr.Location : "Online"
                    }
                ).ToListAsync();

                var classMap = activeClasses.ToDictionary(c => c.ClassId);
                var existingKeys = sessions
                    .Select(s => $"{s.ClassId}:{s.ScheduleDate}")
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var totalDays = (int)(rawEnd - rawStart).TotalDays + 1;
                if (totalDays <= 62) // Giới hạn sinh lịch tuần hợp lý
                {
                    for (var dayIndex = 0; dayIndex < totalDays; dayIndex++)
                    {
                        var currentDate = rawStart.AddDays(dayIndex);
                        var currentIsoDow = ToIsoDayOfWeek(currentDate.DayOfWeek);
                        var currentScheduleDow = IsoToScheduleDayOfWeek(currentIsoDow);

                        foreach (var sched in recurringSchedules.Where(s => s.DayOfWeek == currentIsoDow))
                        {
                            if (!classMap.TryGetValue(sched.ClassId, out var cls)) continue;
                            if (currentDate < cls.StartDate.Date || currentDate > cls.EndDate.Date) continue;

                            var dateStr = currentDate.ToString("yyyy-MM-dd");
                            var compositeKey = $"{sched.ClassId}:{dateStr}";
                            if (existingKeys.Contains(compositeKey)) continue;

                            existingKeys.Add(compositeKey);
                            var startTime = sched.RawStartTime ?? new TimeSpan(18, 0, 0);
                            var endTime = sched.RawEndTime ?? new TimeSpan(20, 0, 0);
                            var teacherName = teachersByClass.GetValueOrDefault(cls.ClassId)?.FirstOrDefault()?.FullName;

                            sessions.Add(new StudentScheduleSessionItemDto
                            {
                                ClassLessonId = null,
                                ScheduleId = sched.ScheduleId,
                                ClassId = cls.ClassId,
                                ClassCode = cls.ClassCode,
                                ClassName = cls.ClassName,
                                CourseTitle = cls.CourseTitle,
                                LessonId = null,
                                LessonTitle = $"Buổi học định kỳ - {cls.ClassName}",
                                ScheduleDate = dateStr,
                                DayOfWeek = currentScheduleDow,
                                TimeSlot = FormatTimeRange(startTime, endTime),
                                StartTime = startTime.ToString(@"hh\:mm"),
                                EndTime = endTime.ToString(@"hh\:mm"),
                                Classroom = sched.ClassroomName,
                                Location = sched.Location,
                                TeacherName = teacherName,
                                Status = currentDate < today ? SessionCompleted : SessionScheduled,
                                Notes = null
                            });
                        }
                    }
                }
            }

            sessions = sessions
                .OrderBy(s => s.ScheduleDate)
                .ThenBy(s => s.StartTime)
                .ToList();

            var response = new WeeklyScheduleResponseDto
            {
                StartDate = rawStart.ToString("yyyy-MM-dd"),
                EndDate = rawEnd.ToString("yyyy-MM-dd"),
                Sessions = sessions
            };

            return ServiceResult<WeeklyScheduleResponseDto>.Ok(response, "Lấy lịch học cá nhân theo tuần thành công.");
        }
    }
}
