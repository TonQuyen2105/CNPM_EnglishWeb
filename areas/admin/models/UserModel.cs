using System.ComponentModel.DataAnnotations;

namespace CNPM_EnglishLearning.Areas.Admin.Models
{
    public class UserModel : BaseModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ và tên")]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ email")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        public string Email { get; set; } = string.Empty;

        public string Password { get; set; } = "123456";

        public string AvatarUrl { get; set; } = string.Empty;

        public string Role { get; set; } = "student"; // admin, teacher, student

        public bool IsActive { get; set; } = true;
    }
}
