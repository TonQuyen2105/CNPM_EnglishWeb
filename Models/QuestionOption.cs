using System;
using System.Collections.Generic;

namespace CNPM_EnglishLearning.Models;

public partial class QuestionOption
{
    public int Id { get; set; }

    public int QuestionId { get; set; }

    public string OptionText { get; set; } = null!;

    public bool IsCorrect { get; set; }

    public virtual Question Question { get; set; } = null!;

    public virtual ICollection<UserExamAnswer> UserExamAnswers { get; set; } = new List<UserExamAnswer>();
}
