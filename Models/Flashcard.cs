using System;
using System.Collections.Generic;

namespace CNPM_EnglishLearning.Models;

public partial class Flashcard
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string Word { get; set; } = null!;

    public string Meaning { get; set; } = null!;

    public string? Phonetic { get; set; }

    public string? AudioUrl { get; set; }

    public string? ExampleSentence { get; set; }

    public DateTime? NextReviewAt { get; set; }

    public int? IntervalDays { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User1 User { get; set; } = null!;
}
