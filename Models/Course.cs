using System;
using System.Collections.Generic;

namespace CNPM_EnglishLearning.Models;

public partial class Course
{
    public int Id { get; set; }

    public int SubjectId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public string? ThumbnailUrl { get; set; }

    public string? Level { get; set; }

    public decimal Price { get; set; }

    public bool IsPublished { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<CourseClass> CourseClasses { get; set; } = new List<CourseClass>();

    public virtual ICollection<CourseReview> CourseReviews { get; set; } = new List<CourseReview>();

    public virtual ICollection<Exam> Exams { get; set; } = new List<Exam>();

    public virtual ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();

    public virtual Subject Subject { get; set; } = null!;
}
