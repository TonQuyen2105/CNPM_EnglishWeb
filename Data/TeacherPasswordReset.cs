using System.Text;
using CNPM_EnglishLearning.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CNPM_EnglishLearning.Data;

internal static class TeacherPasswordReset
{
    public static async Task RunAsync(ApplicationDbContext db)
    {
        Console.Write("Email giảng viên (Enter để dùng teacher@englishlearning.example): ");
        var email = Console.ReadLine()?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email))
            email = ApplicationDataSeeder.Email;

        var user = await db.Users.SingleOrDefaultAsync(u => u.Email.ToLower() == email);
        if (user is null)
            throw new InvalidOperationException("Không tìm thấy email này trong database.");
        if (user.Role != "teacher" || !user.IsActive)
            throw new InvalidOperationException("Tài khoản không phải giảng viên đang hoạt động.");
        if (!await db.Teachers.AnyAsync(t => t.UserId == user.Id))
            throw new InvalidOperationException("Tài khoản chưa được liên kết với hồ sơ teachers.");

        var password = ReadPassword("Mật khẩu mới (ít nhất 12 ký tự): ");
        if (password.Length < 12)
            throw new InvalidOperationException("Mật khẩu phải có ít nhất 12 ký tự.");
        if (password != ReadPassword("Nhập lại mật khẩu mới: "))
            throw new InvalidOperationException("Hai mật khẩu không khớp.");

        user.Password = new PasswordHasher<User>().HashPassword(user, password);
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        await db.Entry(user).ReloadAsync();
        if (new PasswordHasher<User>().VerifyHashedPassword(user, user.Password, password)
            == PasswordVerificationResult.Failed)
            throw new InvalidOperationException("Không xác minh được mật khẩu vừa lưu.");

        Console.WriteLine($"Đã đặt lại và xác minh mật khẩu cho {email}.");
    }

    private static string ReadPassword(string prompt)
    {
        if (Console.IsInputRedirected)
            throw new InvalidOperationException("Chạy lệnh trực tiếp trong Terminal để nhập mật khẩu.");
        Console.Write(prompt);
        var value = new StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                return value.ToString();
            }
            if (key.Key == ConsoleKey.Backspace)
            {
                if (value.Length > 0) value.Length--;
            }
            else if (!char.IsControl(key.KeyChar))
            {
                value.Append(key.KeyChar);
            }
        }
    }
}
