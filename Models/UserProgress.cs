using System;
using System.Collections.Generic;

namespace CNPM_EnglishLearning.Models;

public partial class UserProgress
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int LessonId { get; set; }

    public int? Score { get; set; }

    public bool IsCompleted { get; set; }

    public DateTime? CompletedAt { get; set; }

    public virtual Lesson Lesson { get; set; } = null!;

    public virtual User1 User { get; set; } = null!;
}
