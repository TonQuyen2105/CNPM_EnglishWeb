using CNPM_EnglishLearning.Areas.Student.DTOs;
using CNPM_EnglishLearning.Areas.Student.DTOs.Common;
using CNPM_EnglishLearning.Areas.Student.Models;
using CNPM_EnglishLearning.Areas.Student.Services.Interfaces;
using CNPM_EnglishLearning.Data;
using Microsoft.EntityFrameworkCore;

namespace CNPM_EnglishLearning.Areas.Student.Services.Implementations
{
    /// <summary>
    /// Nhóm 9: Nghiệp vụ Quản lý Hồ sơ Cá nhân Học sinh (Xem & Cập nhật thông tin).
    /// </summary>
    public class StudentProfileService : BaseStudentService, IStudentProfileService
    {
        public StudentProfileService(ApplicationDbContext db) : base(db)
        {
        }

        /// <summary>
        /// 9.1. Xem thông tin hồ sơ học sinh.
        /// </summary>
        public async Task<ServiceResult<StudentProfileDto>> GetProfileAsync(int studentId)
        {
            var user = await Db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == studentId);

            if (user == null)
            {
                return ServiceResult<StudentProfileDto>.NotFound($"Không tìm thấy thông tin học sinh có ID = {studentId}.");
            }

            var ext = StudentExtendedStore.GetOrCreateUserProfileExtension(studentId);

            var dto = new StudentProfileDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                AvatarUrl = user.AvatarUrl,
                PhoneNumber = user.PhoneNumber ?? ext.PhoneNumber,
                LearningGoal = ext.LearningGoal
            };

            return ServiceResult<StudentProfileDto>.Ok(dto, "Lấy thông tin hồ sơ học sinh thành công.");
        }

        /// <summary>
        /// 9.2. Cập nhật thông tin hồ sơ cá nhân.
        /// </summary>
        public async Task<ServiceResult<StudentProfileDto>> UpdateProfileAsync(
            int studentId,
            UpdateStudentProfileRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.FullName) || string.IsNullOrWhiteSpace(request.Email))
            {
                return ServiceResult<StudentProfileDto>.BadRequest("Họ tên và Email không được để trống.");
            }

            var user = await Db.Users.FirstOrDefaultAsync(u => u.Id == studentId);
            if (user == null)
            {
                return ServiceResult<StudentProfileDto>.NotFound($"Không tìm thấy thông tin học sinh có ID = {studentId}.");
            }

            var normalizedEmail = request.Email.Trim();

            // Kiểm tra trùng lặp Email nếu học sinh thay đổi Email
            if (!string.Equals(user.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase))
            {
                var emailTaken = await Db.Users.AsNoTracking()
                    .AnyAsync(u => u.Id != studentId && u.Email.ToLower() == normalizedEmail.ToLower());

                if (emailTaken)
                {
                    return ServiceResult<StudentProfileDto>.Conflict($"Email '{normalizedEmail}' đã được sử dụng bởi tài khoản khác.");
                }
            }

            user.FullName = request.FullName.Trim();
            user.Email = normalizedEmail;
            if (request.PhoneNumber != null)
            {
                user.PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim();
            }
            if (request.AvatarUrl != null)
            {
                user.AvatarUrl = string.IsNullOrWhiteSpace(request.AvatarUrl) ? null : request.AvatarUrl.Trim();
            }
            user.UpdatedAt = DateTime.UtcNow;

            await Db.SaveChangesAsync();

            var ext = StudentExtendedStore.UpdateUserProfileExtension(
                studentId,
                user.PhoneNumber,
                request.LearningGoal);

            var dto = new StudentProfileDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                AvatarUrl = user.AvatarUrl,
                PhoneNumber = user.PhoneNumber ?? ext.PhoneNumber,
                LearningGoal = ext.LearningGoal
            };

            return ServiceResult<StudentProfileDto>.Ok(dto, "Cập nhật thông tin hồ sơ cá nhân thành công.");
        }
    }
}
