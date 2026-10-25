using System.ComponentModel.DataAnnotations;

namespace CNPM_EnglishLearning.Areas.Admin.Models
{
    public class CourseModel : BaseModel
    {
        public int SubjectId { get; set; }
        public SubjectModel? Subject { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên khóa học")]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string Level { get; set; } = "beginner"; // beginner, intermediate, advanced

        public decimal Price { get; set; } = 0;

        public string ThumbnailUrl { get; set; } = string.Empty;

        public int LessonCount { get; set; } = 0;

        public bool IsPublished { get; set; } = true;
    }
}
