using System.Security.Claims;
using CNPM_EnglishLearning.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CNPM_EnglishLearning.Areas.Student.Controllers;

[Authorize(Roles = "student")]
[Route("api/student/published-exams")]
public class PublishedExamsController(ApplicationDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            || !await db.Users.AnyAsync(u => u.Id == userId && u.IsActive && u.Role == "student", ct)) return Forbid();
        var courses = from e in db.Enrollments join c in db.CourseClasses on e.ClassId equals c.Id
                      where e.UserId == userId && (e.Status == "active" || e.Status == "completed") && c.Status != "cancelled"
                      select c.CourseId;
        return Ok(await db.Exams.Where(e => e.IsPublished && e.CourseId.HasValue && courses.Contains(e.CourseId.Value))
            .OrderByDescending(e => e.UpdatedAt).ThenByDescending(e => e.Id)
            .Select(e => new { e.Title, e.DurationMinutes, e.Description, e.PassScore }).ToListAsync(ct));
    }
}
