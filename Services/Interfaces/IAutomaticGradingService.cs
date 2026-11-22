namespace CNPM_EnglishLearning.Services.Interfaces;

public record AutomaticGradingResult(int AttemptId, int GradedQuestions, decimal? TotalScore, string Status);

public interface IAutomaticGradingService
{
    Task<AutomaticGradingResult> GradeAsync(int userExamId, CancellationToken cancellationToken = default, int? classId = null);
}
