using System;
using System.Collections.Generic;

namespace CNPM_EnglishLearning.Models;

public partial class CourseClass
{
    public int Id { get; set; }

    public int CourseId { get; set; }

    public string ClassCode { get; set; } = null!;

    public string? ClassName { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public int? MaxStudents { get; set; }

    public decimal? Price { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<ClassLesson> ClassLessons { get; set; } = new List<ClassLesson>();

    public virtual ICollection<ClassSchedule> ClassSchedules { get; set; } = new List<ClassSchedule>();

    public virtual ICollection<ClassTeacher> ClassTeachers { get; set; } = new List<ClassTeacher>();

    public virtual Course Course { get; set; } = null!;

    public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}
