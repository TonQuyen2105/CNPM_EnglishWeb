using System.ComponentModel.DataAnnotations;

namespace CNPM_EnglishLearning.Areas.Teacher.ViewModels;

public sealed class TeacherExamInput : IValidatableObject
{
    [Range(1, int.MaxValue)] public int CourseId { get; set; }
    [Required, StringLength(200)] public string Title { get; set; } = "";
    [StringLength(20000)] public string? Description { get; set; }
    [Range(1, 1440)] public int DurationMinutes { get; set; }
    public decimal PassScore { get; set; }
    [Range(1, 100)] public int? MaxAttempts { get; set; }
    public DateTime? ExpectedUpdatedAt { get; set; }
    public List<TeacherExamQuestionInput>? Questions { get; set; }
    public bool CanPublish() => Validator.TryValidateObject(this, new ValidationContext(this), new List<ValidationResult>(), true)
        && Questions!.Count > 0 && PassScore <= Questions.Sum(q => q.Points);

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (PassScore <= 0 || PassScore > 100000 || decimal.Round(PassScore, 2) != PassScore)
            yield return new("Điểm đạt phải lớn hơn 0, tối đa 100.000 và có tối đa 2 chữ số thập phân.");
        if (Questions == null || Questions.Count > 100)
            yield return new("Đề có tối đa 100 câu hỏi.");
        else if (Questions.Any(q => q == null || q.QuestionId <= 0 || q.Points <= 0 || q.Points > 1000 || decimal.Round(q.Points, 2) != q.Points)
            || Questions.Select(q => q.QuestionId).Distinct().Count() != Questions.Count)
            yield return new("Câu hỏi không được trùng; điểm mỗi câu từ 0,01 đến 1.000, tối đa 2 chữ số thập phân.");
    }
}
public record TeacherExamQuestionInput(int QuestionId, decimal Points, DateTime? ExpectedUpdatedAt);
public record TeacherExamVersion(DateTime? ExpectedUpdatedAt);
