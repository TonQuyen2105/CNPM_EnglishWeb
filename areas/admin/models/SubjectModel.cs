using System.ComponentModel.DataAnnotations;

namespace CNPM_EnglishLearning.Areas.Admin.Models
{
    public class SubjectModel : BaseModel
    {
        [Required(ErrorMessage = "Vui lòng nhập mã môn học")]
        [MaxLength(20)]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập tên môn học")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public int CourseCount { get; set; } = 0;
    }
}
