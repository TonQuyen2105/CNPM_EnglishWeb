using System;
using System.Collections.Generic;

namespace CNPM_EnglishLearning.Models;

public partial class Exam
{
    public int Id { get; set; }

    public int? CourseId { get; set; }

    public int CreatedByTeacherId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public int DurationMinutes { get; set; }

    public int PassScore { get; set; }

    public int? MaxAttempts { get; set; }

    public bool IsPublished { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Course? Course { get; set; }

    public virtual Teacher CreatedByTeacher { get; set; } = null!;

    public virtual ICollection<ExamQuestion> ExamQuestions { get; set; } = new List<ExamQuestion>();

    public virtual ICollection<UserExam> UserExams { get; set; } = new List<UserExam>();
}
