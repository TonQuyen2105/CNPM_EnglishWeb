using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CNPM_EnglishLearning.Areas.Admin.Models
{
    [Table("class_teachers")]
    public class ClassTeacher
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("class_id")]
        public int ClassId { get; set; }

        [Column("teacher_id")]
        public int TeacherId { get; set; }

        [Column("role")]
        [MaxLength(20)]
        public string Role { get; set; } = "main";

        [Column("assigned_at")]
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    }
}
