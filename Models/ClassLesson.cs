using System;
using System.Collections.Generic;

namespace CNPM_EnglishLearning.Models;

public partial class ClassLesson
{
    public int Id { get; set; }

    public int ClassId { get; set; }

    public int LessonId { get; set; }

    public int? ScheduleId { get; set; }

    public int? TeacherId { get; set; }

    public DateOnly ScheduleDate { get; set; }

    public string Status { get; set; } = null!;

    public string? Notes { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual CourseClass Class { get; set; } = null!;

    public virtual Lesson Lesson { get; set; } = null!;

    public virtual ClassSchedule? Schedule { get; set; }

    public virtual Teacher? Teacher { get; set; }
}
