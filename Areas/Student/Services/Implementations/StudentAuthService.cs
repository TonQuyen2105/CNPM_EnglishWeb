using CNPM_EnglishLearning.Areas.Student.DTOs;
using CNPM_EnglishLearning.Areas.Student.DTOs.Common;
using CNPM_EnglishLearning.Areas.Student.Models;
using CNPM_EnglishLearning.Areas.Student.Services.Interfaces;
using CNPM_EnglishLearning.Data;
using CNPM_EnglishLearning.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace CNPM_EnglishLearning.Areas.Student.Services.Implementations
{
    /// <summary>
    /// Nghiệp vụ Xác thực & Đăng nhập cho Phân hệ Học sinh (OOP Service).
    /// Đảm bảo tuân thủ nghiêm ngặt quy tắc nghiệp vụ:
    /// 1. Đúng email & password
    /// 2. Role của tài khoản phải là "student"
    /// 3. Tài khoản phải đang hoạt động (is_active = TRUE)
    /// </summary>
    public class StudentAuthService : BaseStudentService, IStudentAuthService
    {
        private static readonly Microsoft.AspNetCore.Identity.PasswordHasher<User> PasswordHasher = new();

        public StudentAuthService(ApplicationDbContext db) : base(db)
        {
        }

        private static bool VerifyPassword(User user, string inputPassword)
        {
            if (string.IsNullOrEmpty(user.Password) || string.IsNullOrEmpty(inputPassword))
            {
                return false;
            }

            // 1. So khớp trực tiếp plaintext (ví dụ password trong DB lưu dạng chuỗi chưa mã hóa)
            if (string.Equals(user.Password, inputPassword, StringComparison.Ordinal))
            {
                return true;
            }

            // 2. Kiểm tra bằng PasswordHasher của ASP.NET Core Identity nếu mật khẩu được hash
            try
            {
                var result = PasswordHasher.VerifyHashedPassword(user, user.Password, inputPassword);
                if (result == Microsoft.AspNetCore.Identity.PasswordVerificationResult.Success ||
                    result == Microsoft.AspNetCore.Identity.PasswordVerificationResult.SuccessRehashNeeded)
                {
                    return true;
                }
            }
            catch
            {
                // Bỏ qua ngoại lệ format hash không hợp lệ
            }

            return false;
        }

        public async Task<ServiceResult<StudentLoginResponseDto>> LoginAsync(StudentLoginRequestDto request)
        {
            if (request == null)
            {
                return ServiceResult<StudentLoginResponseDto>.BadRequest("Dữ liệu đăng nhập không hợp lệ.");
            }

            var email = request.Email?.Trim() ?? string.Empty;
            var password = request.Password ?? string.Empty;

            if (string.IsNullOrWhiteSpace(email))
            {
                return ServiceResult<StudentLoginResponseDto>.BadRequest("Vui lòng nhập địa chỉ email.");
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                return ServiceResult<StudentLoginResponseDto>.BadRequest("Vui lòng nhập mật khẩu.");
            }

            // Tối ưu hóa truy vấn: Tìm người dùng theo email (case-insensitive) trên PostgreSQL
            var normalizedEmail = email.ToLower();
            var user = await Db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

            // 1. Kiểm tra tài khoản tồn tại và khớp mật khẩu (Hỗ trợ cả Hash ASP.NET Identity & Plaintext)
            if (user == null || !VerifyPassword(user, password))
            {
                return ServiceResult<StudentLoginResponseDto>.Unauthorized("Email hoặc mật khẩu không chính xác.");
            }

            // 2. Kiểm tra trạng thái kích hoạt của tài khoản (is_active = TRUE)
            if (!user.IsActive)
            {
                return ServiceResult<StudentLoginResponseDto>.Forbidden("Tài khoản học sinh của bạn hiện đang bị khóa hoặc chưa được kích hoạt. Vui lòng liên hệ ban quản trị trung tâm để được hỗ trợ.");
            }

            // 3. Kiểm tra vai trò tài khoản: Chỉ cho phép tài khoản có role là "student"
            if (!string.Equals(user.Role, "student", StringComparison.OrdinalIgnoreCase))
            {
                return ServiceResult<StudentLoginResponseDto>.Forbidden($"Cổng đăng nhập này chỉ dành riêng cho Học sinh. Tài khoản của bạn mang vai trò '{user.Role}' không có quyền truy cập vào phân hệ này.");
            }

            // Đọc phone_number từ entity
            var phoneNumber = user.PhoneNumber ?? string.Empty;

            var userDto = new StudentLoginUserDto
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Role = user.Role,
                PhoneNumber = phoneNumber,
                AvatarUrl = user.AvatarUrl,
                IsActive = user.IsActive
            };

            var responseDto = new StudentLoginResponseDto
            {
                User = userDto,
                Token = $"student_token_{user.Id}_{Guid.NewGuid():N}",
                RedirectUrl = "Areas/Student/Views/dashboard.html"
            };

            return ServiceResult<StudentLoginResponseDto>.Ok(
                responseDto,
                $"Đăng nhập thành công! Xin chào học viên {user.FullName}.");
        }

        public async Task<ServiceResult<StudentRegisterResponseDto>> RegisterAsync(StudentRegisterRequestDto request)
        {
            if (request == null)
            {
                return ServiceResult<StudentRegisterResponseDto>.BadRequest("Dữ liệu đăng ký không hợp lệ.");
            }

            var fullName = request.FullName?.Trim() ?? string.Empty;
            var email = request.Email?.Trim() ?? string.Empty;
            var password = request.Password ?? string.Empty;
            var confirmPassword = request.ConfirmPassword ?? string.Empty;
            var phoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim();

            if (string.IsNullOrWhiteSpace(fullName))
            {
                return ServiceResult<StudentRegisterResponseDto>.BadRequest("Vui lòng nhập họ và tên của bạn.");
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                return ServiceResult<StudentRegisterResponseDto>.BadRequest("Vui lòng nhập địa chỉ email.");
            }

            if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email))
            {
                return ServiceResult<StudentRegisterResponseDto>.BadRequest("Định dạng email không hợp lệ.");
            }

            if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            {
                return ServiceResult<StudentRegisterResponseDto>.BadRequest("Mật khẩu phải có độ dài tối thiểu từ 6 ký tự trở lên.");
            }

            if (!string.Equals(password, confirmPassword, StringComparison.Ordinal))
            {
                return ServiceResult<StudentRegisterResponseDto>.BadRequest("Mật khẩu xác nhận không trùng khớp.");
            }

            // 1. Kiểm tra email đã tồn tại trong hệ thống chưa (case-insensitive)
            var normalizedEmail = email.ToLower();
            var existingUser = await Db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

            if (existingUser != null)
            {
                return ServiceResult<StudentRegisterResponseDto>.Conflict("Địa chỉ email này đã được sử dụng để đăng ký tài khoản trong hệ thống. Vui lòng chọn email khác hoặc đăng nhập.");
            }

            // 2. Sinh ID an toàn cho User mới
            var nextUserId = await GetNextIdAsync(Db.Users, u => u.Id);

            // 3. Khởi tạo User mới với vai trò mặc định "student", is_active = TRUE
            var newUser = new User
            {
                Id = nextUserId,
                Email = email,
                FullName = fullName,
                PhoneNumber = phoneNumber,
                Role = "student",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Mã hóa mật khẩu bảo mật chuẩn ASP.NET Core Identity PasswordHasher
            newUser.Password = PasswordHasher.HashPassword(newUser, password);

            Db.Users.Add(newUser);
            await Db.SaveChangesAsync();

            var userDto = new StudentLoginUserDto
            {
                Id = newUser.Id,
                Email = newUser.Email,
                FullName = newUser.FullName,
                Role = newUser.Role,
                PhoneNumber = newUser.PhoneNumber,
                AvatarUrl = newUser.AvatarUrl,
                IsActive = newUser.IsActive
            };

            var responseDto = new StudentRegisterResponseDto
            {
                User = userDto,
                Token = $"student_token_{newUser.Id}_{Guid.NewGuid():N}",
                RedirectUrl = "Areas/Student/Views/dashboard.html"
            };

            return ServiceResult<StudentRegisterResponseDto>.Created(
                responseDto,
                $"Đăng ký tài khoản học viên thành công! Chào mừng {newUser.FullName} gia nhập hệ thống.");
        }
    }
}
