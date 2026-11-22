using CNPM_EnglishLearning.Data;
using CNPM_EnglishLearning.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CNPM_EnglishLearning.Services.Implementations;

public sealed class AutomaticGradingWorker(IServiceScopeFactory scopes, ILogger<AutomaticGradingWorker> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try { await GradePendingAsync(ct); }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                log.LogWarning(ex, "Could not check submitted exams for automatic grading.");
            }
            await Task.Delay(TimeSpan.FromSeconds(5), ct);
        }
    }

    private async Task GradePendingAsync(CancellationToken ct)
    {
        const int batchSize = 50;
        var afterId = 0;
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        while (!ct.IsCancellationRequested)
        {
            var ids = await db.UserExams.Where(t => t.Id > afterId && t.SubmittedAt != null && t.Status == "submitted"
                && db.UserExamAnswers.Any(a => a.UserExamId == t.Id
                    && (a.ScoreEarned == null || a.GradingStatus != "auto_graded")
                    && db.Questions.Any(q => q.Id == a.QuestionId && (q.QuestionType == "multiple_choice" || q.QuestionType == "fill_blank"))))
                .OrderBy(t => t.Id).Select(t => t.Id).Take(batchSize).ToListAsync(ct);
            foreach (var id in ids)
            {
                using var attemptScope = scopes.CreateScope();
                try { await attemptScope.ServiceProvider.GetRequiredService<IAutomaticGradingService>().GradeAsync(id, ct); }
                catch (GradingValidationException ex) { log.LogWarning("Cannot grade attempt {Id}: {Reason}", id, ex.Message); }
            }
            if (ids.Count < batchSize) return;
            afterId = ids[^1];
        }
    }
}
