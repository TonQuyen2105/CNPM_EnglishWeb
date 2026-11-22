using System.Security.Claims;
using CNPM_EnglishLearning.Data;
using CNPM_EnglishLearning.Models.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CNPM_EnglishLearning.Controllers;

[Route("Account/TeacherSession")]
public class TeacherSessionController(ApplicationDbContext db) : ControllerBase
{
    public record Credentials(string Email, string Password, bool Remember);

    [HttpPost]
    [HttpPost("/Account/Session")]
    public async Task<IActionResult> Login([FromBody] Credentials? credentials, CancellationToken cancellationToken)
    {
        if (credentials is null || string.IsNullOrWhiteSpace(credentials.Email)
            || string.IsNullOrEmpty(credentials.Password))
            return BadRequest();

        var email = credentials.Email.Trim().ToLowerInvariant();
        var user = await db.Users.SingleOrDefaultAsync(u => u.Email.ToLower() == email
            && u.IsActive, cancellationToken);

        if (user is null || !MatchesPassword(user, credentials.Password))
            return Unauthorized();

        if (user.Role is not ("admin" or "teacher" or "student"))
            return Forbid();
        if (user.Role == "teacher" && !await db.Teachers.AnyAsync(t => t.UserId == user.Id, cancellationToken))
            return Forbid();

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Role, user.Role)
        };
        var scheme = CookieAuthenticationDefaults.AuthenticationScheme;
        await HttpContext.SignInAsync(scheme, new ClaimsPrincipal(new ClaimsIdentity(claims, scheme)),
            new AuthenticationProperties { IsPersistent = credentials.Remember });

        return Ok(new { user.Id, user.Email, user.FullName, user.Role, user.PhoneNumber, user.AvatarUrl });
    }

    [HttpDelete]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    private static bool MatchesPassword(User user, string password)
    {
        // Existing accounts may still use the original password format.
        if (!user.Password.StartsWith("AQAAAA", StringComparison.Ordinal))
            return user.Password == password;

        try
        {
            return new PasswordHasher<User>().VerifyHashedPassword(user, user.Password, password)
                != PasswordVerificationResult.Failed;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
