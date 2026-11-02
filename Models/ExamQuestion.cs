using System;
using System.Collections.Generic;

namespace CNPM_EnglishLearning.Models;

public partial class ExamQuestion
{
    public int Id { get; set; }

    public int ExamId { get; set; }

    public int QuestionId { get; set; }

    public int Points { get; set; }

    public int OrderIndex { get; set; }

    public virtual Exam Exam { get; set; } = null!;

    public virtual Question Question { get; set; } = null!;
}
