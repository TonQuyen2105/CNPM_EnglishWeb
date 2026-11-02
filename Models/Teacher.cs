using System;
using System.Collections.Generic;

namespace CNPM_EnglishLearning.Models;

public partial class Teacher
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string? Bio { get; set; }

    public string? Qualification { get; set; }

    public bool IsVerified { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<ClassLesson> ClassLessons { get; set; } = new List<ClassLesson>();

    public virtual ICollection<ClassTeacher> ClassTeachers { get; set; } = new List<ClassTeacher>();

    public virtual ICollection<Exam> Exams { get; set; } = new List<Exam>();

    public virtual User1 User { get; set; } = null!;

    public virtual ICollection<UserExamAnswer> UserExamAnswers { get; set; } = new List<UserExamAnswer>();
}
