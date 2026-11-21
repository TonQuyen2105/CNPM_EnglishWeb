using CNPM_EnglishLearning.Data;
using CNPM_EnglishLearning.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CNPM_EnglishLearning.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ExamsController : Controller
    {
        private const int MaxTitleLength = 200;
        private const int MaxDescriptionLength = 2000;
        private const int MaxDurationMinutes = 600;
        private const int MaxQuestionTextLength = 4000;

        private static readonly HashSet<string> ValidQuestionTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "multiple_choice", "fill_blank", "pronunciation", "essay"
        };

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
                        teacherName = teacherUser != null ? teacherUser.FullName : "Giảng viên",
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
                    .OrderByDescending(x => x.isPublished)
                    .ThenByDescending(x => x.id)
                    .ToListAsync(cancellationToken);

                var totalExams = await _context.Exams.AsNoTracking().CountAsync(cancellationToken);
                var publishedExams = await _context.Exams.AsNoTracking().CountAsync(e => e.IsPublished, cancellationToken);
                var totalQuestions = await _context.ExamQuestions.AsNoTracking().CountAsync(cancellationToken);
                var totalAttempts = await _context.UserExams.AsNoTracking().CountAsync(cancellationToken);

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
                        where u.Role == "teacher" && u.IsActive
                        orderby u.FullName
                        select new
                        {
                            id = t.Id,
                            fullName = u.FullName,
                            isVerified = t.IsVerified
                        }
                    ).ToListAsync(cancellationToken)
                    : null;

                return Ok(new
                {
                    success = true,
                    items,
                    courses,
                    teachers,
                    stats = new
                    {
                        totalExams,
                        publishedExams,
                        draftExams = totalExams - publishedExams,
                        totalQuestions,
                        totalAttempts
                    }
                });
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
                    teacherId = await (
                        from t in _context.Teachers.AsNoTracking()
                        join u in _context.Users.AsNoTracking() on t.UserId equals u.Id
                        where u.Role == "teacher" && u.IsActive
                        orderby t.IsVerified descending, t.Id
                        select t.Id
                    ).FirstOrDefaultAsync(cancellationToken);
                }
                else
                {
                    var teacherExists = await (
                        from t in _context.Teachers.AsNoTracking()
                        join u in _context.Users.AsNoTracking() on t.UserId equals u.Id
                        where t.Id == teacherId && u.Role == "teacher" && u.IsActive
                        select t.Id
                    ).AnyAsync(cancellationToken);
                    if (!teacherExists)
                    {
                        return this.ApiBadRequest("Giảng viên phụ trách đề thi không tồn tại hoặc tài khoản đang bị khóa/đổi vai trò.");
                    }
                }

                if (teacherId <= 0)
                {
                    return this.ApiBadRequest("Hệ thống chưa có hồ sơ giảng viên hoạt động nào để gắn người tạo đề.");
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
                try
                {
                    await _context.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation })
                {
                    return this.ApiBadRequest("Khóa học hoặc giảng viên phụ trách không còn tồn tại.");
                }

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
                    var teacherExists = await (
                        from t in _context.Teachers.AsNoTracking()
                        join u in _context.Users.AsNoTracking() on t.UserId equals u.Id
                        where t.Id == dto.CreatedByTeacherId && u.Role == "teacher" && u.IsActive
                        select t.Id
                    ).AnyAsync(cancellationToken);
                    if (!teacherExists)
                    {
                        return this.ApiBadRequest("Giảng viên phụ trách không tồn tại hoặc tài khoản đang bị khóa/đổi vai trò.");
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

                try
                {
                    await _context.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation })
                {
                    return this.ApiBadRequest("Khóa học hoặc giảng viên phụ trách không còn tồn tại.");
                }

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
                    message = exam.IsPublished
                        ? $"Đã công bố đề thi \"{exam.Title}\"."
                        : $"Đã chuyển đề thi \"{exam.Title}\" về bản nháp.",
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

        [HttpGet("/Admin/api/Exams/Detail/{id}")]
        public async Task<IActionResult> Detail(int id, CancellationToken cancellationToken)
        {
            if (id <= 0)
            {
                return this.ApiBadRequest("Mã đề thi không hợp lệ.");
            }

            try
            {
                var exam = await (
                    from e in _context.Exams.AsNoTracking()
                    join c in _context.Courses.AsNoTracking() on e.CourseId equals c.Id into courseGroup
                    from course in courseGroup.DefaultIfEmpty()
                    join s in _context.Subjects.AsNoTracking() on course.SubjectId equals s.Id into subjGroup
                    from subj in subjGroup.DefaultIfEmpty()
                    join t in _context.Teachers.AsNoTracking() on e.CreatedByTeacherId equals t.Id into teacherGroup
                    from teacher in teacherGroup.DefaultIfEmpty()
                    join u in _context.Users.AsNoTracking() on teacher.UserId equals u.Id into userGroup
                    from teacherUser in userGroup.DefaultIfEmpty()
                    where e.Id == id
                    select new
                    {
                        id = e.Id,
                        title = e.Title,
                        description = e.Description,
                        courseId = e.CourseId,
                        courseTitle = course != null ? course.Title : "Đánh giá chung",
                        subjectName = subj != null ? subj.Name : "Chung",
                        createdByTeacherId = e.CreatedByTeacherId,
                        teacherName = teacherUser != null ? teacherUser.FullName : "Giảng viên",
                        teacherEmail = teacherUser != null ? teacherUser.Email : null,
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
                    return this.ApiNotFound("Không tìm thấy đề thi yêu cầu.");
                }

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

                return Ok(new
                {
                    success = true,
                    exam = new
                    {
                        exam.id,
                        exam.title,
                        exam.description,
                        exam.courseId,
                        exam.courseTitle,
                        exam.subjectName,
                        exam.createdByTeacherId,
                        exam.teacherName,
                        exam.teacherEmail,
                        exam.durationMinutes,
                        exam.passScore,
                        exam.maxAttempts,
                        exam.isPublished,
                        exam.createdAt,
                        exam.updatedAt,
                        questionCount = questions.Count,
                        totalPoints,
                        attemptCount = attempts.Count
                    },
                    attemptStats = new
                    {
                        totalAttempts = attempts.Count,
                        passedCount = attempts.Count(a => a.status == "passed"),
                        failedCount = attempts.Count(a => a.status == "failed"),
                        submittedCount = attempts.Count(a => a.status == "submitted"),
                        inProgressCount = attempts.Count(a => a.status == "in_progress"),
                        avgScore = scoredAttempts.Count > 0 ? Math.Round(scoredAttempts.Average(a => a.totalScore!.Value), 1) : (double?)null
                    },
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
                _logger.LogError(ex, "Không thể tải chi tiết đề thi {ExamId}.", id);
                return this.ApiServerError("Không thể tải chi tiết đề thi.");
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

        [HttpPost("/Admin/api/Exams/{examId}/Questions/Create")]
        public async Task<IActionResult> CreateQuestion(
            int examId,
            [FromBody] ExamQuestionUpsertDto? dto,
            CancellationToken cancellationToken)
        {
            if (examId <= 0)
            {
                return this.ApiBadRequest("Mã đề thi không hợp lệ.");
            }

            var error = ValidateQuestionDto(dto, out var qText, out var qType, out var cleanOptions, out var refAnswer, out var audioUrl);
            if (error != null)
            {
                return this.ApiBadRequest(error);
            }

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                long examLockKey = 900_000_000L + examId;
                await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({examLockKey})", cancellationToken);

                var exam = await _context.Exams.FirstOrDefaultAsync(e => e.Id == examId, cancellationToken);
                if (exam == null)
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiNotFound("Không tìm thấy đề thi.");
                }

                var nextOrder = dto!.OrderIndex.HasValue && dto.OrderIndex.Value > 0
                    ? dto.OrderIndex.Value
                    : ((await _context.ExamQuestions
                        .AsNoTracking()
                        .Where(eq => eq.ExamId == examId)
                        .Select(eq => (int?)eq.OrderIndex)
                        .MaxAsync(cancellationToken)) ?? 0) + 1;

                if (nextOrder > 1000)
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiBadRequest("Thứ tự câu hỏi phải từ 1 đến 1.000.");
                }

                var duplicateOrder = await _context.ExamQuestions
                    .AsNoTracking()
                    .AnyAsync(eq => eq.ExamId == examId && eq.OrderIndex == nextOrder, cancellationToken);
                if (duplicateOrder)
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiBadRequest($"Thứ tự câu hỏi số {nextOrder} đã tồn tại trong đề thi này.");
                }

                var now = DateTime.UtcNow;
                var question = new Question
                {
                    QuestionText = qText,
                    QuestionType = qType,
                    AudioUrl = audioUrl,
                    DefaultPoints = dto.Points,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                _context.Questions.Add(question);
                await _context.SaveChangesAsync(cancellationToken);

                if (qType == "multiple_choice")
                {
                    var options = cleanOptions.Select(o => new QuestionOption
                    {
                        QuestionId = question.Id,
                        OptionText = o.Text,
                        IsCorrect = o.IsCorrect
                    });
                    _context.QuestionOptions.AddRange(options);
                }
                else if (!string.IsNullOrWhiteSpace(refAnswer))
                {
                    _context.QuestionAnswers.Add(new QuestionAnswer
                    {
                        QuestionId = question.Id,
                        AnswerText = refAnswer,
                        IsCaseSensitive = dto.IsCaseSensitive ?? false
                    });
                }

                var examQuestion = new ExamQuestion
                {
                    ExamId = examId,
                    QuestionId = question.Id,
                    Points = dto.Points,
                    OrderIndex = nextOrder
                };
                _context.ExamQuestions.Add(examQuestion);
                exam.UpdatedAt = now;

                try
                {
                    await _context.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiBadRequest($"Thứ tự câu hỏi số {nextOrder} đã tồn tại trong đề thi này.");
                }
                await transaction.CommitAsync(cancellationToken);

                return Ok(new
                {
                    success = true,
                    message = "Đã thêm câu hỏi mới vào đề thi.",
                    examQuestionId = examQuestion.Id,
                    questionId = question.Id
                });
            }
            catch (OperationCanceledException)
            {
                return this.ApiCancelled();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể thêm câu hỏi cho đề thi {ExamId}.", examId);
                await transaction.RollbackAsync(CancellationToken.None);
                return this.ApiServerError("Không thể thêm câu hỏi mới vào đề thi.");
            }
        }

        [HttpPost("/Admin/api/Exams/{examId}/Questions/Update/{examQuestionId}")]
        [HttpPut("/Admin/api/Exams/{examId}/Questions/Update/{examQuestionId}")]
        public async Task<IActionResult> UpdateQuestion(
            int examId,
            int examQuestionId,
            [FromBody] ExamQuestionUpsertDto? dto,
            CancellationToken cancellationToken)
        {
            if (examId <= 0 || examQuestionId <= 0)
            {
                return this.ApiBadRequest("Mã đề thi hoặc câu hỏi không hợp lệ.");
            }

            var error = ValidateQuestionDto(dto, out var qText, out var qType, out var cleanOptions, out var refAnswer, out var audioUrl);
            if (error != null)
            {
                return this.ApiBadRequest(error);
            }

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                long examLockKey = 900_000_000L + examId;
                await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({examLockKey})", cancellationToken);

                var eq = await _context.ExamQuestions
                    .FirstOrDefaultAsync(x => x.Id == examQuestionId && x.ExamId == examId, cancellationToken);
                if (eq == null)
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiNotFound("Không tìm thấy câu hỏi trong đề thi.");
                }

                var newOrder = dto!.OrderIndex.HasValue && dto.OrderIndex.Value > 0
                    ? dto.OrderIndex.Value
                    : eq.OrderIndex;

                var duplicateOrder = await _context.ExamQuestions
                    .AsNoTracking()
                    .AnyAsync(x => x.ExamId == examId && x.Id != examQuestionId && x.OrderIndex == newOrder, cancellationToken);
                if (duplicateOrder)
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiBadRequest($"Thứ tự câu hỏi số {newOrder} đã tồn tại trong đề thi này.");
                }

                var question = await _context.Questions.FirstOrDefaultAsync(q => q.Id == eq.QuestionId, cancellationToken);
                var exam = await _context.Exams.FirstOrDefaultAsync(e => e.Id == examId, cancellationToken);
                if (question == null || exam == null)
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiNotFound("Không tìm thấy dữ liệu câu hỏi hoặc đề thi.");
                }

                var hasAnyStudentAnswers = await _context.UserExamAnswers
                    .AsNoTracking()
                    .AnyAsync(a => a.QuestionId == question.Id, cancellationToken);

                if (hasAnyStudentAnswers && !string.Equals(question.QuestionType, qType, StringComparison.OrdinalIgnoreCase))
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiBadRequest("Câu hỏi này đã có học viên làm bài nên không thể đổi sang loại câu hỏi khác.");
                }

                var referencedOptionIds = hasAnyStudentAnswers
                    ? await _context.UserExamAnswers
                        .AsNoTracking()
                        .Where(a => a.QuestionId == question.Id && a.SelectedOptionId != null)
                        .Select(a => a.SelectedOptionId!.Value)
                        .Distinct()
                        .ToListAsync(cancellationToken)
                    : new List<int>();

                var now = DateTime.UtcNow;
                question.QuestionText = qText;
                question.QuestionType = qType;
                if (dto.AudioUrl != null)
                {
                    question.AudioUrl = audioUrl;
                }
                question.DefaultPoints = dto.Points;
                question.UpdatedAt = now;

                eq.Points = dto.Points;
                eq.OrderIndex = newOrder;
                exam.UpdatedAt = now;

                var existingOptions = await _context.QuestionOptions
                    .Where(o => o.QuestionId == question.Id)
                    .OrderBy(o => o.Id)
                    .ToListAsync(cancellationToken);

                var existingRefs = await _context.QuestionAnswers
                    .Where(a => a.QuestionId == question.Id)
                    .OrderBy(a => a.Id)
                    .ToListAsync(cancellationToken);

                if (qType == "multiple_choice")
                {
                    if (existingRefs.Count > 0 && !hasAnyStudentAnswers)
                    {
                        _context.QuestionAnswers.RemoveRange(existingRefs);
                    }

                    // Khi đã có bài làm học viên, khớp phương án theo Id (nếu client gửi kèm) để không làm lệch SelectedOptionId
                    if (hasAnyStudentAnswers || referencedOptionIds.Count > 0)
                    {
                        var hasClientIds = cleanOptions.Any(o => o.Id.HasValue && o.Id.Value > 0);
                        if (hasClientIds)
                        {
                            var optionById = existingOptions.ToDictionary(o => o.Id);
                            var keptOptionIds = new HashSet<int>();

                            foreach (var optDto in cleanOptions)
                            {
                                if (optDto.Id.HasValue && optionById.TryGetValue(optDto.Id.Value, out var matched))
                                {
                                    matched.OptionText = optDto.Text;
                                    matched.IsCorrect = optDto.IsCorrect;
                                    keptOptionIds.Add(matched.Id);
                                }
                                else
                                {
                                    _context.QuestionOptions.Add(new QuestionOption
                                    {
                                        QuestionId = question.Id,
                                        OptionText = optDto.Text,
                                        IsCorrect = optDto.IsCorrect
                                    });
                                }
                            }

                            var toRemove = existingOptions.Where(o => !keptOptionIds.Contains(o.Id)).ToList();
                            if (toRemove.Any(o => referencedOptionIds.Contains(o.Id)))
                            {
                                await transaction.RollbackAsync(CancellationToken.None);
                                return this.ApiBadRequest("Không thể xóa lựa chọn vì đã có học viên chọn phương án đó.");
                            }
                            if (toRemove.Count > 0)
                            {
                                _context.QuestionOptions.RemoveRange(toRemove);
                            }
                        }
                        else
                        {
                            for (var i = 0; i < existingOptions.Count && i < cleanOptions.Count; i++)
                            {
                                existingOptions[i].OptionText = cleanOptions[i].Text;
                                existingOptions[i].IsCorrect = cleanOptions[i].IsCorrect;
                            }
                            if (cleanOptions.Count < existingOptions.Count)
                            {
                                var toRemove = existingOptions.Skip(cleanOptions.Count).ToList();
                                if (toRemove.Any(o => referencedOptionIds.Contains(o.Id)))
                                {
                                    await transaction.RollbackAsync(CancellationToken.None);
                                    return this.ApiBadRequest("Không thể xóa bớt lựa chọn vì đã có học viên chọn lựa chọn đó.");
                                }
                                _context.QuestionOptions.RemoveRange(toRemove);
                            }
                            else if (cleanOptions.Count > existingOptions.Count)
                            {
                                _context.QuestionOptions.AddRange(cleanOptions.Skip(existingOptions.Count).Select(o => new QuestionOption
                                {
                                    QuestionId = question.Id,
                                    OptionText = o.Text,
                                    IsCorrect = o.IsCorrect
                                }));
                            }
                        }
                    }
                    else
                    {
                        if (existingOptions.Count > 0)
                        {
                            _context.QuestionOptions.RemoveRange(existingOptions);
                        }
                        _context.QuestionOptions.AddRange(cleanOptions.Select(o => new QuestionOption
                        {
                            QuestionId = question.Id,
                            OptionText = o.Text,
                            IsCorrect = o.IsCorrect
                        }));
                    }
                }
                else
                {
                    if (existingOptions.Count > 0 && !hasAnyStudentAnswers)
                    {
                        _context.QuestionOptions.RemoveRange(existingOptions);
                    }

                    if (hasAnyStudentAnswers)
                    {
                        // Cập nhật tại chỗ để bảo toàn bản ghi tham chiếu khi đã có bài làm fill_blank / essay / pronunciation
                        if (!string.IsNullOrWhiteSpace(refAnswer))
                        {
                            if (existingRefs.Count > 0)
                            {
                                existingRefs[0].AnswerText = refAnswer;
                                existingRefs[0].IsCaseSensitive = dto.IsCaseSensitive ?? existingRefs[0].IsCaseSensitive;
                            }
                            else
                            {
                                _context.QuestionAnswers.Add(new QuestionAnswer
                                {
                                    QuestionId = question.Id,
                                    AnswerText = refAnswer,
                                    IsCaseSensitive = dto.IsCaseSensitive ?? false
                                });
                            }
                        }
                    }
                    else
                    {
                        var preservedCaseSensitive = existingRefs.Count > 0 && existingRefs[0].IsCaseSensitive;
                        var preservedRefAudio = existingRefs.FirstOrDefault()?.ReferenceAudioUrl;
                        if (existingRefs.Count > 0)
                        {
                            _context.QuestionAnswers.RemoveRange(existingRefs);
                        }
                        if (!string.IsNullOrWhiteSpace(refAnswer) || !string.IsNullOrWhiteSpace(preservedRefAudio))
                        {
                            _context.QuestionAnswers.Add(new QuestionAnswer
                            {
                                QuestionId = question.Id,
                                AnswerText = refAnswer,
                                ReferenceAudioUrl = preservedRefAudio,
                                IsCaseSensitive = dto.IsCaseSensitive ?? preservedCaseSensitive
                            });
                        }
                    }
                }

                try
                {
                    await _context.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiBadRequest($"Thứ tự câu hỏi số {newOrder} đã tồn tại trong đề thi này.");
                }
                catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation })
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiBadRequest("Không thể xóa lựa chọn vì đã có học viên chọn phương án đó.");
                }
                await transaction.CommitAsync(cancellationToken);

                return Ok(new
                {
                    success = true,
                    message = "Cập nhật câu hỏi thành công.",
                    examQuestionId = eq.Id
                });
            }
            catch (OperationCanceledException)
            {
                return this.ApiCancelled();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể cập nhật câu hỏi {ExamQuestionId} của đề {ExamId}.", examQuestionId, examId);
                await transaction.RollbackAsync(CancellationToken.None);
                return this.ApiServerError("Không thể cập nhật câu hỏi.");
            }
        }

        [HttpPost("/Admin/api/Exams/{examId}/Questions/Delete/{examQuestionId}")]
        [HttpDelete("/Admin/api/Exams/{examId}/Questions/Delete/{examQuestionId}")]
        public async Task<IActionResult> DeleteQuestion(int examId, int examQuestionId, CancellationToken cancellationToken)
        {
            if (examId <= 0 || examQuestionId <= 0)
            {
                return this.ApiBadRequest("Mã đề thi hoặc câu hỏi không hợp lệ.");
            }

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                long examLockKey = 900_000_000L + examId;
                await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({examLockKey})", cancellationToken);

                var eq = await _context.ExamQuestions
                    .FirstOrDefaultAsync(x => x.Id == examQuestionId && x.ExamId == examId, cancellationToken);
                if (eq == null)
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiNotFound("Không tìm thấy câu hỏi trong đề thi.");
                }

                var hasStudentAnswers = await (
                    from a in _context.UserExamAnswers.AsNoTracking()
                    join ue in _context.UserExams.AsNoTracking() on a.UserExamId equals ue.Id
                    where ue.ExamId == examId && a.QuestionId == eq.QuestionId
                    select a.Id
                ).AnyAsync(cancellationToken);

                if (hasStudentAnswers)
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiBadRequest("Không thể xóa câu hỏi đã có học viên làm bài.");
                }

                var questionId = eq.QuestionId;
                _context.ExamQuestions.Remove(eq);
                await _context.SaveChangesAsync(cancellationToken);

                var stillUsedElsewhere = await _context.ExamQuestions
                    .AsNoTracking()
                    .AnyAsync(x => x.QuestionId == questionId, cancellationToken)
                    || await _context.UserExamAnswers
                    .AsNoTracking()
                    .AnyAsync(a => a.QuestionId == questionId, cancellationToken);

                if (!stillUsedElsewhere)
                {
                    var opts = await _context.QuestionOptions.Where(o => o.QuestionId == questionId).ToListAsync(cancellationToken);
                    var refs = await _context.QuestionAnswers.Where(a => a.QuestionId == questionId).ToListAsync(cancellationToken);
                    var q = await _context.Questions.FirstOrDefaultAsync(x => x.Id == questionId, cancellationToken);

                    if (opts.Count > 0) _context.QuestionOptions.RemoveRange(opts);
                    if (refs.Count > 0) _context.QuestionAnswers.RemoveRange(refs);
                    if (q != null) _context.Questions.Remove(q);
                }

                var exam = await _context.Exams.FirstOrDefaultAsync(e => e.Id == examId, cancellationToken);
                if (exam != null)
                {
                    exam.UpdatedAt = DateTime.UtcNow;
                }

                try
                {
                    await _context.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation })
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return this.ApiBadRequest("Không thể xóa câu hỏi vì đang có bài làm của học viên tham chiếu.");
                }
                await transaction.CommitAsync(cancellationToken);

                return Ok(new
                {
                    success = true,
                    message = "Đã xóa câu hỏi khỏi đề thi."
                });
            }
            catch (OperationCanceledException)
            {
                return this.ApiCancelled();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể xóa câu hỏi {ExamQuestionId} khỏi đề {ExamId}.", examQuestionId, examId);
                await transaction.RollbackAsync(CancellationToken.None);
                return this.ApiServerError("Không thể xóa câu hỏi khỏi đề thi.");
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

        private static string? ValidateQuestionDto(
            ExamQuestionUpsertDto? dto,
            out string questionText,
            out string questionType,
            out List<(int? Id, string Text, bool IsCorrect)> cleanOptions,
            out string? refAnswer,
            out string? audioUrl)
        {
            questionText = string.Empty;
            questionType = "multiple_choice";
            cleanOptions = new List<(int?, string, bool)>();
            refAnswer = null;
            audioUrl = null;

            if (dto == null)
            {
                return "Dữ liệu câu hỏi không hợp lệ.";
            }

            questionText = (dto.QuestionText ?? string.Empty).Trim();
            questionType = (dto.QuestionType ?? "multiple_choice").Trim().ToLowerInvariant();
            refAnswer = string.IsNullOrWhiteSpace(dto.ReferenceAnswer) ? null : dto.ReferenceAnswer.Trim();
            audioUrl = string.IsNullOrWhiteSpace(dto.AudioUrl) ? null : dto.AudioUrl.Trim();

            if (questionText.Length == 0 || questionText.Length > MaxQuestionTextLength)
            {
                return $"Nội dung câu hỏi không hợp lệ (1 - {MaxQuestionTextLength} ký tự).";
            }

            if (!ValidQuestionTypes.Contains(questionType))
            {
                return "Loại câu hỏi không hợp lệ (multiple_choice, fill_blank, pronunciation, essay).";
            }

            if (dto.Points < 1 || dto.Points > 1000)
            {
                return "Điểm câu hỏi phải từ 1 đến 1.000.";
            }

            if (dto.OrderIndex.HasValue && (dto.OrderIndex.Value < 1 || dto.OrderIndex.Value > 1000))
            {
                return "Thứ tự câu hỏi phải từ 1 đến 1.000.";
            }

            if (questionType == "multiple_choice")
            {
                if (dto.Options == null)
                {
                    return "Câu hỏi trắc nghiệm cần ít nhất 2 phương án trả lời.";
                }

                cleanOptions = dto.Options
                    .Where(o => o != null && !string.IsNullOrWhiteSpace(o.OptionText))
                    .Select(o => (o.Id, o.OptionText!.Trim(), o.IsCorrect))
                    .ToList();

                if (cleanOptions.Count < 2)
                {
                    return "Câu hỏi trắc nghiệm phải có ít nhất 2 lựa chọn hợp lệ.";
                }

                if (!cleanOptions.Any(o => o.IsCorrect))
                {
                    return "Vui lòng chọn ít nhất 1 đáp án đúng cho câu hỏi trắc nghiệm.";
                }
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

        public class ExamQuestionOptionDto
        {
            public int? Id { get; set; }
            public string? OptionText { get; set; }
            public bool IsCorrect { get; set; }
        }

        public class ExamQuestionUpsertDto
        {
            public string? QuestionText { get; set; }
            public string? QuestionType { get; set; }
            public int Points { get; set; } = 10;
            public int? OrderIndex { get; set; }
            public string? AudioUrl { get; set; }
            public string? ReferenceAnswer { get; set; }
            public bool? IsCaseSensitive { get; set; }
            public List<ExamQuestionOptionDto>? Options { get; set; }
        }
    }
}
