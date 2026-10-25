using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CNPM_EnglishLearning.Models.Entities
{
    [Table("user_progress")]
    public class UserProgress
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("lesson_id")]
        public int LessonId { get; set; }

        [Column("score")]
        public decimal? Score { get; set; }

        [Column("is_completed")]
        public bool IsCompleted { get; set; } = false;

        [Column("completed_at")]
        public DateTime? CompletedAt { get; set; }
    }
}
