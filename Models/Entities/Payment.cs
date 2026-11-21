using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CNPM_EnglishLearning.Models.Entities
{
    [Table("payments")]
    public class Payment
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("class_id")]
        public int ClassId { get; set; }

        [Column("enrollment_id")]
        public int? EnrollmentId { get; set; }

        [Required]
        [Column("transaction_code")]
        public string TransactionCode { get; set; } = string.Empty;

        [Column("amount")]
        public decimal Amount { get; set; }

        [Required]
        [Column("payment_method")]
        public string PaymentMethod { get; set; } = "bank_transfer"; // bank_transfer, vnpay, momo, cash

        [Required]
        [Column("status")]
        public string Status { get; set; } = "pending"; // pending, completed, failed, refunded

        [Column("paid_at")]
        public DateTime? PaidAt { get; set; }

        [Column("note")]
        public string? Note { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
