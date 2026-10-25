using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CNPM_EnglishLearning.Areas.Admin.Models
{
    [Table("class_lessons")]
    public class ClassLesson
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("class_id")]
        public int ClassId { get; set; }

        [Column("lesson_id")]
        public int LessonId { get; set; }

        [Column("schedule_id")]
        public int? ScheduleId { get; set; }

        [Column("teacher_id")]
        public int? TeacherId { get; set; }

        [Column("schedule_date")]
        public DateTime? ScheduleDate { get; set; }

        [Column("status")]
        [MaxLength(20)]
        public string Status { get; set; } = "scheduled";

        [Column("notes")]
        public string? Notes { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
