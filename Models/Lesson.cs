using System;
using System.Collections.Generic;

namespace CNPM_EnglishLearning.Models;

public partial class Lesson
{
    public int Id { get; set; }

    public int CourseId { get; set; }

    public string Title { get; set; } = null!;

    public int OrderIndex { get; set; }

    public string? ContentType { get; set; }

    public string? Content { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<ClassLesson> ClassLessons { get; set; } = new List<ClassLesson>();

    public virtual Course Course { get; set; } = null!;

    public virtual ICollection<Question> Questions { get; set; } = new List<Question>();

    public virtual ICollection<UserProgress> UserProgresses { get; set; } = new List<UserProgress>();
}
