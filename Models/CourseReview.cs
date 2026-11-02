using System;
using System.Collections.Generic;

namespace CNPM_EnglishLearning.Models;

public partial class CourseReview
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int CourseId { get; set; }

    public short Rating { get; set; }

    public string? Comment { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Course Course { get; set; } = null!;

    public virtual User1 User { get; set; } = null!;
}
