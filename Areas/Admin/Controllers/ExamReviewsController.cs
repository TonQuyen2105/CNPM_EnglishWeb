using CNPM_EnglishLearning.Data;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CNPM_EnglishLearning.Areas.Admin.Controllers;

[Authorize(Roles = "admin")]
[Route("api/admin/exam-reviews")]
public sealed class ExamReviewsController(ApplicationDbContext db, IAntiforgery antiforgery) : ControllerBase
{
    private Task<bool> ActiveAdmin(CancellationToken ct)
    {
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId);
        return db.Users.AnyAsync(u => u.Id == userId && u.Role == "admin" && u.IsActive, ct);
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        if (!await ActiveAdmin(ct)) return Forbid();
        var exams = await (from e in db.Exams
            join t in db.Teachers on e.CreatedByTeacherId equals t.Id
            join u in db.Users on t.UserId equals u.Id
            where e.ReviewStatus == "pending" && !e.IsPublished
            orderby e.UpdatedAt, e.Id
            select new { e.Id, e.Title, e.Description, e.DurationMinutes, e.PassScore, e.UpdatedAt,
                Teacher = u.FullName,
                Questions = (from eq in db.ExamQuestions join q in db.Questions on eq.QuestionId equals q.Id
                    where eq.ExamId == e.Id orderby eq.OrderIndex
                    select new { q.Id, q.QuestionText, eq.Points,
                        Options = db.QuestionOptions.Where(o => o.QuestionId == q.Id).OrderBy(o => o.Id)
                            .Select(o => new { o.OptionText, o.IsCorrect }).ToList(),
                        Answers = db.QuestionAnswers.Where(a => a.QuestionId == q.Id).OrderBy(a => a.Id)
                            .Select(a => a.AnswerText).ToList() }).ToList()
            }).AsSplitQuery().ToListAsync(ct);
        return Ok(new { exams, token = antiforgery.GetAndStoreTokens(HttpContext).RequestToken });
    }

    public sealed record Decision(bool? Approve, DateTime? ExpectedUpdatedAt);

    [HttpPost("{id:int}"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Review(int id, [FromBody] Decision decision, CancellationToken ct)
    {
        if (!await ActiveAdmin(ct)) return Forbid();
        if (!ModelState.IsValid || decision?.Approve == null || decision.ExpectedUpdatedAt == null) return BadRequest();
        var approve = decision.Approve.Value;
        var changed = await db.Exams.Where(e => e.Id == id && e.ReviewStatus == "pending"
                && !e.IsPublished && e.UpdatedAt == decision.ExpectedUpdatedAt)
            .ExecuteUpdateAsync(set => set.SetProperty(e => e.IsPublished, approve)
                .SetProperty(e => e.ReviewStatus, approve ? null : "rejected")
                .SetProperty(e => e.UpdatedAt, DateTime.UtcNow), ct);
        if (changed == 0) return Conflict(new { message = "Đề đã thay đổi hoặc đã được xử lý. Hãy tải lại danh sách." });
        return Ok(new { message = approve ? "Đã duyệt và công bố đề." : "Đã từ chối đề." });
    }
}
