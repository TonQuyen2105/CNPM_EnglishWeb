using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CNPM_EnglishLearning.Areas.Teacher.Models
{
    [Table("questions")]
    public class Question
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("lesson_id")]
        public int? LessonId { get; set; }

        [Required]
        [Column("question_text")]
        public string QuestionText { get; set; } = string.Empty;

        [Column("audio_url")]
        public string? AudioUrl { get; set; }

        [Column("question_type")]
        [MaxLength(30)]
        public string QuestionType { get; set; } = "multiple_choice";

        [Column("default_points")]
        public decimal DefaultPoints { get; set; } = 1.0m;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}
