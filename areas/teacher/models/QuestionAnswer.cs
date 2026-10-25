using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CNPM_EnglishLearning.Areas.Teacher.Models
{
    [Table("question_answers")]
    public class QuestionAnswer
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("question_id")]
        public int QuestionId { get; set; }

        [Required]
        [Column("answer_text")]
        public string AnswerText { get; set; } = string.Empty;

        [Column("reference_audio_url")]
        public string? ReferenceAudioUrl { get; set; }

        [Column("is_case_sensitive")]
        public bool IsCaseSensitive { get; set; } = false;
    }
}
