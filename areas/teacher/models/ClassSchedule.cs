using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CNPM_EnglishLearning.Areas.Teacher.Models
{
    [Table("class_schedules")]
    public class ClassSchedule
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("class_id")]
        public int ClassId { get; set; }

        [Column("classroom_id")]
        public int ClassroomId { get; set; }

        [Column("time_slot_id")]
        public int TimeSlotId { get; set; }

        [Column("day_of_week")]
        public int DayOfWeek { get; set; } // 1 - 7

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
