using CNPM_EnglishLearning.Data;
using CNPM_EnglishLearning.Models.Entities;
using CNPM_EnglishLearning.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CNPM_EnglishLearning.Services.Implementations;

public sealed class AutomaticGradingService(ApplicationDbContext db) : IAutomaticGradingService
{
    public async Task<AutomaticGradingResult> GradeAsync(int userExamId, CancellationToken cancellationToken = default)
    {
        // Same lock as manual grading: repeated/concurrent requests cannot double-count points.
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT id FROM user_exams WHERE id = {userExamId} FOR UPDATE", cancellationToken);
        var attempt = await db.UserExams.SingleOrDefaultAsync(a => a.Id == userExamId, cancellationToken)
            ?? throw new GradingValidationException("Không tìm thấy lượt làm bài.");
        if (attempt.SubmittedAt == null || attempt.Status == "in_progress")
            throw new GradingValidationException("Chỉ chấm bài đã nộp. Học viên vẫn đang làm bài này.");
        var exam = await db.Exams.SingleAsync(e => e.Id == attempt.ExamId, cancellationToken);
        var questions = await (from eq in db.ExamQuestions join q in db.Questions on eq.QuestionId equals q.Id
            where eq.ExamId == exam.Id select new ScoringQuestion(q.Id, q.QuestionType, eq.Points)).ToListAsync(cancellationToken);
        var answers = await db.UserExamAnswers.Where(a => a.UserExamId == userExamId).ToListAsync(cancellationToken);
        if (questions.Count == 0 || questions.Select(q => q.Id).Distinct().Count() != questions.Count
            || answers.Count != questions.Count || questions.Any(q => answers.Count(a => a.QuestionId == q.Id) != 1))
            throw new GradingValidationException("Bài nộp thiếu, trùng hoặc có câu trả lời ngoài đề. Cần lưu đúng một dòng cho mỗi câu, kể cả câu bỏ trống, trước khi chấm.");
        if (questions.Any(q => q.Type is not ("essay" or "multiple_choice" or "fill_blank")))
            throw new GradingValidationException("Đề có loại câu hỏi chưa hỗ trợ chấm. Hiện chỉ hỗ trợ tự luận, trắc nghiệm và điền từ.");
        var objective = questions.Where(q => q.Type != "essay").ToList();
        var pending = objective.Where(q => answers.Any(a => a.QuestionId == q.Id
            && (a.ScoreEarned == null || a.GradingStatus != "auto_graded"))).ToList();
        var ids = pending.Select(q => q.Id).ToArray();
        List<QuestionOption> options = pending.Count == 0 ? [] : await db.QuestionOptions.AsNoTracking().Where(o => Enumerable.Contains(ids, o.QuestionId)).ToListAsync(cancellationToken);
        List<QuestionAnswer> keys = pending.Count == 0 ? [] : await db.QuestionAnswers.AsNoTracking().Where(k => Enumerable.Contains(ids, k.QuestionId)).ToListAsync(cancellationToken);
        // Validate and calculate all answers before mutating tracked data.
        var scores = pending.Select(q => new { Question = q, Score = ExamScoring.GradeObjective(q,
            answers.Single(a => a.QuestionId == q.Id), options, keys) }).ToList();
        foreach (var item in scores)
        {
            var answer = answers.Single(a => a.QuestionId == item.Question.Id);
            answer.ScoreEarned = item.Score;
            answer.GradingStatus = "auto_graded";
            answer.GradedAt = DateTime.UtcNow;
            answer.GradedByTeacherId = null;
        }
        foreach (var essay in questions.Where(q => q.Type == "essay"))
        {
            var answer = answers.Single(a => a.QuestionId == essay.Id);
            if (answer.GradingStatus != "graded")
            {
                answer.ScoreEarned = null;
                answer.GradingStatus = "pending_review";
                answer.GradedAt = null;
                answer.GradedByTeacherId = null;
            }
        }
        var result = ExamScoring.Summarize(questions, answers, exam.PassScore);
        attempt.TotalScore = result.Score;
        attempt.Status = result.Score == null ? "submitted" : result.Status;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new(attempt.Id, scores.Count, attempt.TotalScore, result.Status);
    }
}
