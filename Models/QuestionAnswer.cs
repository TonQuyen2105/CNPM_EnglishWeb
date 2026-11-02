using System;
using System.Collections.Generic;

namespace CNPM_EnglishLearning.Models;

public partial class QuestionAnswer
{
    public int Id { get; set; }

    public int QuestionId { get; set; }

    public string? AnswerText { get; set; }

    public string? ReferenceAudioUrl { get; set; }

    public bool IsCaseSensitive { get; set; }

    public virtual Question Question { get; set; } = null!;
}
