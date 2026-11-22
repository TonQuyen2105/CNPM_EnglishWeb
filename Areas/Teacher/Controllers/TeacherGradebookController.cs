using System.Security.Claims;
using CNPM_EnglishLearning.Data;
using CNPM_EnglishLearning.Services.Implementations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CNPM_EnglishLearning.Areas.Teacher.Controllers;

[Authorize(Roles = "teacher")]
[TypeFilter(typeof(TeacherGradingExceptionFilter))]
[Route("api/teacher/gradebook")]
public class TeacherGradebookController(ApplicationDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(int? classId, int? examId, CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Forbid();
        var teacherId = await (from t in db.Teachers join u in db.Users on t.UserId equals u.Id
            where u.Id == userId && u.IsActive && u.Role == "teacher" select (int?)t.Id).SingleOrDefaultAsync(cancellationToken);
        if (teacherId == null) return Forbid();
        var classes = db.CourseClasses.Where(c => db.ClassTeachers.Any(t => t.ClassId == c.Id && t.TeacherId == teacherId));
        if (classId == null) return Ok(await classes.OrderBy(c => c.ClassCode).Select(c => new {
            c.Id, c.ClassCode, c.ClassName, c.Status,
            Students = db.Enrollments.Count(e => e.ClassId == c.Id && e.Status != "cancelled")
        }).ToListAsync(cancellationToken));
        var selected = await classes.Where(c => c.Id == classId).Select(c => new { c.Id, c.CourseId, c.ClassCode, c.ClassName, c.Status }).SingleOrDefaultAsync(cancellationToken);
        if (selected == null) return NotFound();
        var exams = await db.Exams.Where(e => e.CourseId == selected.CourseId && e.CreatedByTeacherId == teacherId && e.IsPublished
            && db.ExamQuestions.Any(eq => eq.ExamId == e.Id && db.Questions.Any(q => q.Id == eq.QuestionId && (q.QuestionType == "essay" || q.QuestionType == "multiple_choice" || q.QuestionType == "fill_blank"))))
            .OrderBy(e => e.Title).Select(e => new { e.Id, e.Title, e.PassScore }).ToListAsync(cancellationToken);
        var chosen = examId ?? exams.FirstOrDefault()?.Id;
        var exam = exams.SingleOrDefault(e => e.Id == chosen);
        if (chosen != null && exam == null) return NotFound();
        var students = await db.Users.Where(u => db.Enrollments.Any(e => e.ClassId == selected.Id && e.UserId == u.Id && e.Status != "cancelled"))
            .OrderBy(u => u.FullName).Select(u => new { u.Id, u.FullName, u.Email }).ToListAsync(cancellationToken);
        var questions = await (from eq in db.ExamQuestions join q in db.Questions on eq.QuestionId equals q.Id
            where eq.ExamId == chosen select new { eq.QuestionId, eq.Points, q.QuestionType }).ToListAsync(cancellationToken);
        var essayQuestions = questions.Where(q => q.QuestionType == "essay").ToList();
        var maxScore = questions.Sum(q => q.Points);
        var essayOnly = questions.Count > 0 && questions.Count == essayQuestions.Count;
        var ids = students.Select(s => s.Id).ToArray();
        var attempts = await db.UserExams.Where(a => a.ExamId == chosen && Enumerable.Contains(ids, a.UserId))
            .OrderByDescending(a => a.AttemptNumber).ThenByDescending(a => a.Id)
            .Select(a => new { a.Id, a.UserId, a.AttemptNumber, a.SubmittedAt }).ToListAsync(cancellationToken);
        var attemptIds = attempts.Where(a => a.SubmittedAt != null).Select(a => a.Id).ToArray();
        var answers = await db.UserExamAnswers.Where(a => Enumerable.Contains(attemptIds, a.UserExamId))
            .AsNoTracking().ToListAsync(cancellationToken);
        var scoringQuestions = questions.Select(q => new ScoringQuestion(q.QuestionId, q.QuestionType, q.Points)).ToList();
        var rows = students.Select(student => {
            var history = attempts.Where(a => a.UserId == student.Id && a.SubmittedAt != null).Select(attempt => {
                var current = answers.Where(a => a.UserExamId == attempt.Id).ToList();
                var result = ExamScoring.Summarize(scoringQuestions, current, exam!.PassScore);
                return new { AttemptId = attempt.Id, attempt.AttemptNumber, attempt.SubmittedAt, result.Score, result.AutomaticScore, result.EssayScore, result.Status };
            }).ToList();
            var latest = history.FirstOrDefault();
            return new { student.Id, student.FullName, student.Email, Latest = latest, History = history,
                Status = latest?.Status ?? (attempts.Any(a => a.UserId == student.Id) ? "in_progress" : "not_submitted") };
        }).ToList();
        return Ok(new { Class = selected, Exams = exams, ExamId = chosen, MaxScore = maxScore, EssayOnly = essayOnly,
            AutomaticMaxScore = questions.Where(q => q.QuestionType == "multiple_choice" || q.QuestionType == "fill_blank").Sum(q => q.Points),
            EssayMaxScore = essayQuestions.Sum(q => q.Points), PassScore = exam?.PassScore, Students = rows });
    }
}
