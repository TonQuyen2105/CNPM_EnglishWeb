using CNPM_EnglishLearning.Models.Entities;

namespace CNPM_EnglishLearning.Services.Implementations;

public static class GradingAttemptPolicy
{
    // Same ordering as the gradebook. An unfinished retake does not replace a submitted attempt.
    public static IQueryable<UserExam> LatestSubmitted(this IQueryable<UserExam> attempts) =>
        attempts.Where(a => a.SubmittedAt != null && !attempts.Any(newer =>
            newer.UserId == a.UserId && newer.ExamId == a.ExamId && newer.SubmittedAt != null
            && (newer.AttemptNumber > a.AttemptNumber || (newer.AttemptNumber == a.AttemptNumber && newer.Id > a.Id))));
}
