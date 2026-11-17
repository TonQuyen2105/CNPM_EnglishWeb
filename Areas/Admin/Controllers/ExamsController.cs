using CNPM_EnglishLearning.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CNPM_EnglishLearning.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ExamsController : Controller
    {
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

                return Ok(new
                {
                    success = true,
                    items,
                    courses,
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
    }
}
