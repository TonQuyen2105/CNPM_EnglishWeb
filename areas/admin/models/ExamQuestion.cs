using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CNPM_EnglishLearning.Areas.Admin.Models
{
    [Table("exam_questions")]
    public class ExamQuestion
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("exam_id")]
        public int ExamId { get; set; }

        [Column("question_id")]
        public int QuestionId { get; set; }

        [Column("points")]
        public decimal Points { get; set; } = 1.0m;

        [Column("order_index")]
        public int OrderIndex { get; set; }
    }
}
