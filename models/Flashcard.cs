using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CNPM_EnglishLearning.Models
{
    [Table("flashcards")]
    public class Flashcard
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Required]
        [Column("word")]
        [MaxLength(100)]
        public string Word { get; set; } = string.Empty;

        [Required]
        [Column("meaning")]
        public string Meaning { get; set; } = string.Empty;

        [Column("phonetic")]
        [MaxLength(100)]
        public string? Phonetic { get; set; }

        [Column("audio_url")]
        public string? AudioUrl { get; set; }

        [Column("example_sentence")]
        public string? ExampleSentence { get; set; }

        [Column("next_review_at")]
        public DateTime? NextReviewAt { get; set; }

        [Column("interval_days")]
        public int IntervalDays { get; set; } = 1;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
