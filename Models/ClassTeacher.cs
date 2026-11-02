using System;
using System.Collections.Generic;

namespace CNPM_EnglishLearning.Models;

public partial class ClassTeacher
{
    public int Id { get; set; }

    public int ClassId { get; set; }

    public int TeacherId { get; set; }

    public string Role { get; set; } = null!;

    public DateTime? AssignedAt { get; set; }

    public virtual CourseClass Class { get; set; } = null!;

    public virtual Teacher Teacher { get; set; } = null!;
}
