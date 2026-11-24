using System.Globalization;
using System.Text.Json;
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
    public class ExamsController : Controller
    {
        private const int MaxTitleLength = 200;
        private const int MaxDescriptionLength = 1700;
        private const string RoomWindowPrefix = "EL_EXAM_ROOM_V1 ";

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
                var nowUtc = DateTime.UtcNow;

                // Tự động đóng các phòng thi đã quá thời gian kết thúc (CloseAt <= nowUtc)
                await AutoCloseExpiredRoomsAsync(nowUtc, cancellationToken);

                var query =
                    from e in _context.Exams.AsNoTracking()
                    join c in _context.Courses.AsNoTracking() on e.CourseId equals c.Id into courseGroup
                    from course in courseGroup.DefaultIfEmpty()
                    join s in _context.Subjects.AsNoTracking() on course.SubjectId equals s.Id into subjGroup
                    from subj in subjGroup.DefaultIfEmpty()
                    select new
                    {
                        id = e.Id,
                        title = e.Title,
                        rawDescription = e.Description,
                        courseId = e.CourseId,
                        courseTitle = course != null ? course.Title : "Đánh giá chung",
                        subjectName = subj != null ? subj.Name : "Chung",
                        durationMinutes = e.DurationMinutes,
                        passScore = e.PassScore,
                        maxAttempts = e.MaxAttempts,
                        isPublished = e.IsPublished,
                        createdAt = e.CreatedAt,
                        updatedAt = e.UpdatedAt,
                        questionCount = _context.ExamQuestions.Count(eq => eq.ExamId == e.Id),
                        attemptCount = _context.UserExams.Count(ue => ue.ExamId == e.Id),
                        passedCount = _context.UserExams.Count(ue => ue.ExamId == e.Id && ue.Status == "passed")
                    };

                if (courseId.HasValue && courseId.Value > 0)
                {
                    query = query.Where(x => x.courseId == courseId.Value);
                }

                var rawItems = await query
                    .OrderByDescending(x => x.isPublished)
                    .ThenByDescending(x => x.id)
                    .ToListAsync(cancellationToken);

                var courseIds = rawItems
                    .Where(x => x.courseId.HasValue)
                    .Select(x => x.courseId!.Value)
                    .Distinct()
                    .ToList();

                var courseTeachersRaw = courseIds.Count == 0
                    ? new()
                    : await (
                        from cc in _context.CourseClasses.AsNoTracking()
                        join ct in _context.ClassTeachers.AsNoTracking() on cc.Id equals ct.ClassId
                        join t in _context.Teachers.AsNoTracking() on ct.TeacherId equals t.Id
                        join u in _context.Users.AsNoTracking() on t.UserId equals u.Id
                        where courseIds.Contains(cc.CourseId) && cc.Status != "cancelled" && u.IsActive
                        select new
                        {
                            cc.CourseId,
                            teacherId = t.Id,
                            fullName = u.FullName
                        }
                    ).Distinct().ToListAsync(cancellationToken);

                var teachersByCourse = courseTeachersRaw
                    .GroupBy(x => x.CourseId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.OrderBy(x => x.fullName).Select(x => x.fullName).Distinct().ToList()
                    );

                var items = rawItems.Select(x =>
                {
                    var openAtUtc = ToUtcNullable(x.createdAt);
                    var closeAtUtc = ToUtcNullable(x.updatedAt);
                    var cleanNote = ParseRoomDescription(x.rawDescription).Note;
                    var roomState = ComputeRoomState(x.isPublished, x.questionCount, openAtUtc, closeAtUtc, nowUtc);
                    var tNames = x.courseId.HasValue && teachersByCourse.TryGetValue(x.courseId.Value, out var list)
                        ? list
                        : new List<string>();

                    return new
                    {
                        x.id,
                        x.title,
                        description = cleanNote,
                        openAt = openAtUtc,
                        closeAt = closeAtUtc,
                        roomState = roomState.Code,
                        roomStateLabel = roomState.Label,
                        isExpired = roomState.IsExpired,
                        isOpenNow = roomState.IsOpenNow,
                        x.courseId,
                        x.courseTitle,
                        x.subjectName,
                        teacherNames = tNames,
                        teacherSummary = tNames.Count > 0
                            ? string.Join(", ", tNames)
                            : "Chưa phân công giảng viên cho lớp thuộc môn",
                        x.durationMinutes,
                        x.passScore,
                        x.maxAttempts,
                        x.isPublished,
                        createdAt = openAtUtc,
                        updatedAt = closeAtUtc,
                        x.questionCount,
                        x.attemptCount,
                        x.passedCount
                    };
                }).ToList();

                if (!string.IsNullOrWhiteSpace(published) && !published.Equals("all", StringComparison.OrdinalIgnoreCase))
                {
                    var norm = published.Trim().ToLowerInvariant();
                    items = norm switch
                    {
                        "open" or "true" or "1" => items.Where(x => x.roomState == "open").ToList(),
                        "scheduled" => items.Where(x => x.roomState == "scheduled").ToList(),
                        "expired" => items.Where(x => x.roomState == "expired").ToList(),
                        "draft" or "false" or "0" => items.Where(x => !x.isPublished && !x.isExpired).ToList(),
                        _ => items
                    };
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var term = search.Trim().ToLowerInvariant();
                    items = items.Where(x =>
                        x.title.ToLowerInvariant().Contains(term) ||
                        x.courseTitle.ToLowerInvariant().Contains(term) ||
                        x.subjectName.ToLowerInvariant().Contains(term) ||
                        x.teacherSummary.ToLowerInvariant().Contains(term)
                    ).ToList();
                }

                var totalExams = await _context.Exams.AsNoTracking().CountAsync(cancellationToken);
                var publishedExams = await _context.Exams.AsNoTracking().CountAsync(e => e.IsPublished, cancellationToken);
                var totalQuestions = await _context.ExamQuestions.AsNoTracking().CountAsync(cancellationToken);
                var totalAttempts = await _context.UserExams.AsNoTracking().CountAsync(cancellationToken);
                var passedAttempts = await _context.UserExams.AsNoTracking().CountAsync(ue => ue.Status == "passed", cancellationToken);

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
                            subjectName = subj != null ? subj.Name : null,
                            teacherCount = (
                                from cc in _context.CourseClasses
                                join ct in _context.ClassTeachers on cc.Id equals ct.ClassId
                                where cc.CourseId == c.Id && cc.Status != "cancelled"
                                select ct.TeacherId
                            ).Distinct().Count()
                        }
                    ).ToListAsync(cancellationToken)
                    : null;

                return Ok(new
                {
                    success = true,
                    serverNowUtc = nowUtc,
                    items,
                    courses,
                    stats = new
                    {
                        totalExams,
                        publishedExams,
                        draftExams = totalExams - publishedExams,
                        totalQuestions,
                        totalAttempts,
                        passedAttempts
                    }
                });
            }
            catch (OperationCanceledException)
            {
                return this.ApiCancelled();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể tải danh sách phòng thi.");
                return this.ApiServerError("Không thể tải danh sách phòng thi.");
            }
        }

        [HttpPost("/Admin/api/Exams/Create")]
        public async Task<IActionResult> Create([FromBody] ExamUpsertDto? dto, CancellationToken cancellationToken)
        {
            var error = ValidateExam(dto, out var title, out var note, out var openAtUtc, out var closeAtUtc);
            if (error != null)
            {
                return this.ApiBadRequest(error);
            }

            try
            {
                if (!dto!.CourseId.HasValue || dto.CourseId.Value <= 0)
                {
                    return this.ApiBadRequest("Vui lòng chọn môn học để mở phòng thi.");
                }

                var courseExists = await _context.Courses
                    .AsNoTracking()
                    .AnyAsync(c => c.Id == dto.CourseId.Value, cancellationToken);
                if (!courseExists)
                {
                    return this.ApiBadRequest("Môn học được chọn không tồn tại.");
                }

                var nowUtc = DateTime.UtcNow;
                if (closeAtUtc.HasValue && closeAtUtc.Value <= nowUtc)
                {
                    return this.ApiBadRequest("Thời gian kết thúc phòng thi phải lớn hơn thời điểm hiện tại.");
                }

                var representativeTeacherId = await ResolveRepresentativeTeacherIdAsync(dto.CourseId.Value, cancellationToken);
                if (representativeTeacherId <= 0)
                {
                    return this.ApiBadRequest("Hệ thống chưa có hồ sơ giảng viên hoạt động nào để liên kết môn học.");
                }

                var openAtDb = await ResolveDbTimestampAsync(openAtUtc ?? nowUtc, cancellationToken);
                var closeAtDb = closeAtUtc.HasValue
                    ? (openAtDb.Kind == DateTimeKind.Utc ? closeAtUtc.Value : DateTime.SpecifyKind(closeAtUtc.Value, DateTimeKind.Unspecified))
                    : (DateTime?)null;

                await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

                var exam = new Exam
                {
                    Title = title,
                    CourseId = dto.CourseId.Value,
                    CreatedByTeacherId = representativeTeacherId,
                    Description = note,
                    DurationMinutes = 45,
                    PassScore = 5.0m,
                    MaxAttempts = null,
                    IsPublished = dto.IsPublished ?? false,
                    CreatedAt = openAtDb,
                    UpdatedAt = closeAtDb
                };

                _context.Exams.Add(exam);
                try
                {
                    await _context.SaveChangesAsync(cancellationToken);

                    // Đảm bảo review_status = NULL khi Admin mở phòng thi cho môn học để giảng viên thêm đề
                    await _context.Database.ExecuteSqlInterpolatedAsync(
                        $"UPDATE exams SET review_status = NULL WHERE id = {exam.Id}",
                        cancellationToken);

                    await transaction.CommitAsync(cancellationToken);
                }
                catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation })
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiBadRequest("Môn học được chọn không còn tồn tại.");
                }

                return Ok(new
                {
                    success = true,
                    message = exam.IsPublished
                        ? "Đã mở phòng thi theo khung giờ quy định."
                        : "Đã tạo phòng thi và lịch đóng/mở (chờ giảng viên soạn đề).",
                    id = exam.Id
                });
            }
            catch (OperationCanceledException)
            {
                return this.ApiCancelled();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể tạo phòng thi mới.");
                return this.ApiServerError($"Không thể tạo phòng thi mới: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        [HttpPost("/Admin/api/Exams/Update/{id}")]
        [HttpPut("/Admin/api/Exams/Update/{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] ExamUpsertDto? dto, CancellationToken cancellationToken)
        {
            if (id <= 0)
            {
                return this.ApiBadRequest("Mã phòng thi không hợp lệ.");
            }

            var error = ValidateExam(dto, out var title, out var note, out var openAtUtc, out var closeAtUtc);
            if (error != null)
            {
                return this.ApiBadRequest(error);
            }

            try
            {
                var exam = await _context.Exams
                    .AsNoTracking()
                    .Where(e => e.Id == id)
                    .Select(e => new { e.Id, e.CourseId, e.CreatedByTeacherId, e.IsPublished, e.CreatedAt })
                    .FirstOrDefaultAsync(cancellationToken);

                if (exam == null)
                {
                    return this.ApiNotFound("Không tìm thấy phòng thi.");
                }

                if (!dto!.CourseId.HasValue || dto.CourseId.Value <= 0)
                {
                    return this.ApiBadRequest("Vui lòng chọn môn học cho phòng thi.");
                }

                var courseExists = await _context.Courses
                    .AsNoTracking()
                    .AnyAsync(c => c.Id == dto.CourseId.Value, cancellationToken);
                if (!courseExists)
                {
                    return this.ApiBadRequest("Môn học được chọn không tồn tại.");
                }

                var nowUtc = DateTime.UtcNow;
                var isExpired = closeAtUtc.HasValue && closeAtUtc.Value <= nowUtc;

                if (dto.IsPublished == true)
                {
                    if (isExpired)
                    {
                        return this.ApiBadRequest("Thời gian đóng phòng thi đã qua. Vui lòng gia hạn thời gian kết thúc trước khi mở phòng thi.");
                    }

                    var hasQuestions = await _context.ExamQuestions
                        .AsNoTracking()
                        .AnyAsync(eq => eq.ExamId == id, cancellationToken);
                    if (!hasQuestions)
                    {
                        return this.ApiBadRequest("Giảng viên của môn học chưa tạo câu hỏi cho phòng thi này nên chưa thể mở thi.");
                    }
                }

                var targetTeacherId = exam.CreatedByTeacherId;
                if (exam.CourseId != dto.CourseId.Value)
                {
                    var repTeacherId = await ResolveRepresentativeTeacherIdAsync(dto.CourseId.Value, cancellationToken);
                    if (repTeacherId > 0)
                    {
                        targetTeacherId = repTeacherId;
                    }
                }

                var nextPublished = isExpired
                    ? false
                    : (dto.IsPublished ?? exam.IsPublished);

                var openAtDb = exam.CreatedAt.Kind == DateTimeKind.Utc
                    ? (openAtUtc ?? nowUtc)
                    : DateTime.SpecifyKind(openAtUtc ?? nowUtc, DateTimeKind.Unspecified);

                var closeAtDb = closeAtUtc.HasValue
                    ? (exam.CreatedAt.Kind == DateTimeKind.Utc
                        ? closeAtUtc.Value
                        : DateTime.SpecifyKind(closeAtUtc.Value, DateTimeKind.Unspecified))
                    : (DateTime?)null;

                // Lưu thời gian bắt đầu vào created_at, thời gian kết thúc vào updated_at và mô tả thuần vào description
                await _context.Database.ExecuteSqlInterpolatedAsync(
                    $@"UPDATE exams
                       SET title = {title},
                           course_id = {dto.CourseId.Value},
                           created_by_teacher_id = {targetTeacherId},
                           description = {note},
                           is_published = {nextPublished},
                           review_status = CASE WHEN {nextPublished} THEN NULL ELSE review_status END,
                           created_at = {openAtDb},
                           updated_at = {closeAtDb}
                       WHERE id = {id}",
                    cancellationToken);

                return Ok(new
                {
                    success = true,
                    message = isExpired
                        ? "Đã cập nhật phòng thi (tự động đóng vì đã quá giờ kết thúc)."
                        : "Cập nhật thông tin & khung giờ phòng thi thành công.",
                    id = exam.Id
                });
            }
            catch (OperationCanceledException)
            {
                return this.ApiCancelled();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể cập nhật phòng thi {ExamId}.", id);
                return this.ApiServerError($"Không thể cập nhật phòng thi: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        [HttpPost("/Admin/api/Exams/TogglePublish/{id}")]
        public async Task<IActionResult> TogglePublish(int id, CancellationToken cancellationToken)
        {
            if (id <= 0)
            {
                return this.ApiBadRequest("Mã phòng thi không hợp lệ.");
            }

            try
            {
                var nowUtc = DateTime.UtcNow;
                await AutoCloseExpiredRoomsAsync(nowUtc, cancellationToken);

                var exam = await _context.Exams
                    .AsNoTracking()
                    .Where(e => e.Id == id)
                    .Select(e => new
                    {
                        e.Id,
                        e.Title,
                        e.Description,
                        e.IsPublished,
                        e.CreatedAt,
                        e.UpdatedAt
                    })
                    .FirstOrDefaultAsync(cancellationToken);

                if (exam == null)
                {
                    return this.ApiNotFound("Không tìm thấy phòng thi.");
                }

                var openAtUtc = ToUtcNullable(exam.CreatedAt);
                var closeAtUtc = ToUtcNullable(exam.UpdatedAt);

                if (!exam.IsPublished)
                {
                    if (closeAtUtc.HasValue && closeAtUtc.Value <= nowUtc)
                    {
                        return this.ApiBadRequest("Phòng thi đã hết thời gian mở thi. Vui lòng bấm Sửa và gia hạn thời gian kết thúc trước khi mở lại.");
                    }

                    var hasQuestions = await _context.ExamQuestions
                        .AsNoTracking()
                        .AnyAsync(eq => eq.ExamId == id, cancellationToken);
                    if (!hasQuestions)
                    {
                        return this.ApiBadRequest("Giảng viên của môn học chưa tạo câu hỏi trong phòng thi này nên chưa thể mở công bố.");
                    }
                }

                var nextPublished = !exam.IsPublished;

                // Không ghi đè updated_at vì updated_at lưu thời gian kết thúc phòng thi (CloseAt)
                await _context.Database.ExecuteSqlInterpolatedAsync(
                    $@"UPDATE exams
                       SET is_published = {nextPublished},
                           review_status = CASE WHEN {nextPublished} THEN NULL ELSE review_status END
                       WHERE id = {id}",
                    cancellationToken);

                var state = ComputeRoomState(nextPublished, 1, openAtUtc, closeAtUtc, nowUtc);
                var message = nextPublished
                    ? (state.Code == "scheduled"
                        ? $"Đã kích hoạt phòng thi \"{exam.Title}\" (sẽ tự mở cho học viên vào giờ quy định)."
                        : $"Đã mở phòng thi \"{exam.Title}\" cho học viên làm bài.")
                    : $"Đã đóng/ẩn phòng thi \"{exam.Title}\".";

                return Ok(new
                {
                    success = true,
                    message,
                    id = exam.Id,
                    isPublished = nextPublished
                });
            }
            catch (OperationCanceledException)
            {
                return this.ApiCancelled();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể đổi trạng thái phòng thi {ExamId}.", id);
                return this.ApiServerError($"Không thể đổi trạng thái phòng thi: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        [HttpGet("/Admin/api/Exams/Detail/{id}")]
        public async Task<IActionResult> Detail(int id, CancellationToken cancellationToken)
        {
            if (id <= 0)
            {
                return this.ApiBadRequest("Mã phòng thi không hợp lệ.");
            }

            try
            {
                var nowUtc = DateTime.UtcNow;
                await AutoCloseExpiredRoomsAsync(nowUtc, cancellationToken);

                var exam = await (
                    from e in _context.Exams.AsNoTracking()
                    join c in _context.Courses.AsNoTracking() on e.CourseId equals c.Id into courseGroup
                    from course in courseGroup.DefaultIfEmpty()
                    join s in _context.Subjects.AsNoTracking() on course.SubjectId equals s.Id into subjGroup
                    from subj in subjGroup.DefaultIfEmpty()
                    where e.Id == id
                    select new
                    {
                        id = e.Id,
                        title = e.Title,
                        rawDescription = e.Description,
                        courseId = e.CourseId,
                        courseTitle = course != null ? course.Title : "Đánh giá chung",
                        subjectName = subj != null ? subj.Name : "Chung",
                        durationMinutes = e.DurationMinutes,
                        passScore = e.PassScore,
                        maxAttempts = e.MaxAttempts,
                        isPublished = e.IsPublished,
                        createdAt = e.CreatedAt,
                        updatedAt = e.UpdatedAt
                    }
                ).FirstOrDefaultAsync(cancellationToken);

                if (exam == null)
                {
                    return this.ApiNotFound("Không tìm thấy phòng thi yêu cầu.");
                }

                var openAtUtc = ToUtcNullable(exam.createdAt);
                var closeAtUtc = ToUtcNullable(exam.updatedAt);
                var cleanNote = ParseRoomDescription(exam.rawDescription).Note;

                var courseTeachers = exam.courseId.HasValue
                    ? await (
                        from cc in _context.CourseClasses.AsNoTracking()
                        join ct in _context.ClassTeachers.AsNoTracking() on cc.Id equals ct.ClassId
                        join t in _context.Teachers.AsNoTracking() on ct.TeacherId equals t.Id
                        join u in _context.Users.AsNoTracking() on t.UserId equals u.Id
                        where cc.CourseId == exam.courseId.Value && cc.Status != "cancelled" && u.IsActive
                        select new
                        {
                            teacherId = t.Id,
                            fullName = u.FullName,
                            email = u.Email,
                            isVerified = t.IsVerified
                        }
                    ).Distinct().OrderBy(x => x.fullName).ToListAsync(cancellationToken)
                    : new();

                var relatedCourseExams = exam.courseId.HasValue
                    ? await (
                        from e in _context.Exams.AsNoTracking()
                        where e.CourseId == exam.courseId.Value
                        orderby e.Id descending
                        select new
                        {
                            id = e.Id,
                            title = e.Title,
                            isPublished = e.IsPublished,
                            questionCount = _context.ExamQuestions.Count(eq => eq.ExamId == e.Id),
                            attemptCount = _context.UserExams.Count(ue => ue.ExamId == e.Id),
                            passedCount = _context.UserExams.Count(ue => ue.ExamId == e.Id && ue.Status == "passed")
                        }
                    ).ToListAsync(cancellationToken)
                    : new();

                var rawQuestions = await (
                    from eq in _context.ExamQuestions.AsNoTracking()
                    join q in _context.Questions.AsNoTracking() on eq.QuestionId equals q.Id
                    where eq.ExamId == id
                    orderby eq.OrderIndex, eq.Id
                    select new
                    {
                        examQuestionId = eq.Id,
                        questionId = q.Id,
                        orderIndex = eq.OrderIndex,
                        points = eq.Points,
                        questionText = q.QuestionText,
                        questionType = q.QuestionType,
                        audioUrl = q.AudioUrl
                    }
                ).ToListAsync(cancellationToken);

                var roomState = ComputeRoomState(exam.isPublished, rawQuestions.Count, openAtUtc, closeAtUtc, nowUtc);
                var questionIds = rawQuestions.Select(x => x.questionId).Distinct().ToList();

                var optionsByQuestion = questionIds.Count == 0
                    ? new Dictionary<int, List<object>>()
                    : (await _context.QuestionOptions
                        .AsNoTracking()
                        .Where(o => questionIds.Contains(o.QuestionId))
                        .OrderBy(o => o.Id)
                        .Select(o => new
                        {
                            o.QuestionId,
                            id = o.Id,
                            optionText = o.OptionText,
                            isCorrect = o.IsCorrect
                        })
                        .ToListAsync(cancellationToken))
                    .GroupBy(o => o.QuestionId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(o => (object)new { o.id, o.optionText, o.isCorrect }).ToList()
                    );

                var answersByQuestion = questionIds.Count == 0
                    ? new Dictionary<int, List<object>>()
                    : (await _context.QuestionAnswers
                        .AsNoTracking()
                        .Where(a => questionIds.Contains(a.QuestionId))
                        .OrderBy(a => a.Id)
                        .Select(a => new
                        {
                            a.QuestionId,
                            id = a.Id,
                            answerText = a.AnswerText,
                            isCaseSensitive = a.IsCaseSensitive,
                            referenceAudioUrl = a.ReferenceAudioUrl
                        })
                        .ToListAsync(cancellationToken))
                    .GroupBy(a => a.QuestionId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(a => (object)new { a.id, a.answerText, a.isCaseSensitive, a.referenceAudioUrl }).ToList()
                    );

                var questions = rawQuestions.Select(q => new
                {
                    q.examQuestionId,
                    q.questionId,
                    q.orderIndex,
                    q.points,
                    q.questionText,
                    q.questionType,
                    q.audioUrl,
                    options = optionsByQuestion.TryGetValue(q.questionId, out var opts) ? opts : new List<object>(),
                    referenceAnswers = answersByQuestion.TryGetValue(q.questionId, out var refs) ? refs : new List<object>()
                }).ToList();

                var attempts = await (
                    from ue in _context.UserExams.AsNoTracking()
                    join u in _context.Users.AsNoTracking() on ue.UserId equals u.Id
                    where ue.ExamId == id
                    orderby ue.SubmittedAt ?? ue.StartedAt descending, ue.Id descending
                    select new
                    {
                        userExamId = ue.Id,
                        userId = ue.UserId,
                        studentName = u.FullName,
                        studentEmail = u.Email,
                        attemptNumber = ue.AttemptNumber,
                        startedAt = ue.StartedAt,
                        submittedAt = ue.SubmittedAt,
                        totalScore = ue.TotalScore,
                        status = ue.Status
                    }
                ).ToListAsync(cancellationToken);

                var scoredAttempts = attempts.Where(a => a.totalScore.HasValue).ToList();
                var totalPoints = rawQuestions.Sum(q => q.points);
                var finishedCount = attempts.Count(a => a.status == "passed" || a.status == "failed");

                return Ok(new
                {
                    success = true,
                    exam = new
                    {
                        exam.id,
                        exam.title,
                        description = cleanNote,
                        openAt = openAtUtc,
                        closeAt = closeAtUtc,
                        roomState = roomState.Code,
                        roomStateLabel = roomState.Label,
                        isExpired = roomState.IsExpired,
                        isOpenNow = roomState.IsOpenNow,
                        exam.courseId,
                        exam.courseTitle,
                        exam.subjectName,
                        courseTeachers,
                        exam.durationMinutes,
                        exam.passScore,
                        exam.maxAttempts,
                        exam.isPublished,
                        createdAt = openAtUtc,
                        updatedAt = closeAtUtc,
                        questionCount = questions.Count,
                        totalPoints,
                        attemptCount = attempts.Count
                    },
                    attemptStats = new
                    {
                        totalAttempts = attempts.Count,
                        distinctStudents = attempts.Select(a => a.userId).Distinct().Count(),
                        passedCount = attempts.Count(a => a.status == "passed"),
                        failedCount = attempts.Count(a => a.status == "failed"),
                        submittedCount = attempts.Count(a => a.status == "submitted"),
                        inProgressCount = attempts.Count(a => a.status == "in_progress"),
                        passRate = finishedCount > 0 ? Math.Round(attempts.Count(a => a.status == "passed") * 100.0 / finishedCount, 1) : (double?)null,
                        avgScore = scoredAttempts.Count > 0 ? Math.Round(scoredAttempts.Average(a => a.totalScore!.Value), 1) : (decimal?)null,
                        highestScore = scoredAttempts.Count > 0 ? scoredAttempts.Max(a => a.totalScore!.Value) : (decimal?)null,
                        lowestScore = scoredAttempts.Count > 0 ? scoredAttempts.Min(a => a.totalScore!.Value) : (decimal?)null
                    },
                    relatedCourseExams,
                    questions,
                    attempts
                });
            }
            catch (OperationCanceledException)
            {
                return this.ApiCancelled();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể tải chi tiết phòng thi {ExamId}.", id);
                return this.ApiServerError("Không thể tải chi tiết phòng thi.");
            }
        }

        [HttpGet("/Admin/api/Exams/AttemptDetail/{userExamId}")]
        public async Task<IActionResult> AttemptDetail(int userExamId, CancellationToken cancellationToken)
        {
            if (userExamId <= 0)
            {
                return this.ApiBadRequest("Mã lượt thi không hợp lệ.");
            }

            try
            {
                var attempt = await (
                    from ue in _context.UserExams.AsNoTracking()
                    join u in _context.Users.AsNoTracking() on ue.UserId equals u.Id
                    join e in _context.Exams.AsNoTracking() on ue.ExamId equals e.Id
                    join c in _context.Courses.AsNoTracking() on e.CourseId equals c.Id into courseGroup
                    from course in courseGroup.DefaultIfEmpty()
                    where ue.Id == userExamId
                    select new
                    {
                        userExamId = ue.Id,
                        examId = e.Id,
                        examTitle = e.Title,
                        courseId = e.CourseId,
                        courseTitle = course != null ? course.Title : "Đánh giá chung",
                        durationMinutes = e.DurationMinutes,
                        passScore = e.PassScore,
                        isPublished = e.IsPublished,
                        userId = u.Id,
                        studentName = u.FullName,
                        studentEmail = u.Email,
                        attemptNumber = ue.AttemptNumber,
                        startedAt = ue.StartedAt,
                        submittedAt = ue.SubmittedAt,
                        totalScore = ue.TotalScore,
                        status = ue.Status
                    }
                ).FirstOrDefaultAsync(cancellationToken);

                if (attempt == null)
                {
                    return this.ApiNotFound("Không tìm thấy bài làm của học viên.");
                }

                var rawQuestions = await (
                    from eq in _context.ExamQuestions.AsNoTracking()
                    join q in _context.Questions.AsNoTracking() on eq.QuestionId equals q.Id
                    where eq.ExamId == attempt.examId
                    orderby eq.OrderIndex, eq.Id
                    select new
                    {
                        examQuestionId = eq.Id,
                        questionId = q.Id,
                        orderIndex = eq.OrderIndex,
                        points = eq.Points,
                        questionText = q.QuestionText,
                        questionType = q.QuestionType,
                        audioUrl = q.AudioUrl
                    }
                ).ToListAsync(cancellationToken);

                var questionIds = rawQuestions.Select(x => x.questionId).Distinct().ToList();

                var optionsByQuestion = questionIds.Count == 0
                    ? new Dictionary<int, List<(int Id, string OptionText, bool IsCorrect)>>()
                    : (await _context.QuestionOptions
                        .AsNoTracking()
                        .Where(o => questionIds.Contains(o.QuestionId))
                        .OrderBy(o => o.Id)
                        .ToListAsync(cancellationToken))
                    .GroupBy(o => o.QuestionId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(o => (o.Id, o.OptionText, o.IsCorrect)).ToList()
                    );

                var refsByQuestion = questionIds.Count == 0
                    ? new Dictionary<int, List<object>>()
                    : (await _context.QuestionAnswers
                        .AsNoTracking()
                        .Where(a => questionIds.Contains(a.QuestionId))
                        .OrderBy(a => a.Id)
                        .ToListAsync(cancellationToken))
                    .GroupBy(a => a.QuestionId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(a => (object)new
                        {
                            id = a.Id,
                            answerText = a.AnswerText,
                            isCaseSensitive = a.IsCaseSensitive
                        }).ToList()
                    );

                var studentAnswersList = await _context.UserExamAnswers
                    .AsNoTracking()
                    .Where(a => a.UserExamId == userExamId)
                    .ToListAsync(cancellationToken);

                var studentAnswersByQuestion = studentAnswersList
                    .GroupBy(a => a.QuestionId)
                    .ToDictionary(g => g.Key, g => g.First());

                var graderIds = studentAnswersList
                    .Where(a => a.GradedByTeacherId.HasValue)
                    .Select(a => a.GradedByTeacherId!.Value)
                    .Distinct()
                    .ToList();

                var graderNames = graderIds.Count == 0
                    ? new Dictionary<int, string>()
                    : await (
                        from t in _context.Teachers.AsNoTracking()
                        join u in _context.Users.AsNoTracking() on t.UserId equals u.Id
                        where graderIds.Contains(t.Id)
                        select new { t.Id, u.FullName }
                    ).ToDictionaryAsync(x => x.Id, x => x.FullName, cancellationToken);

                var items = rawQuestions.Select(q =>
                {
                    studentAnswersByQuestion.TryGetValue(q.questionId, out var sa);
                    var opts = optionsByQuestion.TryGetValue(q.questionId, out var rawOpts)
                        ? rawOpts
                        : new List<(int Id, string OptionText, bool IsCorrect)>();

                    var selectedOption = sa?.SelectedOptionId != null
                        ? opts.FirstOrDefault(o => o.Id == sa.SelectedOptionId.Value)
                        : default;

                    string? graderName = null;
                    if (sa?.GradedByTeacherId != null && graderNames.TryGetValue(sa.GradedByTeacherId.Value, out var gn))
                    {
                        graderName = gn;
                    }

                    return new
                    {
                        q.examQuestionId,
                        q.questionId,
                        q.orderIndex,
                        q.points,
                        q.questionText,
                        q.questionType,
                        q.audioUrl,
                        isAnswered = sa != null,
                        selectedOptionId = sa?.SelectedOptionId,
                        selectedOptionText = selectedOption.Id != 0 ? selectedOption.OptionText : null,
                        isSelectedCorrect = selectedOption.Id != 0 ? selectedOption.IsCorrect : (bool?)null,
                        textAnswer = sa?.TextAnswer,
                        scoreEarned = sa?.ScoreEarned,
                        gradingStatus = sa?.GradingStatus,
                        feedback = sa?.Feedback,
                        gradedAt = sa?.GradedAt,
                        gradedByTeacherName = graderName,
                        options = opts.Select(o => new
                        {
                            id = o.Id,
                            optionText = o.OptionText,
                            isCorrect = o.IsCorrect,
                            isSelected = sa?.SelectedOptionId == o.Id
                        }).ToList(),
                        referenceAnswers = refsByQuestion.TryGetValue(q.questionId, out var ra) ? ra : new List<object>()
                    };
                }).ToList();

                return Ok(new
                {
                    success = true,
                    attempt = new
                    {
                        attempt.userExamId,
                        attempt.examId,
                        attempt.examTitle,
                        attempt.courseTitle,
                        attempt.durationMinutes,
                        attempt.passScore,
                        attempt.isPublished,
                        attempt.userId,
                        attempt.studentName,
                        attempt.studentEmail,
                        attempt.attemptNumber,
                        attempt.startedAt,
                        attempt.submittedAt,
                        attempt.totalScore,
                        maxTotalPoints = rawQuestions.Sum(q => q.points),
                        answeredCount = items.Count(i => i.isAnswered),
                        questionCount = items.Count,
                        attempt.status
                    },
                    answers = items
                });
            }
            catch (OperationCanceledException)
            {
                return this.ApiCancelled();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể tải chi tiết bài làm {UserExamId}.", userExamId);
                return this.ApiServerError("Không thể tải chi tiết bài làm của học viên.");
            }
        }

        [HttpGet("/Admin/api/Exams/CheckRoomAvailability/{id}")]
        public async Task<IActionResult> CheckRoomAvailability(int id, CancellationToken cancellationToken)
        {
            if (id <= 0)
            {
                return this.ApiBadRequest("Mã phòng thi không hợp lệ.");
            }

            var nowUtc = DateTime.UtcNow;
            await AutoCloseExpiredRoomsAsync(nowUtc, cancellationToken);

            var exam = await _context.Exams
                .AsNoTracking()
                .Where(e => e.Id == id)
                .Select(e => new
                {
                    e.Id,
                    e.Title,
                    e.Description,
                    e.IsPublished,
                    e.CreatedAt,
                    e.UpdatedAt,
                    questionCount = _context.ExamQuestions.Count(eq => eq.ExamId == e.Id)
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (exam == null)
            {
                return this.ApiNotFound("Không tìm thấy phòng thi.");
            }

            var openAtUtc = ToUtcNullable(exam.CreatedAt);
            var closeAtUtc = ToUtcNullable(exam.UpdatedAt);
            var canEnter = CanStudentEnterExamWindow(openAtUtc, closeAtUtc, exam.IsPublished, exam.questionCount, nowUtc, out var reason);
            return Ok(new
            {
                success = true,
                examId = exam.Id,
                canEnter,
                reason,
                openAt = openAtUtc,
                closeAt = closeAtUtc,
                serverNowUtc = nowUtc
            });
        }

        [HttpPost("/Admin/api/Exams/{examId}/Questions/Create")]
        public IActionResult CreateQuestion(int examId)
            => TeacherQuestionReadOnlyForbidden();

        [HttpPost("/Admin/api/Exams/{examId}/Questions/Update/{examQuestionId}")]
        [HttpPut("/Admin/api/Exams/{examId}/Questions/Update/{examQuestionId}")]
        public IActionResult UpdateQuestion(int examId, int examQuestionId)
            => TeacherQuestionReadOnlyForbidden();

        [HttpPost("/Admin/api/Exams/{examId}/Questions/Delete/{examQuestionId}")]
        [HttpDelete("/Admin/api/Exams/{examId}/Questions/Delete/{examQuestionId}")]
        public IActionResult DeleteQuestion(int examId, int examQuestionId)
            => TeacherQuestionReadOnlyForbidden();

        private async Task<DateTime> ResolveDbTimestampAsync(DateTime nowUtc, CancellationToken cancellationToken)
        {
            var sample = await _context.Exams
                .AsNoTracking()
                .Select(e => (DateTime?)e.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (sample.HasValue && sample.Value.Kind == DateTimeKind.Utc)
            {
                return nowUtc;
            }
            return DateTime.SpecifyKind(nowUtc, DateTimeKind.Unspecified);
        }

        private static DateTime? ToUtcNullable(DateTime? dt)
        {
            if (!dt.HasValue)
            {
                return null;
            }
            return dt.Value.Kind == DateTimeKind.Utc
                ? dt.Value
                : DateTime.SpecifyKind(dt.Value, DateTimeKind.Utc);
        }

        private async Task AutoCloseExpiredRoomsAsync(DateTime nowUtc, CancellationToken cancellationToken)
        {
            // 1. Tự động chuyển dữ liệu cũ lưu trong description (EL_EXAM_ROOM_V1 ...) sang created_at (openAt) & updated_at (closeAt)
            var legacyRows = await _context.Exams
                .AsNoTracking()
                .Where(e => e.Description != null && e.Description.StartsWith(RoomWindowPrefix))
                .Select(e => new { e.Id, e.Description, e.CreatedAt, e.UpdatedAt })
                .ToListAsync(cancellationToken);

            foreach (var row in legacyRows)
            {
                var parsed = ParseRoomDescription(row.Description);
                var isUtcKind = row.CreatedAt.Kind == DateTimeKind.Utc;

                var newCreatedAt = parsed.OpenAtUtc.HasValue
                    ? (isUtcKind ? parsed.OpenAtUtc.Value : DateTime.SpecifyKind(parsed.OpenAtUtc.Value, DateTimeKind.Unspecified))
                    : row.CreatedAt;

                var newUpdatedAt = parsed.CloseAtUtc.HasValue
                    ? (isUtcKind ? parsed.CloseAtUtc.Value : DateTime.SpecifyKind(parsed.CloseAtUtc.Value, DateTimeKind.Unspecified))
                    : row.UpdatedAt;

                var cleanNote = string.IsNullOrWhiteSpace(parsed.Note) ? null : parsed.Note;

                await _context.Database.ExecuteSqlInterpolatedAsync(
                    $@"UPDATE exams
                       SET description = {cleanNote},
                           created_at = {newCreatedAt},
                           updated_at = {newUpdatedAt}
                       WHERE id = {row.Id}",
                    cancellationToken);
            }

            // 2. Tự động đóng các phòng thi đã quá thời gian kết thúc (updated_at <= nowUtc), giữ nguyên updated_at
            var candidates = await _context.Exams
                .AsNoTracking()
                .Where(e => e.IsPublished && e.UpdatedAt.HasValue)
                .Select(e => new { e.Id, e.UpdatedAt })
                .ToListAsync(cancellationToken);

            foreach (var exam in candidates)
            {
                var closeAtUtc = ToUtcNullable(exam.UpdatedAt);
                if (closeAtUtc.HasValue && closeAtUtc.Value <= nowUtc)
                {
                    await _context.Database.ExecuteSqlInterpolatedAsync(
                        $"UPDATE exams SET is_published = FALSE WHERE id = {exam.Id}",
                        cancellationToken);
                }
            }
        }

        private async Task<int> ResolveRepresentativeTeacherIdAsync(int courseId, CancellationToken cancellationToken)
        {
            var courseTeacherId = await (
                from cc in _context.CourseClasses.AsNoTracking()
                join ct in _context.ClassTeachers.AsNoTracking() on cc.Id equals ct.ClassId
                join t in _context.Teachers.AsNoTracking() on ct.TeacherId equals t.Id
                join u in _context.Users.AsNoTracking() on t.UserId equals u.Id
                where cc.CourseId == courseId && cc.Status != "cancelled" && u.IsActive
                orderby t.IsVerified descending, t.Id
                select t.Id
            ).FirstOrDefaultAsync(cancellationToken);

            if (courseTeacherId > 0)
            {
                return courseTeacherId;
            }

            return await (
                from t in _context.Teachers.AsNoTracking()
                join u in _context.Users.AsNoTracking() on t.UserId equals u.Id
                where u.Role == "teacher" && u.IsActive
                orderby t.IsVerified descending, t.Id
                select t.Id
            ).FirstOrDefaultAsync(cancellationToken);
        }

        private IActionResult TeacherQuestionReadOnlyForbidden()
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                success = false,
                message = "Câu hỏi, thời lượng và điểm qua đề thi do đội ngũ giảng viên của môn học thiết lập. Quản trị viên chỉ mở phòng thi theo môn học và xem thống kê."
            });
        }

        private static string? ValidateExam(
            ExamUpsertDto? dto,
            out string title,
            out string? note,
            out DateTime? openAtUtc,
            out DateTime? closeAtUtc)
        {
            title = string.Empty;
            note = null;
            openAtUtc = null;
            closeAtUtc = null;

            if (dto == null)
            {
                return "Dữ liệu gửi lên không hợp lệ.";
            }

            title = (dto.Title ?? string.Empty).Trim();
            note = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();

            if (title.Length == 0 || title.Length > MaxTitleLength)
            {
                return $"Tên phòng thi / bài thi không hợp lệ (1 - {MaxTitleLength} ký tự).";
            }

            if (note != null && note.Length > MaxDescriptionLength)
            {
                return $"Ghi chú phòng thi không được vượt quá {MaxDescriptionLength} ký tự.";
            }

            if (!string.IsNullOrWhiteSpace(dto.OpenAt))
            {
                if (!TryParseIsoUtc(dto.OpenAt, out var parsedOpen))
                {
                    return "Thời gian mở phòng thi không hợp lệ.";
                }
                openAtUtc = parsedOpen;
            }

            if (!string.IsNullOrWhiteSpace(dto.CloseAt))
            {
                if (!TryParseIsoUtc(dto.CloseAt, out var parsedClose))
                {
                    return "Thời gian kết thúc phòng thi không hợp lệ.";
                }
                closeAtUtc = parsedClose;
            }

            if (!openAtUtc.HasValue || !closeAtUtc.HasValue)
            {
                return "Vui lòng chọn đầy đủ thời gian mở phòng thi và thời gian kết thúc phòng thi.";
            }

            if (closeAtUtc.Value <= openAtUtc.Value.AddMinutes(5))
            {
                return "Thời gian kết thúc phòng thi phải sau thời gian mở phòng ít nhất 5 phút.";
            }

            return null;
        }

        private static bool TryParseIsoUtc(string raw, out DateTime utcDateTime)
        {
            utcDateTime = default;
            if (DateTimeOffset.TryParse(raw.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dto))
            {
                utcDateTime = dto.UtcDateTime;
                return true;
            }
            return false;
        }

        public static string? BuildRoomDescription(string? note, DateTime? openAtUtc, DateTime? closeAtUtc)
        {
            var cleanNote = string.IsNullOrWhiteSpace(note) ? string.Empty : note.Trim();
            if (!openAtUtc.HasValue && !closeAtUtc.HasValue)
            {
                return cleanNote.Length == 0 ? null : cleanNote;
            }

            var payload = new RoomWindowPayload
            {
                OpenAt = openAtUtc?.ToString("O"),
                CloseAt = closeAtUtc?.ToString("O"),
                Note = cleanNote
            };
            return RoomWindowPrefix + JsonSerializer.Serialize(payload);
        }

        public static (DateTime? OpenAtUtc, DateTime? CloseAtUtc, string Note) ParseRoomDescription(string? rawDescription)
        {
            var raw = (rawDescription ?? string.Empty).Trim();
            if (!raw.StartsWith(RoomWindowPrefix, StringComparison.Ordinal))
            {
                return (null, null, raw);
            }

            var jsonPart = raw.Substring(RoomWindowPrefix.Length).Trim();
            try
            {
                var parsed = JsonSerializer.Deserialize<RoomWindowPayload>(jsonPart);
                if (parsed != null)
                {
                    DateTime? openUtc = !string.IsNullOrWhiteSpace(parsed.OpenAt) && TryParseIsoUtc(parsed.OpenAt, out var o) ? o : null;
                    DateTime? closeUtc = !string.IsNullOrWhiteSpace(parsed.CloseAt) && TryParseIsoUtc(parsed.CloseAt, out var c) ? c : null;
                    return (openUtc, closeUtc, (parsed.Note ?? string.Empty).Trim());
                }
            }
            catch
            {
            }

            return (null, null, raw);
        }

        public static bool CanStudentEnterExamWindow(
            DateTime? openAtUtc,
            DateTime? closeAtUtc,
            bool isPublished,
            int questionCount,
            DateTime nowUtc,
            out string reason)
        {
            if (closeAtUtc.HasValue && closeAtUtc.Value <= nowUtc)
            {
                reason = "Phòng thi đã đóng vì đã quá thời gian quy định của kỳ thi.";
                return false;
            }

            if (!isPublished)
            {
                reason = "Phòng thi hiện đang đóng hoặc chưa được mở công bố.";
                return false;
            }

            if (questionCount <= 0)
            {
                reason = "Giảng viên chưa hoàn tất đề thi cho phòng thi này.";
                return false;
            }

            if (openAtUtc.HasValue && nowUtc < openAtUtc.Value)
            {
                reason = "Chưa đến thời gian mở phòng thi.";
                return false;
            }

            reason = "Phòng thi đang mở.";
            return true;
        }

        public static bool CanStudentEnterExam(
            string? rawDescription,
            bool isPublished,
            int questionCount,
            DateTime nowUtc,
            out string reason,
            out DateTime? openAtUtc,
            out DateTime? closeAtUtc)
        {
            var parsed = ParseRoomDescription(rawDescription);
            openAtUtc = parsed.OpenAtUtc;
            closeAtUtc = parsed.CloseAtUtc;
            return CanStudentEnterExamWindow(openAtUtc, closeAtUtc, isPublished, questionCount, nowUtc, out reason);
        }

        private static (string Code, string Label, bool IsExpired, bool IsOpenNow) ComputeRoomState(
            bool isPublished,
            int questionCount,
            DateTime? openAtUtc,
            DateTime? closeAtUtc,
            DateTime nowUtc)
        {
            if (closeAtUtc.HasValue && closeAtUtc.Value <= nowUtc)
            {
                return ("expired", "Đã đóng (Hết giờ)", true, false);
            }

            if (!isPublished)
            {
                return questionCount > 0
                    ? ("draft", "Đã có đề (Đang ẩn)", false, false)
                    : ("waiting_questions", "Chờ GV soạn đề", false, false);
            }

            if (openAtUtc.HasValue && nowUtc < openAtUtc.Value)
            {
                return ("scheduled", "Chờ đến giờ mở", false, false);
            }

            return ("open", "Đang mở thi", false, true);
        }

        private class RoomWindowPayload
        {
            public string? OpenAt { get; set; }
            public string? CloseAt { get; set; }
            public string? Note { get; set; }
        }

        public class ExamUpsertDto
        {
            public string? Title { get; set; }
            public int? CourseId { get; set; }
            public string? OpenAt { get; set; }
            public string? CloseAt { get; set; }
            public string? Description { get; set; }
            public bool? IsPublished { get; set; }
        }
    }
}