using System.ComponentModel.DataAnnotations;

namespace CNPM_EnglishLearning.Areas.Admin.Models
{
    public class TeacherModel : BaseModel
    {
        public int UserId { get; set; }
        public UserModel? User { get; set; }

        public string Bio { get; set; } = string.Empty;

        [MaxLength(100)]
        public string HighestDegree { get; set; } = string.Empty;

        [MaxLength(200)]
        public string Certifications { get; set; } = string.Empty;

        public bool IsVerified { get; set; } = false;

        public int YearsExperience { get; set; } = 0;
    }
}
