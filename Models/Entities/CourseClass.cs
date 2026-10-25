using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CNPM_EnglishLearning.Models.Entities
{
    [Table("course_classes")]
    public class CourseClass
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("course_id")]
        public int CourseId { get; set; }

        [ForeignKey("CourseId")]
        public Course? Course { get; set; }

        [Required]
        [Column("class_code")]
        [MaxLength(50)]
        public string ClassCode { get; set; } = string.Empty;

        [Required]
        [Column("class_name")]
        [MaxLength(150)]
        public string ClassName { get; set; } = string.Empty;

        [Column("start_date")]
        public DateTime StartDate { get; set; }

        [Column("end_date")]
        public DateTime EndDate { get; set; }

        [Column("max_students")]
        public int MaxStudents { get; set; } = 20;

        [Column("price")]
        public decimal Price { get; set; } = 0;

        [Column("status")]
        [MaxLength(20)]
        public string Status { get; set; } = "upcoming"; // upcoming, ongoing, completed, cancelled

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}
