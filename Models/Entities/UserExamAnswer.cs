using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CNPM_EnglishLearning.Models.Entities
{
    [Table("user_exam_answers")]
    public class UserExamAnswer
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("user_exam_id")]
        public int UserExamId { get; set; }

        [Column("question_id")]
        public int QuestionId { get; set; }

        [Column("selected_option_id")]
        public int? SelectedOptionId { get; set; }

        [Column("text_answer")]
        public string? TextAnswer { get; set; }

        [Column("score_earned")]
        public decimal? ScoreEarned { get; set; }

        [Column("grading_status")]
        [MaxLength(20)]
        public string GradingStatus { get; set; } = "auto_graded"; // auto_graded, pending_review, graded

        [Column("graded_by_teacher_id")]
        public int? GradedByTeacherId { get; set; }

        [Column("graded_at")]
        public DateTime? GradedAt { get; set; }

        [Column("feedback")]
        public string? Feedback { get; set; }
    }
}
