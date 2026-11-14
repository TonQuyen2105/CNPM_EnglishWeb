using System.Security.Claims;
using CNPM_EnglishLearning.Data;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CNPM_EnglishLearning.Areas.Teacher.Controllers;

[Authorize(Roles = "teacher")]
[TypeFilter(typeof(TeacherGradingExceptionFilter))]
[Route("api/teacher/grading-workspace")]
public class TeacherGradingController(ApplicationDbContext db, IAntiforgery antiforgery) : ControllerBase
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
            && db.ExamQuestions.Any(eq => eq.ExamId == e.Id && db.Questions.Any(q => q.Id == eq.QuestionId && q.QuestionType == "essay")))
            .OrderBy(e => e.Title).Select(e => new { e.Id, e.Title }).ToListAsync();
        var chosen = examId ?? exams.FirstOrDefault()?.Id;
        if (chosen != null && !exams.Any(e => e.Id == chosen)) return NotFound();
        var users = await db.Users.Where(u => db.Enrollments.Any(e => e.ClassId == classId && e.UserId == u.Id && e.Status != "cancelled"))
            .OrderBy(u => u.FullName).Select(u => new { u.Id, u.FullName, u.Email }).ToListAsync();
        // Explicit LINQ overload avoids the span-based array overload in expression trees.
        var ids = users.Select(u => u.Id).ToArray();
        var attempts = await db.UserExams.Where(a => a.ExamId == chosen && Enumerable.Contains(ids, a.UserId))
            .OrderByDescending(a => a.AttemptNumber).ThenByDescending(a => a.Id).ToListAsync();
        var attemptIds = attempts.Select(a => a.Id).ToArray();
        var pending = await db.UserExamAnswers.Where(a => Enumerable.Contains(attemptIds, a.UserExamId) && a.GradingStatus == "pending_review")
            .Select(a => a.UserExamId).Distinct().ToListAsync();
        return Ok(new { Class = new { selected.Id, selected.ClassCode, selected.ClassName }, Exams = exams, ExamId = chosen,
            Students = users.Select(u => {
                var a = attempts.FirstOrDefault(a => a.UserId == u.Id);
                return new { u.Id, u.FullName, u.Email, AttemptId = a?.Id, a?.AttemptNumber, a?.SubmittedAt,
                    Status = a == null ? "not_submitted" : a.SubmittedAt == null ? "in_progress" : pending.Contains(a.Id) ? "pending_review" : "graded" };
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
        var attempt = await (from a in db.UserExams join u in db.Users on a.UserId equals u.Id join e in db.Exams on a.ExamId equals e.Id
            where a.Id == id select new { a.Id, a.AttemptNumber, a.SubmittedAt, u.FullName, e.Title }).SingleAsync();
        var answers = await (from a in db.UserExamAnswers join t in db.UserExams on a.UserExamId equals t.Id
            join q in db.Questions on a.QuestionId equals q.Id
            join eq in db.ExamQuestions on new { t.ExamId, a.QuestionId } equals new { eq.ExamId, eq.QuestionId }
            where t.Id == id && q.QuestionType == "essay" orderby eq.OrderIndex
            select new { a.Id, q.QuestionText, a.TextAnswer, a.ScoreEarned, a.Feedback, a.GradingStatus, a.GradedAt, eq.Points }).ToListAsync();
        return Ok(new { Attempt = attempt, Answers = answers, Token = antiforgery.GetAndStoreTokens(HttpContext).RequestToken });
    }

    public record GradeInput(int ClassId, List<AnswerGrade>? Answers);
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
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT id FROM user_exams WHERE id = {id} FOR UPDATE");
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
        var isRevision = essays.Any(a => a.GradingStatus == "graded");
        foreach (var grade in input.Answers)
        {
            var answer = essays.Single(a => a.Id == grade.Id);
            if (grade.Score == null || grade.Score < 0 || grade.Score > essayPoints[answer.QuestionId] || decimal.Round(grade.Score.Value, 2) != grade.Score.Value || grade.Feedback?.Length > 4000)
                return BadRequest(new { message = "Điểm phải nằm trong thang điểm của câu, tối đa 2 chữ số thập phân; nhận xét không quá 4.000 ký tự." });
            answer.ScoreEarned = grade.Score;
            answer.Feedback = grade.Feedback?.Trim();
            answer.GradingStatus = "graded";
            answer.GradedByTeacherId = teacherId;
            answer.GradedAt = DateTime.UtcNow;
        }
        var expected = await db.ExamQuestions.CountAsync(q => q.ExamId == attempt.ExamId);
        if (answers.Select(a => a.QuestionId).Distinct().Count() == expected && answers.All(a => a.ScoreEarned != null && a.GradingStatus != "pending_review"))
        {
            attempt.TotalScore = answers.Sum(a => a.ScoreEarned ?? 0);
            var passScore = await db.Exams.Where(e => e.Id == attempt.ExamId).Select(e => e.PassScore).SingleAsync();
            attempt.Status = attempt.TotalScore >= passScore ? "passed" : "failed";
        }
        else
        {
            attempt.TotalScore = null;
            attempt.Status = "submitted";
        }
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return Ok(new { message = isRevision ? "Đã cập nhật điểm và nhận xét." : "Đã lưu điểm và nhận xét.", attempt.TotalScore });
    }
}
