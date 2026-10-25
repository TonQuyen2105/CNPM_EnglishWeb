using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CNPM_EnglishLearning.Areas.Student.Models
{
    [Table("exams")]
    public class Exam
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("course_id")]
        public int? CourseId { get; set; }

        [Column("created_by_teacher_id")]
        public int CreatedByTeacherId { get; set; }

        [Required]
        [Column("title")]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Column("description")]
        public string? Description { get; set; }

        [Column("duration_minutes")]
        public int DurationMinutes { get; set; } = 45;

        [Column("pass_score")]
        public decimal PassScore { get; set; } = 5.0m;

        [Column("max_attempts")]
        public int? MaxAttempts { get; set; }

        [Column("is_published")]
        public bool IsPublished { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}
