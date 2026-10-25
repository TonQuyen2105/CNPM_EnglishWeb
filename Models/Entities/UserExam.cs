using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CNPM_EnglishLearning.Models.Entities
{
    [Table("user_exams")]
    public class UserExam
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("exam_id")]
        public int ExamId { get; set; }

        [Column("attempt_number")]
        public int AttemptNumber { get; set; } = 1;

        [Column("started_at")]
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;

        [Column("submitted_at")]
        public DateTime? SubmittedAt { get; set; }

        [Column("total_score")]
        public decimal? TotalScore { get; set; }

        [Column("status")]
        [MaxLength(20)]
        public string Status { get; set; } = "in_progress"; // in_progress, submitted, passed, failed
    }
}
