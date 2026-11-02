using System;
using System.Collections.Generic;

namespace CNPM_EnglishLearning.Models;

public partial class Classroom
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Location { get; set; }

    public int? Capacity { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<ClassSchedule> ClassSchedules { get; set; } = new List<ClassSchedule>();
}
