using System;
using System.Collections.Generic;

namespace CNPM_EnglishLearning.Models;

public partial class ClassSchedule
{
    public int Id { get; set; }

    public int ClassId { get; set; }

    public int ClassroomId { get; set; }

    public int TimeSlotId { get; set; }

    public short DayOfWeek { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual CourseClass Class { get; set; } = null!;

    public virtual ICollection<ClassLesson> ClassLessons { get; set; } = new List<ClassLesson>();

    public virtual Classroom Classroom { get; set; } = null!;

    public virtual TimeSlot TimeSlot { get; set; } = null!;
}
