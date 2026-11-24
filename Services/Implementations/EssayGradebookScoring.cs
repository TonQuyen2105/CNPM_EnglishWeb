namespace CNPM_EnglishLearning.Services.Implementations;

public record EssayGradebookAnswer(int QuestionId, decimal? Score, string Status);
public record EssayGradebookResult(decimal? Score, string Status);

// Read-only projection: never infer zero for missing or ungraded answers.
public static class EssayGradebookScoring
{
    public static EssayGradebookResult Calculate(IReadOnlyDictionary<int, decimal> points,
        IReadOnlyCollection<EssayGradebookAnswer> answers, decimal? passScore)
    {
        var complete = points.Count > 0 && answers.Count == points.Count && points.All(q =>
            answers.Count(a => a.QuestionId == q.Key && a.Status == "graded" && a.Score != null
                && a.Score >= 0 && a.Score <= q.Value) == 1);
        if (!complete) return new(null, "pending_review");
        var score = answers.Sum(a => a.Score!.Value);
        // A mixed exam's pass threshold applies to the whole exam, not its essay subtotal.
        return new(score, passScore == null ? "graded" : score >= passScore ? "passed" : "failed");
    }
}
