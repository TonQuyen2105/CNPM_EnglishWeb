using System;
using System.Collections.Generic;

namespace CNPM_EnglishLearning.Models;

public partial class UserExam
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int ExamId { get; set; }

    public int AttemptNumber { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public int? TotalScore { get; set; }

    public string Status { get; set; } = null!;

    public virtual Exam Exam { get; set; } = null!;

    public virtual User1 User { get; set; } = null!;

    public virtual ICollection<UserExamAnswer> UserExamAnswers { get; set; } = new List<UserExamAnswer>();
}
