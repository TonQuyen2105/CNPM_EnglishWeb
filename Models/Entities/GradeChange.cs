using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CNPM_EnglishLearning.Models.Entities;

[Table("grade_changes")]
public class GradeChange
{
    [Key, Column("id")] public long Id { get; set; }
    [Column("user_exam_id")] public int UserExamId { get; set; }
    [Column("answer_id")] public int AnswerId { get; set; }
    [Column("class_id")] public int ClassId { get; set; }
    [Column("teacher_id")] public int TeacherId { get; set; }
    [Column("teacher_name")] public string TeacherName { get; set; } = "";
    [Column("question_text")] public string QuestionText { get; set; } = "";
    [Column("max_score")] public decimal MaxScore { get; set; }
    [Column("old_score")] public decimal? OldScore { get; set; }
    [Column("new_score")] public decimal NewScore { get; set; }
    [Column("old_feedback")] public string? OldFeedback { get; set; }
    [Column("new_feedback")] public string? NewFeedback { get; set; }
    [Column("is_revision")] public bool IsRevision { get; set; }
    [Column("reason")] public string? Reason { get; set; }
    [Column("changed_at")] public DateTime ChangedAt { get; set; }
}
