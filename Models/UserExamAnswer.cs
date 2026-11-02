using System;
using System.Collections.Generic;

namespace CNPM_EnglishLearning.Models;

public partial class UserExamAnswer
{
    public int Id { get; set; }

    public int UserExamId { get; set; }

    public int QuestionId { get; set; }

    public int? SelectedOptionId { get; set; }

    public string? TextAnswer { get; set; }

    public int? ScoreEarned { get; set; }

    public string GradingStatus { get; set; } = null!;

    public int? GradedByTeacherId { get; set; }

    public DateTime? GradedAt { get; set; }

    public string? Feedback { get; set; }

    public virtual Teacher? GradedByTeacher { get; set; }

    public virtual Question Question { get; set; } = null!;

    public virtual QuestionOption? SelectedOption { get; set; }

    public virtual UserExam UserExam { get; set; } = null!;
}
