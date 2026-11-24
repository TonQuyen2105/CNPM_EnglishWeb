using CNPM_EnglishLearning.Services.Implementations;
using CNPM_EnglishLearning.Services.Interfaces;
using System.Security.Claims;
using CNPM_EnglishLearning.Data;
using CNPM_EnglishLearning.Models.Entities;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CNPM_EnglishLearning.Areas.Teacher.Controllers;

[Authorize(Roles = "teacher")]
[TypeFilter(typeof(TeacherGradingExceptionFilter))]
[Route("api/teacher/grading-workspace")]
public class TeacherGradingController(ApplicationDbContext db, IAntiforgery antiforgery, IAutomaticGradingService autoGrading) : ControllerBase
{
    private async Task<int?> TeacherId()
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return null;
        return await (from t in db.Teachers join u in db.Users on t.UserId equals u.Id
            where u.Id == userId && u.Role == "teacher" && u.IsActive select (int?)t.Id).SingleOrDefaultAsync();
    }

    [HttpGet]
    public async Task<IActionResult> Get(int? classId, int? examId)
    {
        var teacherId = await TeacherId();
        if (teacherId == null) return Forbid();
        var classes = db.CourseClasses.Where(c => db.ClassTeachers.Any(t => t.ClassId == c.Id && t.TeacherId == teacherId));
        if (classId == null) return Ok(await classes.OrderBy(c => c.ClassCode).Select(c => new {
            c.Id, c.ClassCode, c.ClassName, c.Status,
            Students = db.Enrollments.Count(e => e.ClassId == c.Id && e.Status != "cancelled")
        }).ToListAsync());
        var selected = await classes.SingleOrDefaultAsync(c => c.Id == classId);
        if (selected == null) return NotFound();
        var exams = await db.Exams.Where(e => e.CourseId == selected.CourseId && e.CreatedByTeacherId == teacherId && e.IsPublished
            && db.ExamQuestions.Any(eq => eq.ExamId == e.Id && db.Questions.Any(q => q.Id == eq.QuestionId && (q.QuestionType == "essay" || q.QuestionType == "multiple_choice" || q.QuestionType == "fill_blank"))))
            .OrderBy(e => e.Title).Select(e => new { e.Id, e.Title }).ToListAsync();
        var chosen = examId ?? exams.FirstOrDefault()?.Id;
        if (chosen != null && !exams.Any(e => e.Id == chosen)) return NotFound();
        var users = await db.Users.Where(u => db.Enrollments.Any(e => e.ClassId == classId && e.UserId == u.Id && e.Status != "cancelled"))
            .OrderBy(u => u.FullName).Select(u => new { u.Id, u.FullName, u.Email }).ToListAsync();
        // Explicit LINQ overload avoids the span-based array overload in expression trees.
        var ids = users.Select(u => u.Id).ToArray();

        // Tự động chấm ngay các câu trắc nghiệm & điền từ cho các bài nộp còn chưa chấm
        if (chosen != null && selected.Status != "cancelled")
        {
            var pendingAttemptIds = await db.UserExams.LatestSubmitted().Where(a => a.ExamId == chosen && Enumerable.Contains(ids, a.UserId)
                && a.SubmittedAt != null && a.Status == "submitted"
                && db.UserExamAnswers.Any(ans => ans.UserExamId == a.Id
                    && (ans.ScoreEarned == null || ans.GradingStatus != "auto_graded")
                    && db.Questions.Any(q => q.Id == ans.QuestionId && (q.QuestionType == "multiple_choice" || q.QuestionType == "fill_blank"))))
                .Select(a => a.Id).ToListAsync();

            foreach (var pId in pendingAttemptIds)
            {
                try { await autoGrading.GradeAsync(pId, classId: selected.Id); } catch { /* Bỏ qua nếu có lỗi validation */ }
            }
        }

        var attempts = await db.UserExams.Where(a => a.ExamId == chosen && Enumerable.Contains(ids, a.UserId))
            .OrderByDescending(a => a.AttemptNumber).ThenByDescending(a => a.Id).ToListAsync();
        var attemptIds = attempts.Select(a => a.Id).ToArray();
        var answers = await db.UserExamAnswers.AsNoTracking().Where(a => Enumerable.Contains(attemptIds, a.UserExamId)).ToListAsync();
        var questions = await (from eq in db.ExamQuestions join q in db.Questions on eq.QuestionId equals q.Id
            where eq.ExamId == chosen select new ScoringQuestion(q.Id, q.QuestionType, eq.Points)).ToListAsync();
        var passScore = await db.Exams.Where(e => e.Id == chosen).Select(e => (decimal?)e.PassScore).SingleOrDefaultAsync() ?? 0;
        return Ok(new { Class = new { selected.Id, selected.ClassCode, selected.ClassName, selected.Status }, Exams = exams, ExamId = chosen,
            HasAutomatic = questions.Any(q => q.Type is "multiple_choice" or "fill_blank"),
            HasEssay = questions.Any(q => q.Type == "essay"), Token = antiforgery.GetAndStoreTokens(HttpContext).RequestToken,
            Students = users.Select(u => {
                var a = attempts.FirstOrDefault(a => a.UserId == u.Id);
                var score = a == null ? null : ExamScoring.Summarize(questions, answers.Where(x => x.UserExamId == a.Id).ToList(), passScore);
                return new { u.Id, u.FullName, u.Email, AttemptId = a?.Id, a?.AttemptNumber, a?.SubmittedAt,
                    Status = a == null ? "not_submitted" : a.SubmittedAt == null ? "in_progress" : score!.Status };
            }) });
    }

    private async Task<bool> Allowed(int classId, int attemptId, int teacherId) => await (
        from a in db.UserExams join e in db.Exams on a.ExamId equals e.Id
        join c in db.CourseClasses on e.CourseId equals (int?)c.CourseId
        where a.Id == attemptId && a.SubmittedAt != null && e.CreatedByTeacherId == teacherId && c.Id == classId
            && db.ClassTeachers.Any(t => t.ClassId == c.Id && t.TeacherId == teacherId)
            && db.Enrollments.Any(en => en.ClassId == c.Id && en.UserId == a.UserId && en.Status != "cancelled")
        select a.Id).AnyAsync();

    [HttpGet("attempts/{id:int}")]
    public async Task<IActionResult> Attempt(int id, int classId)
    {
        var teacherId = await TeacherId();
        if (teacherId == null) return Forbid();
        if (!await Allowed(classId, id, teacherId.Value)) return NotFound();
        var isLatest = await db.UserExams.LatestSubmitted().AnyAsync(a => a.Id == id);
        var cancelled = await db.CourseClasses.AnyAsync(c => c.Id == classId && c.Status == "cancelled");
        var canEdit = isLatest && !cancelled;
        if (canEdit) { try { await autoGrading.GradeAsync(id, classId: classId); } catch { /* Keep viewing available on validation failure. */ } }
        isLatest = await db.UserExams.LatestSubmitted().AnyAsync(a => a.Id == id);
        cancelled = await db.CourseClasses.AnyAsync(c => c.Id == classId && c.Status == "cancelled");
        canEdit = isLatest && !cancelled;
        var readOnlyReason = cancelled ? "Lớp đã hủy — chỉ được xem kết quả." : !isLatest ? "Lượt nộp cũ — chỉ được xem. Chỉ lượt đã nộp mới nhất được chấm hoặc sửa điểm." : null;
        var attempt = await (from a in db.UserExams join u in db.Users on a.UserId equals u.Id join e in db.Exams on a.ExamId equals e.Id
            where a.Id == id select new { a.Id, a.AttemptNumber, a.SubmittedAt, u.FullName, e.Title }).SingleAsync();
        var rawAnswers = await (from a in db.UserExamAnswers join t in db.UserExams on a.UserExamId equals t.Id
            join q in db.Questions on a.QuestionId equals q.Id
            join eq in db.ExamQuestions on new { t.ExamId, a.QuestionId } equals new { eq.ExamId, eq.QuestionId }
            where t.Id == id orderby eq.OrderIndex
            select new { a.Id, QuestionId = q.Id, q.QuestionText, q.QuestionType, a.SelectedOptionId,
                SelectedOption = db.QuestionOptions.Where(o => o.Id == a.SelectedOptionId && o.QuestionId == q.Id).Select(o => o.OptionText).FirstOrDefault(),
                a.TextAnswer, a.ScoreEarned, a.Feedback, a.GradingStatus, a.GradedAt, eq.Points }).ToListAsync();
        var answers = rawAnswers.GroupBy(a => a.QuestionId).Select(g => g.First()).ToList();
        return Ok(new { Attempt = attempt, Answers = answers, CanEdit = canEdit, ReadOnlyReason = readOnlyReason, Token = antiforgery.GetAndStoreTokens(HttpContext).RequestToken });
    }

    [HttpGet("attempts/{id:int}/history")]
    public async Task<IActionResult> History(int id, int classId, long? beforeId, CancellationToken ct)
    {
        var teacherId = await TeacherId();
        if (teacherId == null) return Forbid();
        if (!await Allowed(classId, id, teacherId.Value)) return NotFound();
        var rows = await db.GradeChanges.AsNoTracking()
            .Where(h => h.UserExamId == id && (beforeId == null || h.Id < beforeId))
            .OrderByDescending(h => h.Id).Take(51).ToListAsync(ct);
        var hasMore = rows.Count > 50;
        var items = rows.Take(50).ToList();
        Response.Headers.CacheControl = "no-store";
        return Ok(new { Items = items, NextBeforeId = hasMore ? (long?)items[^1].Id : null });
    }

    public record GradeInput(int ClassId, List<AnswerGrade>? Answers, string? Reason = null);
    public record AnswerGrade(int Id, decimal? Score, string? Feedback, DateTime? ExpectedGradedAt);

    [HttpPost("attempts/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(int id, [FromBody] GradeInput input)
    {
        if (!ModelState.IsValid || input.Answers == null || input.Answers.Count == 0) return BadRequest(new { message = "Vui lòng nhập điểm cho các câu tự luận." });
        var teacherId = await TeacherId();
        if (teacherId == null) return Forbid();
        await using var transaction = await db.Database.BeginTransactionAsync();
        if (!await Allowed(input.ClassId, id, teacherId.Value)) return NotFound();
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT id FROM course_classes WHERE id = {input.ClassId} FOR UPDATE");
        if (await db.CourseClasses.AnyAsync(c => c.Id == input.ClassId && c.Status == "cancelled"))
            return Conflict(new { message = "Lớp đã hủy: chỉ được xem kết quả, không được chấm hoặc sửa điểm." });
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT id FROM user_exams WHERE id = {id} FOR UPDATE");
        if (!await db.UserExams.LatestSubmitted().AnyAsync(a => a.Id == id))
            return Conflict(new { message = "Đã có lượt nộp mới hơn. Lượt nộp cũ chỉ được xem, không được chấm hoặc sửa điểm." });
        var attempt = await db.UserExams.SingleAsync(a => a.Id == id);
        var answers = await db.UserExamAnswers.Where(a => a.UserExamId == id).ToListAsync();
        var essayPoints = await (from eq in db.ExamQuestions join q in db.Questions on eq.QuestionId equals q.Id
            where eq.ExamId == attempt.ExamId && q.QuestionType == "essay" select new { eq.QuestionId, eq.Points }).ToDictionaryAsync(q => q.QuestionId, q => q.Points);
        var essays = answers.Where(a => essayPoints.ContainsKey(a.QuestionId)).ToList();
        if (essays.Count == 0 || input.Answers.Count != essays.Count || input.Answers.Select(a => a.Id).Distinct().Count() != essays.Count
            || input.Answers.Any(a => !essays.Any(e => e.Id == a.Id)))
            return BadRequest(new { message = "Danh sách câu trả lời không hợp lệ. Vui lòng tải lại bài." });
        // Compare the version read by the form while holding the attempt row lock.
        // This also prevents a stale tab from overwriting another grading session.
        if (input.Answers.Any(g => essays.Single(a => a.Id == g.Id).GradedAt != g.ExpectedGradedAt))
            return Conflict(new { message = "Điểm đã được cập nhật ở phiên khác. Hãy mở lại bài để xem điểm mới trước khi sửa." });
        var changed = input.Answers.Where(g => {
            var a = essays.Single(a => a.Id == g.Id);
            return a.GradingStatus != "graded" || a.ScoreEarned != g.Score || (a.Feedback ?? "") != (g.Feedback?.Trim() ?? "");
        }).ToList();
        if (changed.Count == 0) return Ok(new { message = "Không có thay đổi điểm hoặc nhận xét.", attempt.TotalScore });
        var isRevision = changed.Any(g => essays.Single(a => a.Id == g.Id).GradingStatus == "graded");
        if (input.Reason?.Length > 1000 || (isRevision && string.IsNullOrWhiteSpace(input.Reason)))
            return BadRequest(new { message = "Vui lòng nhập lý do sửa điểm/nhận xét (tối đa 1.000 ký tự)." });
        var teacherName = await (from t in db.Teachers join u in db.Users on t.UserId equals u.Id
            where t.Id == teacherId select u.FullName).SingleAsync();
        var questionText = await (from eq in db.ExamQuestions join q in db.Questions on eq.QuestionId equals q.Id
            where eq.ExamId == attempt.ExamId && q.QuestionType == "essay" select new { q.Id, q.QuestionText }).ToDictionaryAsync(q => q.Id, q => q.QuestionText);
        var changedAt = DateTime.UtcNow;
        foreach (var grade in changed)
        {
            var answer = essays.Single(a => a.Id == grade.Id);
            if (grade.Score == null || grade.Score < 0 || grade.Score > essayPoints[answer.QuestionId] || decimal.Round(grade.Score.Value, 2) != grade.Score.Value || grade.Feedback?.Length > 4000)
                return BadRequest(new { message = "Điểm phải nằm trong thang điểm của câu, tối đa 2 chữ số thập phân; nhận xét không quá 4.000 ký tự." });
            db.GradeChanges.Add(new GradeChange {
                UserExamId = id, AnswerId = answer.Id, ClassId = input.ClassId,
                TeacherId = teacherId.Value, TeacherName = teacherName,
                QuestionText = questionText[answer.QuestionId], MaxScore = essayPoints[answer.QuestionId],
                OldScore = answer.ScoreEarned, NewScore = grade.Score.Value,
                OldFeedback = answer.Feedback, NewFeedback = grade.Feedback?.Trim(),
                IsRevision = answer.GradingStatus == "graded", Reason = input.Reason?.Trim(), ChangedAt = changedAt
            });
            answer.ScoreEarned = grade.Score;
            answer.Feedback = grade.Feedback?.Trim();
            answer.GradingStatus = "graded";
            answer.GradedByTeacherId = teacherId;
            answer.GradedAt = changedAt;
        }
        var questions = await (from eq in db.ExamQuestions join q in db.Questions on eq.QuestionId equals q.Id
            where eq.ExamId == attempt.ExamId select new ScoringQuestion(q.Id, q.QuestionType, eq.Points)).ToListAsync();
        var passScore = await db.Exams.Where(e => e.Id == attempt.ExamId).Select(e => e.PassScore).SingleAsync();
        var result = ExamScoring.Summarize(questions, answers, passScore);
        attempt.TotalScore = result.Score;
        attempt.Status = result.Score == null ? "submitted" : result.Status;
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return Ok(new { message = isRevision ? "Đã cập nhật điểm và nhận xét." : "Đã lưu điểm và nhận xét.", attempt.TotalScore });
    }
}
