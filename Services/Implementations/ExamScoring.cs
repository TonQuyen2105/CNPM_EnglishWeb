using System.Text;
using CNPM_EnglishLearning.Models.Entities;

namespace CNPM_EnglishLearning.Services.Implementations;

public sealed class GradingValidationException(string message) : Exception(message);

public record ScoringQuestion(int Id, string Type, decimal Points);
public record ExamScore(decimal? Score, decimal? AutomaticScore, decimal? EssayScore, string Status);

public static class ExamScoring
{
    public static string NormalizeText(string text) => text.Trim().Normalize(NormalizationForm.FormC);

    public static decimal GradeObjective(ScoringQuestion question, UserExamAnswer answer,
        IReadOnlyCollection<QuestionOption> options, IReadOnlyCollection<QuestionAnswer> keys)
    {
        if (question.Points <= 0) throw new GradingValidationException($"Câu #{question.Id} có thang điểm không hợp lệ.");
        if (question.Type == "multiple_choice")
        {
            var choices = options.Where(o => o.QuestionId == question.Id).ToList();
            if (choices.Count < 2 || choices.Count(o => o.IsCorrect) != 1)
                throw new GradingValidationException($"Câu #{question.Id} cần ít nhất hai lựa chọn và đúng một đáp án đúng.");
            if (answer.SelectedOptionId == null) return 0;
            var selected = choices.SingleOrDefault(o => o.Id == answer.SelectedOptionId)
                ?? throw new GradingValidationException($"Lựa chọn của câu #{question.Id} không thuộc câu hỏi. Cần kiểm tra dữ liệu bài nộp.");
            return selected.IsCorrect ? question.Points : 0;
        }
        if (question.Type == "fill_blank")
        {
            var accepted = keys.Where(k => k.QuestionId == question.Id).ToList();
            if (accepted.Count == 0 || accepted.Any(k => string.IsNullOrWhiteSpace(k.AnswerText)))
                throw new GradingValidationException($"Câu #{question.Id} chưa có bộ đáp án điền từ hợp lệ.");
            var text = NormalizeText(answer.TextAnswer ?? "");
            if (text.Length == 0) return 0;
            return accepted.Any(k => string.Equals(text, NormalizeText(k.AnswerText),
                k.IsCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase)) ? question.Points : 0;
        }
        throw new GradingValidationException("Loại câu hỏi không hỗ trợ chấm tự động.");
    }

    public static ExamScore Summarize(IReadOnlyCollection<ScoringQuestion> questions,
        IReadOnlyCollection<UserExamAnswer> answers, decimal passScore)
    {
        // Nhóm theo QuestionId để xử lý an toàn nếu trong DB có bản ghi trùng lặp
        var uniqueAnswers = answers
            .GroupBy(a => a.QuestionId)
            .Select(g => g.OrderByDescending(a => a.GradingStatus == "graded")
                          .ThenByDescending(a => a.ScoreEarned.HasValue)
                          .ThenByDescending(a => a.Id)
                          .First())
            .ToList();

        bool Complete(ScoringQuestion q)
        {
            var match = uniqueAnswers.FirstOrDefault(a => a.QuestionId == q.Id);
            return match != null && q.Points > 0 && match.ScoreEarned is decimal score && score >= 0 && score <= q.Points
                && (q.Type == "essay" ? match.GradingStatus == "graded"
                    : (q.Type is "multiple_choice" or "fill_blank") && match.GradingStatus == "auto_graded");
        }
        var automatic = questions.Where(q => q.Type is "multiple_choice" or "fill_blank").ToList();
        var essays = questions.Where(q => q.Type == "essay").ToList();
        decimal? Sum(IReadOnlyCollection<ScoringQuestion> part) => part.All(Complete)
            ? uniqueAnswers.Where(a => part.Any(q => q.Id == a.QuestionId)).Sum(a => a.ScoreEarned ?? 0) : null;
        var complete = questions.Count > 0 && questions.Select(q => q.Id).Distinct().Count() == questions.Count
            && questions.All(Complete);
        decimal? total = complete ? uniqueAnswers.Where(a => questions.Any(q => q.Id == a.QuestionId)).Sum(a => a.ScoreEarned ?? 0) : null;
        var status = complete ? total >= passScore ? "passed" : "failed"
            : essays.Any(q => !Complete(q)) ? "pending_review"
            : "graded";
        return new(total, Sum(automatic), Sum(essays), status);
    }
}
