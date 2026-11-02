using System;
using System.Collections.Generic;

namespace CNPM_EnglishLearning.Models;

public partial class User1
{
    public int Id { get; set; }

    public string Email { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public string? AvatarUrl { get; set; }

    public string Role { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<CourseReview> CourseReviews { get; set; } = new List<CourseReview>();

    public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();

    public virtual ICollection<Flashcard> Flashcards { get; set; } = new List<Flashcard>();

    public virtual Teacher? Teacher { get; set; }

    public virtual ICollection<UserExam> UserExams { get; set; } = new List<UserExam>();

    public virtual ICollection<UserProgress> UserProgresses { get; set; } = new List<UserProgress>();
}
