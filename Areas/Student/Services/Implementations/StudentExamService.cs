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
    /// Nhóm 6: Nghiệp vụ Hệ thống Thi trực tuyến, Tự động lưu bài, Chấm điểm tự động & Xem lại bài giải.
    /// </summary>
    public class StudentExamService : BaseStudentService, IStudentExamService
    {
        public StudentExamService(ApplicationDbContext db) : base(db)
        {
        }

        /// <summary>
        /// 6.1. Lấy danh sách bài thi hiện có (thuộc khóa học đã đăng ký hoặc bài thi tự do).
        /// </summary>
        public async Task<ServiceResult<List<ExamListItemDto>>> GetAvailableExamsAsync(int studentId, int? courseId)
        {
            var enrolledCourseIds = await (
                from e in Db.Enrollments.AsNoTracking()
                join cc in Db.CourseClasses.AsNoTracking() on e.ClassId equals cc.Id
                where e.UserId == studentId && e.Status != EnrollmentCancelled
                select cc.CourseId
            ).Distinct().ToListAsync();

            var examsQuery = Db.Exams.AsNoTracking().Where(ex => ex.IsPublished || (ex.CourseId.HasValue && enrolledCourseIds.Contains(ex.CourseId.Value)));

            if (courseId.HasValue && courseId.Value > 0)
            {
                var filterCourseId = courseId.Value;
                examsQuery = examsQuery.Where(ex => ex.CourseId == filterCourseId);
            }
            else if (enrolledCourseIds.Count > 0)
            {
                // Lấy các bài thi thuộc khóa đã đăng ký hoặc bài thi tự do; nếu DB mẫu ít dữ liệu thì hiển thị toàn bộ đề đã publish
                var matchingCount = await examsQuery.CountAsync(ex =>
                    ex.CourseId == null || enrolledCourseIds.Contains(ex.CourseId.Value));

                if (matchingCount > 0)
                {
                    examsQuery = examsQuery.Where(ex =>
                        ex.CourseId == null || enrolledCourseIds.Contains(ex.CourseId.Value));
                }
            }

            var exams = await (
                from ex in examsQuery
                join c in Db.Courses.AsNoTracking() on ex.CourseId equals c.Id into cGroup
                from c in cGroup.DefaultIfEmpty()
                orderby ex.CreatedAt descending, ex.Id
                select new
                {
                    Exam = ex,
                    CourseTitle = c != null ? c.Title : "Bài thi đánh giá năng lực chung"
                }
            ).ToListAsync();

            var examIds = exams.Select(x => x.Exam.Id).ToList();

            // Truy vấn các lớp mà học sinh đã ghi danh để kiểm tra trạng thái lớp (complete/cancel)
            var studentEnrolledClasses = await (
                from e in Db.Enrollments.AsNoTracking()
                join cc in Db.CourseClasses.AsNoTracking() on e.ClassId equals cc.Id
                where e.UserId == studentId
                select new
                {
                    cc.Id,
                    cc.CourseId,
                    cc.ClassName,
                    cc.Status
                }
            ).ToListAsync();

            var classesByCourse = studentEnrolledClasses
                .GroupBy(x => x.CourseId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Truy vấn gộp lịch sử làm bài của học sinh cho danh sách bài thi
            var userAttempts = examIds.Count == 0
                ? new List<UserExam>()
                : await Db.UserExams.AsNoTracking()
                    .Where(ue => ue.UserId == studentId && examIds.Contains(ue.ExamId))
                    .ToListAsync();

            var attemptsByExam = userAttempts
                .GroupBy(ue => ue.ExamId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var now = DateTime.UtcNow;

            var result = exams.Select(item =>
            {
                var ex = item.Exam;
                var attempts = attemptsByExam.GetValueOrDefault(ex.Id, new List<UserExam>());

                var completedAttempts = attempts.Where(a => a.Status != ExamInProgress).ToList();
                var activeAttempt = attempts
                    .Where(a => a.Status == ExamInProgress && a.StartedAt.AddMinutes(ex.DurationMinutes) > now)
                    .OrderByDescending(a => a.StartedAt)
                    .FirstOrDefault();

                var attemptCount = completedAttempts.Count;
                int? remainingAttempts = ex.MaxAttempts.HasValue
                    ? Math.Max(0, ex.MaxAttempts.Value - attemptCount)
                    : null;

                var bestScore = completedAttempts
                    .Where(a => a.TotalScore.HasValue)
                    .Select(a => a.TotalScore!.Value)
                    .DefaultIfEmpty()
                    .Max();

                var hasScore = completedAttempts.Any(a => a.TotalScore.HasValue);

                // Kiểm tra trạng thái lớp học gắn với khóa học của bài thi
                var courseClasses = ex.CourseId.HasValue
                    ? classesByCourse.GetValueOrDefault(ex.CourseId.Value, new())
                    : new();

                var isClassClosed = false;
                string? classStatus = null;
                if (courseClasses.Count > 0)
                {
                    var hasOpenClass = courseClasses.Any(c =>
                        !string.Equals(c.Status, "completed", StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(c.Status, "complete", StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(c.Status, "cancelled", StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(c.Status, "canceled", StringComparison.OrdinalIgnoreCase));

                    isClassClosed = !hasOpenClass;
                    classStatus = courseClasses.FirstOrDefault()?.Status;
                }

                var canStart = activeAttempt != null || !ex.MaxAttempts.HasValue || attemptCount < ex.MaxAttempts.Value;
                if (isClassClosed)
                {
                    // Lớp đã complete hoặc cancel: Không được vào thi mới (nếu chưa thi hoặc thi rồi đều canStart = false)
                    canStart = false;
                }

                return new ExamListItemDto
                {
                    ExamId = ex.Id,
                    CourseId = ex.CourseId,
                    CourseTitle = item.CourseTitle,
                    Title = ex.Title,
                    Description = ex.Description,
                    DurationMinutes = ex.DurationMinutes,
                    PassScore = ex.PassScore,
                    MaxAttempts = ex.MaxAttempts,
                    AttemptCount = attemptCount,
                    RemainingAttempts = remainingAttempts,
                    BestScore = hasScore ? bestScore : null,
                    InProgressUserExamId = activeAttempt?.Id,
                    CanStart = canStart,
                    IsClassClosed = isClassClosed,
                    ClassStatus = classStatus
                };
            }).ToList();

            return ServiceResult<List<ExamListItemDto>>.Ok(result, "Lấy danh sách bài thi hiện có thành công.");
        }

        /// <summary>
        /// 6.2. Bắt đầu làm bài thi (Khởi tạo phiên thi hoặc khôi phục phiên đang làm dở).
        /// </summary>
        public async Task<ServiceResult<ExamSessionStartDto>> StartExamSessionAsync(int studentId, int examId)
        {
            var enrolledCourseIds = await (
                from e in Db.Enrollments.AsNoTracking()
                join cc in Db.CourseClasses.AsNoTracking() on e.ClassId equals cc.Id
                where e.UserId == studentId && e.Status != EnrollmentCancelled
                select cc.CourseId
            ).Distinct().ToListAsync();

            var exam = await Db.Exams.AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == examId && (e.IsPublished || (e.CourseId.HasValue && enrolledCourseIds.Contains(e.CourseId.Value))));

            if (exam == null)
            {
                return ServiceResult<ExamSessionStartDto>.NotFound($"Không tìm thấy bài thi có ID = {examId} hoặc bài thi chưa được mở.");
            }

            var now = DateTime.UtcNow;

            // Kiểm tra các phiên thi trước đó của học sinh đối với đề thi này
            var existingUserExams = await Db.UserExams
                .Where(ue => ue.UserId == studentId && ue.ExamId == examId)
                .OrderByDescending(ue => ue.StartedAt)
                .ToListAsync();

            // Đóng tự động các phiên in_progress đã quá thời gian làm bài
            var expiredSessions = existingUserExams
                .Where(ue => ue.Status == ExamInProgress && ue.StartedAt.AddMinutes(exam.DurationMinutes) <= now)
                .ToList();

            if (expiredSessions.Count > 0)
            {
                foreach (var expired in expiredSessions)
                {
                    expired.SubmittedAt = expired.StartedAt.AddMinutes(exam.DurationMinutes);
                    expired.TotalScore ??= 0m;
                    expired.Status = ExamFailed;
                }
                await Db.SaveChangesAsync();
            }

            // Nếu vẫn còn phiên in_progress chưa hết giờ thì trả về phiên đó kèm các đáp án nháp đã lưu
            var activeSession = existingUserExams
                .FirstOrDefault(ue => ue.Status == ExamInProgress && ue.StartedAt.AddMinutes(exam.DurationMinutes) > now);

            if (activeSession == null)
            {
                // Ràng buộc: Nếu bài thi thuộc khóa học mà học viên có lớp đã complete hoặc cancel thì không được vào thi
                if (exam.CourseId.HasValue)
                {
                    var enrolledClasses = await (
                        from e in Db.Enrollments.AsNoTracking()
                        join cc in Db.CourseClasses.AsNoTracking() on e.ClassId equals cc.Id
                        where e.UserId == studentId && cc.CourseId == exam.CourseId.Value
                        select cc
                    ).ToListAsync();

                    if (enrolledClasses.Count > 0)
                    {
                        var hasOpenClass = enrolledClasses.Any(c =>
                            !string.Equals(c.Status, "completed", StringComparison.OrdinalIgnoreCase) &&
                            !string.Equals(c.Status, "complete", StringComparison.OrdinalIgnoreCase) &&
                            !string.Equals(c.Status, "cancelled", StringComparison.OrdinalIgnoreCase) &&
                            !string.Equals(c.Status, "canceled", StringComparison.OrdinalIgnoreCase));

                        if (!hasOpenClass)
                        {
                            return ServiceResult<ExamSessionStartDto>.BadRequest(
                                "Lớp học của bài thi này đã kết thúc hoặc bị hủy, bạn không thể tham gia làm bài thi.");
                        }
                    }
                }

                var finishedAttemptCount = existingUserExams.Count(ue => ue.Status != ExamInProgress);
                if (exam.MaxAttempts.HasValue && finishedAttemptCount >= exam.MaxAttempts.Value)
                {
                    return ServiceResult<ExamSessionStartDto>.BadRequest(
                        $"Bạn đã sử dụng hết số lượt làm bài tối đa ({exam.MaxAttempts.Value} lần) cho bài thi '{exam.Title}'.");
                }

                var nextUserExamId = await GetNextIdAsync(Db.UserExams, ue => ue.Id);
                activeSession = new UserExam
                {
                    Id = nextUserExamId,
                    UserId = studentId,
                    ExamId = exam.Id,
                    AttemptNumber = finishedAttemptCount + 1,
                    StartedAt = now,
                    SubmittedAt = null,
                    TotalScore = null,
                    Status = ExamInProgress
                };

                Db.UserExams.Add(activeSession);
                await Db.SaveChangesAsync();
            }

            // Truy vấn danh sách câu hỏi của đề thi (sắp xếp theo order_index)
            var examQuestions = await (
                from eq in Db.ExamQuestions.AsNoTracking()
                join q in Db.Questions.AsNoTracking() on eq.QuestionId equals q.Id
                where eq.ExamId == examId
                orderby eq.OrderIndex, eq.Id
                select new
                {
                    eq.QuestionId,
                    eq.OrderIndex,
                    eq.Points,
                    q.QuestionType,
                    q.QuestionText,
                    q.AudioUrl
                }
            ).ToListAsync();

            var questionIds = examQuestions.Select(x => x.QuestionId).ToList();

            // Batch-load các lựa chọn (TUYỆT ĐỐI KHÔNG lấy cột IsCorrect trả về cho học sinh khi đang thi)
            var optionsByQuestion = questionIds.Count == 0
                ? new Dictionary<int, List<QuestionOptionSafeDto>>()
                : (await Db.QuestionOptions.AsNoTracking()
                    .Where(opt => questionIds.Contains(opt.QuestionId))
                    .OrderBy(opt => opt.Id)
                    .Select(opt => new QuestionOptionSafeDto
                    {
                        Id = opt.Id,
                        QuestionId = opt.QuestionId,
                        OptionText = opt.OptionText
                    })
                    .ToListAsync())
                    .GroupBy(opt => opt.QuestionId)
                    .ToDictionary(g => g.Key, g => g.ToList());

            // Batch-load các đáp án nháp đã lưu của phiên thi hiện tại (nếu có)
            var savedAnswersMap = questionIds.Count == 0
                ? new Dictionary<int, UserExamAnswer>()
                : (await Db.UserExamAnswers.AsNoTracking()
                    .Where(ans => ans.UserExamId == activeSession.Id && questionIds.Contains(ans.QuestionId))
                    .ToListAsync())
                    .GroupBy(ans => ans.QuestionId)
                    .ToDictionary(g => g.Key, g => g.Last());

            var questionDtos = examQuestions.Select(eq =>
            {
                savedAnswersMap.TryGetValue(eq.QuestionId, out var savedAns);

                return new ExamQuestionSessionDto
                {
                    QuestionId = eq.QuestionId,
                    OrderIndex = eq.OrderIndex,
                    Points = eq.Points,
                    QuestionType = eq.QuestionType,
                    QuestionText = eq.QuestionText,
                    AudioUrl = eq.AudioUrl,
                    Options = optionsByQuestion.GetValueOrDefault(eq.QuestionId, new List<QuestionOptionSafeDto>()),
                    SavedSelectedOptionId = savedAns?.SelectedOptionId,
                    SavedTextAnswer = savedAns?.TextAnswer
                };
            }).ToList();

            var expiresAt = activeSession.StartedAt.AddMinutes(exam.DurationMinutes);
            var remainingSeconds = (int)Math.Max(0, (expiresAt - now).TotalSeconds);

            var sessionDto = new ExamSessionStartDto
            {
                UserExamId = activeSession.Id,
                ExamId = exam.Id,
                ExamTitle = exam.Title,
                AttemptNumber = activeSession.AttemptNumber,
                DurationMinutes = exam.DurationMinutes,
                StartedAt = activeSession.StartedAt,
                ExpiresAt = expiresAt,
                RemainingSeconds = remainingSeconds,
                Status = activeSession.Status,
                Questions = questionDtos
            };

            return ServiceResult<ExamSessionStartDto>.Ok(sessionDto, "Khởi tạo phiên làm bài thi thành công.");
        }

        /// <summary>
        /// 6.3. Lưu nháp câu trả lời trong lúc thi (Auto-save khi chọn đáp án hoặc chuyển câu).
        /// </summary>
        public async Task<ServiceResult<SaveExamAnswerResponseDto>> SaveExamAnswerAsync(
            int studentId,
            int userExamId,
            SaveExamAnswerRequestDto request)
        {
            var sessionInfo = await (
                from ue in Db.UserExams
                join ex in Db.Exams.AsNoTracking() on ue.ExamId equals ex.Id
                where ue.Id == userExamId
                select new
                {
                    UserExam = ue,
                    ex.DurationMinutes
                }
            ).FirstOrDefaultAsync();

            if (sessionInfo == null)
            {
                return ServiceResult<SaveExamAnswerResponseDto>.NotFound($"Không tìm thấy phiên thi có ID = {userExamId}.");
            }

            var userExam = sessionInfo.UserExam;
            if (userExam.UserId != studentId)
            {
                return ServiceResult<SaveExamAnswerResponseDto>.Forbidden("Bạn không có quyền thao tác trên phiên thi của học viên khác.");
            }

            if (userExam.Status != ExamInProgress)
            {
                return ServiceResult<SaveExamAnswerResponseDto>.BadRequest("Phiên thi này đã kết thúc, không thể lưu thay đổi đáp án.");
            }

            var expiresAt = userExam.StartedAt.AddMinutes(sessionInfo.DurationMinutes);
            if (DateTime.UtcNow > expiresAt.AddMinutes(1)) // Ân hạn 1 phút cho độ trễ mạng
            {
                return ServiceResult<SaveExamAnswerResponseDto>.BadRequest("Đã hết thời gian làm bài thi.");
            }

            // Kiểm tra câu hỏi có thuộc đề thi này không
            var questionInExam = await Db.ExamQuestions.AsNoTracking()
                .AnyAsync(eq => eq.ExamId == userExam.ExamId && eq.QuestionId == request.QuestionId);

            if (!questionInExam)
            {
                return ServiceResult<SaveExamAnswerResponseDto>.BadRequest(
                    $"Câu hỏi ID = {request.QuestionId} không thuộc đề thi của phiên làm bài này.");
            }

            var answerEntity = await Db.UserExamAnswers
                .FirstOrDefaultAsync(a => a.UserExamId == userExamId && a.QuestionId == request.QuestionId);

            if (answerEntity != null)
            {
                answerEntity.SelectedOptionId = request.SelectedOptionId;
                answerEntity.TextAnswer = request.TextAnswer;
            }
            else
            {
                var nextAnswerId = await GetNextIdAsync(Db.UserExamAnswers, a => a.Id);
                answerEntity = new UserExamAnswer
                {
                    Id = nextAnswerId,
                    UserExamId = userExamId,
                    QuestionId = request.QuestionId,
                    SelectedOptionId = request.SelectedOptionId,
                    TextAnswer = request.TextAnswer,
                    GradingStatus = GradingAutoGraded
                };
                Db.UserExamAnswers.Add(answerEntity);
            }

            await Db.SaveChangesAsync();

            var response = new SaveExamAnswerResponseDto
            {
                UserExamId = userExamId,
                QuestionId = request.QuestionId,
                SelectedOptionId = answerEntity.SelectedOptionId,
                TextAnswer = answerEntity.TextAnswer,
                SavedAt = DateTime.UtcNow
            };

            return ServiceResult<SaveExamAnswerResponseDto>.Ok(response, "Đã lưu nháp câu trả lời thành công.");
        }

        /// <summary>
        /// 6.4. Nộp bài thi & Chấm điểm tự động (multiple_choice, fill_blank; chuyển pending_review nếu có bài luận essay).
        /// </summary>
        public async Task<ServiceResult<SubmitExamResultDto>> SubmitExamAsync(int studentId, int userExamId)
        {
            var userExam = await Db.UserExams.FirstOrDefaultAsync(ue => ue.Id == userExamId);
            if (userExam == null)
            {
                return ServiceResult<SubmitExamResultDto>.NotFound($"Không tìm thấy phiên thi có ID = {userExamId}.");
            }

            if (userExam.UserId != studentId)
            {
                return ServiceResult<SubmitExamResultDto>.Forbidden("Bạn không có quyền nộp bài thi của học viên khác.");
            }

            if (userExam.Status != ExamInProgress)
            {
                return ServiceResult<SubmitExamResultDto>.BadRequest("Bài thi này đã được nộp trước đó.");
            }

            var exam = await Db.Exams.AsNoTracking().FirstOrDefaultAsync(e => e.Id == userExam.ExamId);
            if (exam == null)
            {
                return ServiceResult<SubmitExamResultDto>.NotFound("Không tìm thấy thông tin đề thi.");
            }

            var now = DateTime.UtcNow;

            // 1. Tải toàn bộ cấu trúc câu hỏi trong đề thi
            var examQuestions = await (
                from eq in Db.ExamQuestions.AsNoTracking()
                join q in Db.Questions.AsNoTracking() on eq.QuestionId equals q.Id
                where eq.ExamId == exam.Id
                orderby eq.OrderIndex
                select new
                {
                    eq.QuestionId,
                    eq.Points,
                    q.QuestionType
                }
            ).ToListAsync();

            var questionIds = examQuestions.Select(x => x.QuestionId).ToList();

            // 2. Batch-load đáp án đúng trắc nghiệm (question_options.is_correct = true)
            var correctOptionIdsByQuestion = questionIds.Count == 0
                ? new Dictionary<int, HashSet<int>>()
                : (await Db.QuestionOptions.AsNoTracking()
                    .Where(opt => questionIds.Contains(opt.QuestionId) && opt.IsCorrect)
                    .Select(opt => new { opt.QuestionId, opt.Id })
                    .ToListAsync())
                    .GroupBy(x => x.QuestionId)
                    .ToDictionary(g => g.Key, g => g.Select(x => x.Id).ToHashSet());

            // 3. Batch-load đáp án đúng điền từ (question_answers)
            var fillBlankAnswersByQuestion = questionIds.Count == 0
                ? new Dictionary<int, List<QuestionAnswer>>()
                : (await Db.QuestionAnswers.AsNoTracking()
                    .Where(qa => questionIds.Contains(qa.QuestionId))
                    .ToListAsync())
                    .GroupBy(qa => qa.QuestionId)
                    .ToDictionary(g => g.Key, g => g.ToList());

            // 4. Tải toàn bộ câu trả lời đã lưu của học sinh trong phiên thi này
            var userAnswers = await Db.UserExamAnswers
                .Where(ans => ans.UserExamId == userExamId)
                .ToListAsync();

            var userAnswerByQuestion = userAnswers
                .GroupBy(a => a.QuestionId)
                .ToDictionary(g => g.Key, g => g.Last());

            var nextAnswerId = await GetNextIdAsync(Db.UserExamAnswers, a => a.Id);

            decimal rawEarnedPoints = 0m;
            decimal maxPossiblePoints = examQuestions.Sum(x => x.Points);
            int autoGradedCount = 0;
            int pendingReviewCount = 0;
            int correctCount = 0;

            foreach (var eq in examQuestions)
            {
                if (!userAnswerByQuestion.TryGetValue(eq.QuestionId, out var answerEntity))
                {
                    answerEntity = new UserExamAnswer
                    {
                        Id = nextAnswerId++,
                        UserExamId = userExamId,
                        QuestionId = eq.QuestionId
                    };
                    Db.UserExamAnswers.Add(answerEntity);
                }

                var qType = (eq.QuestionType ?? QuestionTypeMultipleChoice).Trim().ToLowerInvariant();

                if (qType == QuestionTypeMultipleChoice)
                {
                    autoGradedCount++;
                    var isCorrect = answerEntity.SelectedOptionId.HasValue &&
                                    correctOptionIdsByQuestion.TryGetValue(eq.QuestionId, out var validOptionIds) &&
                                    validOptionIds.Contains(answerEntity.SelectedOptionId.Value);

                    answerEntity.ScoreEarned = isCorrect ? eq.Points : 0m;
                    answerEntity.GradingStatus = GradingAutoGraded;
                    answerEntity.GradedAt = now;

                    if (isCorrect)
                    {
                        correctCount++;
                        rawEarnedPoints += eq.Points;
                    }
                }
                else if (qType == QuestionTypeFillBlank)
                {
                    autoGradedCount++;
                    var studentText = answerEntity.TextAnswer?.Trim() ?? string.Empty;
                    var isCorrect = false;

                    if (!string.IsNullOrEmpty(studentText) &&
                        fillBlankAnswersByQuestion.TryGetValue(eq.QuestionId, out var expectedAnswers))
                    {
                        isCorrect = expectedAnswers.Any(exp =>
                        {
                            var expectedText = exp.AnswerText?.Trim() ?? string.Empty;
                            var comparison = exp.IsCaseSensitive
                                ? StringComparison.Ordinal
                                : StringComparison.OrdinalIgnoreCase;
                            return string.Equals(studentText, expectedText, comparison);
                        });
                    }

                    answerEntity.ScoreEarned = isCorrect ? eq.Points : 0m;
                    answerEntity.GradingStatus = GradingAutoGraded;
                    answerEntity.GradedAt = now;

                    if (isCorrect)
                    {
                        correctCount++;
                        rawEarnedPoints += eq.Points;
                    }
                }
                else if (qType == QuestionTypeEssay)
                {
                    pendingReviewCount++;
                    answerEntity.ScoreEarned = null;
                    answerEntity.GradingStatus = GradingPendingReview;
                }
                else
                {
                    autoGradedCount++;
                    answerEntity.ScoreEarned = 0m;
                    answerEntity.GradingStatus = GradingAutoGraded;
                    answerEntity.GradedAt = now;
                }
            }

            // Tính tổng điểm (chuẩn hóa thang điểm tương thích với pass_score của đề thi)
            decimal calculatedScore = rawEarnedPoints;
            if (maxPossiblePoints > 0 && exam.PassScore <= 10m && maxPossiblePoints > 10m)
            {
                calculatedScore = Math.Round((rawEarnedPoints / maxPossiblePoints) * 10m, 2);
            }
            else
            {
                calculatedScore = Math.Round(rawEarnedPoints, 2);
            }

            userExam.SubmittedAt = now;
            userExam.TotalScore = calculatedScore;

            string overallGradingStatus;
            if (pendingReviewCount > 0)
            {
                userExam.Status = ExamSubmitted;
                overallGradingStatus = GradingPendingReview;
            }
            else
            {
                overallGradingStatus = GradingAutoGraded;
                userExam.Status = calculatedScore >= exam.PassScore ? ExamPassed : ExamFailed;
            }

            await Db.SaveChangesAsync();

            var durationSeconds = (int)Math.Max(0, (now - userExam.StartedAt).TotalSeconds);

            var resultDto = new SubmitExamResultDto
            {
                UserExamId = userExam.Id,
                ExamId = exam.Id,
                ExamTitle = exam.Title,
                StartedAt = userExam.StartedAt,
                SubmittedAt = now,
                DurationSeconds = durationSeconds,
                TotalQuestions = examQuestions.Count,
                AutoGradedQuestions = autoGradedCount,
                PendingReviewQuestions = pendingReviewCount,
                CorrectAnswersCount = correctCount,
                TotalScore = userExam.TotalScore,
                PassScore = exam.PassScore,
                Status = userExam.Status,
                GradingStatus = overallGradingStatus
            };

            return ServiceResult<SubmitExamResultDto>.Ok(resultDto, "Nộp bài thi và chấm điểm thành công.");
        }

        /// <summary>
        /// 6.5. Xem lịch sử các bài thi đã nộp.
        /// </summary>
        public async Task<ServiceResult<List<UserExamHistoryItemDto>>> GetExamHistoryAsync(int studentId)
        {
            var rawHistory = await (
                from ue in Db.UserExams.AsNoTracking()
                join ex in Db.Exams.AsNoTracking() on ue.ExamId equals ex.Id
                join c in Db.Courses.AsNoTracking() on ex.CourseId equals c.Id into cGroup
                from c in cGroup.DefaultIfEmpty()
                where ue.UserId == studentId && ue.Status != ExamInProgress
                orderby ue.SubmittedAt ?? ue.StartedAt descending, ue.Id descending
                select new
                {
                    ue.Id,
                    ue.ExamId,
                    ExamTitle = ex.Title,
                    ex.CourseId,
                    CourseTitle = c != null ? c.Title : "Bài thi chung",
                    ue.AttemptNumber,
                    ue.StartedAt,
                    ue.SubmittedAt,
                    ue.TotalScore,
                    ex.PassScore,
                    ue.Status
                }
            ).ToListAsync();

            var userExamIds = rawHistory.Select(x => x.Id).ToList();
            var examIds = rawHistory.Select(x => x.ExamId).Distinct().ToList();

            // Batch-load các lớp học mà học sinh đang tham gia theo CourseId
            var studentClasses = await (
                from e in Db.Enrollments.AsNoTracking()
                join cc in Db.CourseClasses.AsNoTracking() on e.ClassId equals cc.Id
                where e.UserId == studentId && e.Status != EnrollmentCancelled
                select new
                {
                    cc.Id,
                    cc.CourseId,
                    cc.ClassName,
                    cc.ClassCode
                }
            ).ToListAsync();

            var classByCourse = studentClasses
                .GroupBy(x => x.CourseId)
                .ToDictionary(g => g.Key, g => g.First());

            // Batch-load tổng số câu hỏi của từng đề thi
            var totalQuestionsByExam = examIds.Count == 0
                ? new Dictionary<int, int>()
                : (await Db.ExamQuestions.AsNoTracking()
                    .Where(eq => examIds.Contains(eq.ExamId))
                    .GroupBy(eq => eq.ExamId)
                    .Select(g => new { ExamId = g.Key, Count = g.Count() })
                    .ToListAsync())
                    .ToDictionary(x => x.ExamId, x => x.Count);

            // Batch-load số câu trả lời đúng (score_earned > 0) của từng phiên thi
            var correctCountByUserExam = userExamIds.Count == 0
                ? new Dictionary<int, int>()
                : (await Db.UserExamAnswers.AsNoTracking()
                    .Where(ans => userExamIds.Contains(ans.UserExamId) && ans.ScoreEarned != null && ans.ScoreEarned > 0)
                    .GroupBy(ans => ans.UserExamId)
                    .Select(g => new { UserExamId = g.Key, CorrectCount = g.Count() })
                    .ToListAsync())
                    .ToDictionary(x => x.UserExamId, x => x.CorrectCount);

            var historyDtos = rawHistory.Select(item =>
            {
                var endTime = item.SubmittedAt ?? item.StartedAt;
                var durationSec = (int)Math.Max(0, (endTime - item.StartedAt).TotalSeconds);
                var isGraded = (item.Status == ExamPassed || item.Status == ExamFailed);

                var enrolledClass = item.CourseId.HasValue 
                    ? classByCourse.GetValueOrDefault(item.CourseId.Value) 
                    : null;

                return new UserExamHistoryItemDto
                {
                    UserExamId = item.Id,
                    ExamId = item.ExamId,
                    ExamTitle = item.ExamTitle,
                    CourseId = item.CourseId,
                    CourseTitle = item.CourseTitle,
                    ClassId = enrolledClass?.Id,
                    ClassName = enrolledClass?.ClassName,
                    ClassCode = enrolledClass?.ClassCode,
                    AttemptNumber = item.AttemptNumber,
                    StartedAt = item.StartedAt,
                    SubmittedAt = item.SubmittedAt,
                    DurationSeconds = durationSec,
                    CorrectAnswersCount = isGraded ? correctCountByUserExam.GetValueOrDefault(item.Id, 0) : 0,
                    TotalQuestions = totalQuestionsByExam.GetValueOrDefault(item.ExamId, 0),
                    TotalScore = isGraded ? item.TotalScore : null,
                    PassScore = item.PassScore,
                    Status = item.Status
                };
            }).ToList();

            return ServiceResult<List<UserExamHistoryItemDto>>.Ok(historyDtos, "Lấy lịch sử các bài thi đã nộp thành công.");
        }

        /// <summary>
        /// 6.6. Xem lại bài giải và kết quả chi tiết từng câu (Chỉ cho phép gọi khi status != 'in_progress').
        /// </summary>
        public async Task<ServiceResult<UserExamReviewDetailDto>> GetExamReviewAsync(int studentId, int userExamId)
        {
            var headerInfo = await (
                from ue in Db.UserExams.AsNoTracking()
                join ex in Db.Exams.AsNoTracking() on ue.ExamId equals ex.Id
                join c in Db.Courses.AsNoTracking() on ex.CourseId equals c.Id into cGroup
                from c in cGroup.DefaultIfEmpty()
                where ue.Id == userExamId
                select new
                {
                    UserExam = ue,
                    Exam = ex,
                    CourseTitle = c != null ? c.Title : "Bài thi chung"
                }
            ).FirstOrDefaultAsync();

            if (headerInfo == null)
            {
                return ServiceResult<UserExamReviewDetailDto>.NotFound($"Không tìm thấy kết quả bài thi có ID = {userExamId}.");
            }

            var userExam = headerInfo.UserExam;
            var exam = headerInfo.Exam;

            if (userExam.UserId != studentId)
            {
                return ServiceResult<UserExamReviewDetailDto>.Forbidden("Bạn không có quyền xem chi tiết bài thi của học viên khác.");
            }

            if (userExam.Status == ExamInProgress)
            {
                return ServiceResult<UserExamReviewDetailDto>.BadRequest(
                    "Phiên thi đang trong quá trình làm bài (in_progress). Bạn chỉ có thể xem lại đáp án sau khi đã nộp bài.");
            }

            if (userExam.Status == ExamSubmitted)
            {
                return ServiceResult<UserExamReviewDetailDto>.BadRequest("Bài thi đang chờ chấm điểm.");
            }

            // 1. Lấy danh sách câu hỏi của đề thi
            var examQuestions = await (
                from eq in Db.ExamQuestions.AsNoTracking()
                join q in Db.Questions.AsNoTracking() on eq.QuestionId equals q.Id
                where eq.ExamId == exam.Id
                orderby eq.OrderIndex, eq.Id
                select new
                {
                    eq.QuestionId,
                    eq.OrderIndex,
                    MaxPoints = eq.Points,
                    q.QuestionType,
                    q.QuestionText,
                    q.AudioUrl
                }
            ).ToListAsync();

            var questionIds = examQuestions.Select(x => x.QuestionId).ToList();

            // 2. Batch-load toàn bộ options (bao gồm cả IsCorrect vì bài đã nộp)
            var optionsByQuestion = questionIds.Count == 0
                ? new Dictionary<int, List<QuestionOptionReviewDto>>()
                : (await Db.QuestionOptions.AsNoTracking()
                    .Where(opt => questionIds.Contains(opt.QuestionId))
                    .OrderBy(opt => opt.Id)
                    .Select(opt => new
                    {
                        opt.QuestionId,
                        Option = new QuestionOptionReviewDto
                        {
                            Id = opt.Id,
                            OptionText = opt.OptionText,
                            IsCorrect = opt.IsCorrect
                        }
                    })
                    .ToListAsync())
                    .GroupBy(x => x.QuestionId)
                    .ToDictionary(g => g.Key, g => g.Select(x => x.Option).ToList());

            // 3. Batch-load đáp án chuẩn cho câu điền từ / phát âm (question_answers)
            var referenceAnswersByQuestion = questionIds.Count == 0
                ? new Dictionary<int, QuestionAnswer>()
                : (await Db.QuestionAnswers.AsNoTracking()
                    .Where(qa => questionIds.Contains(qa.QuestionId))
                    .ToListAsync())
                    .GroupBy(qa => qa.QuestionId)
                    .ToDictionary(g => g.Key, g => g.First());

            // 4. Batch-load bài làm của học sinh (user_exam_answers)
            var studentAnswersByQuestion = questionIds.Count == 0
                ? new Dictionary<int, UserExamAnswer>()
                : (await Db.UserExamAnswers.AsNoTracking()
                    .Where(ans => ans.UserExamId == userExamId && questionIds.Contains(ans.QuestionId))
                    .ToListAsync())
                    .GroupBy(ans => ans.QuestionId)
                    .ToDictionary(g => g.Key, g => g.Last());

            var questionReviews = examQuestions.Select(eq =>
            {
                var options = optionsByQuestion.GetValueOrDefault(eq.QuestionId, new List<QuestionOptionReviewDto>());
                var correctOption = options.FirstOrDefault(o => o.IsCorrect);
                referenceAnswersByQuestion.TryGetValue(eq.QuestionId, out var refAnswer);
                studentAnswersByQuestion.TryGetValue(eq.QuestionId, out var userAns);

                bool? isCorrect = null;
                if (userAns?.ScoreEarned != null)
                {
                    isCorrect = userAns.ScoreEarned > 0;
                }

                return new UserExamQuestionReviewItemDto
                {
                    QuestionId = eq.QuestionId,
                    OrderIndex = eq.OrderIndex,
                    QuestionType = eq.QuestionType,
                    QuestionText = eq.QuestionText,
                    AudioUrl = eq.AudioUrl,
                    MaxPoints = eq.MaxPoints,
                    ScoreEarned = userAns?.ScoreEarned,
                    IsCorrect = isCorrect,
                    GradingStatus = userAns?.GradingStatus ?? GradingAutoGraded,
                    Feedback = userAns?.Feedback,
                    SelectedOptionId = userAns?.SelectedOptionId,
                    TextAnswer = userAns?.TextAnswer,
                    CorrectOptionId = correctOption?.Id,
                    CorrectAnswerText = refAnswer?.AnswerText ?? correctOption?.OptionText,
                    ReferenceAudioUrl = refAnswer?.ReferenceAudioUrl,
                    Options = options
                };
            }).ToList();

            var endTime = userExam.SubmittedAt ?? userExam.StartedAt;
            var durationSeconds = (int)Math.Max(0, (endTime - userExam.StartedAt).TotalSeconds);

            CourseClass? enrolledClass = null;
            if (exam.CourseId.HasValue)
            {
                enrolledClass = await (
                    from e in Db.Enrollments.AsNoTracking()
                    join cc in Db.CourseClasses.AsNoTracking() on e.ClassId equals cc.Id
                    where e.UserId == studentId && cc.CourseId == exam.CourseId.Value && e.Status != EnrollmentCancelled
                    select cc
                ).FirstOrDefaultAsync();
            }

            var reviewDto = new UserExamReviewDetailDto
            {
                UserExamId = userExam.Id,
                ExamId = exam.Id,
                ExamTitle = exam.Title,
                CourseId = exam.CourseId,
                CourseTitle = headerInfo.CourseTitle,
                ClassId = enrolledClass?.Id,
                ClassName = enrolledClass?.ClassName,
                ClassCode = enrolledClass?.ClassCode,
                AttemptNumber = userExam.AttemptNumber,
                StartedAt = userExam.StartedAt,
                SubmittedAt = userExam.SubmittedAt,
                DurationSeconds = durationSeconds,
                TotalScore = userExam.TotalScore,
                PassScore = exam.PassScore,
                Status = userExam.Status,
                Questions = questionReviews
            };

            return ServiceResult<UserExamReviewDetailDto>.Ok(reviewDto, "Lấy chi tiết bài giải và kết quả thành công.");
        }
    }
}
