using System;
using System.Collections.Generic;

namespace CNPM_EnglishLearning.Models;

public partial class Question
{
    public int Id { get; set; }

    public int? LessonId { get; set; }

    public string QuestionText { get; set; } = null!;

    public string? AudioUrl { get; set; }

    public string QuestionType { get; set; } = null!;

    public int DefaultPoints { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<ExamQuestion> ExamQuestions { get; set; } = new List<ExamQuestion>();

    public virtual Lesson? Lesson { get; set; }

    public virtual ICollection<QuestionAnswer> QuestionAnswers { get; set; } = new List<QuestionAnswer>();

    public virtual ICollection<QuestionOption> QuestionOptions { get; set; } = new List<QuestionOption>();

    public virtual ICollection<UserExamAnswer> UserExamAnswers { get; set; } = new List<UserExamAnswer>();
}
