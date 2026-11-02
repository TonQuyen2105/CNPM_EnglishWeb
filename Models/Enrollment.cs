using System;
using System.Collections.Generic;

namespace CNPM_EnglishLearning.Models;

public partial class Enrollment
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int ClassId { get; set; }

    public DateTime? EnrolledAt { get; set; }

    public string Status { get; set; } = null!;

    public virtual CourseClass Class { get; set; } = null!;

    public virtual User1 User { get; set; } = null!;
}
