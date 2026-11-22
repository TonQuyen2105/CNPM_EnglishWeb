using CNPM_EnglishLearning.Services.Lessons;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using CNPM_EnglishLearning.Data;
using CNPM_EnglishLearning.Models.Entities;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CNPM_EnglishLearning.Areas.Teacher.Controllers;

[Authorize(Roles = "teacher")]
[Route("api/teacher/lessons-workspace")]
public class TeacherLessonsController(ApplicationDbContext db, IAntiforgery antiforgery, SupabaseLessonStorage files) : ControllerBase
{
    private IQueryable<CourseClass> AssignedClasses()
    {
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId);
        return db.CourseClasses.Where(cl => db.ClassTeachers.Any(ct => ct.ClassId == cl.Id
            && db.Teachers.Any(t => t.Id == ct.TeacherId && t.UserId == userId
                && db.Users.Any(u => u.Id == userId && u.IsActive && u.Role == "teacher"))));
    }

    private IQueryable<Lesson> AvailableLessons(int classId, int courseId)
    {
        var assigned = AssignedClasses();
        return db.Lessons.Where(l => l.CourseId == courseId
            && !db.ClassLessons.Any(a => a.LessonId == l.Id && a.ClassId == classId)
            && (!db.ClassLessons.Any(a => a.LessonId == l.Id)
                || db.ClassLessons.Any(a => a.LessonId == l.Id && assigned.Any(c => c.Id == a.ClassId))));
    }

    [HttpGet]
    public async Task<IActionResult> Get(int? classId, CancellationToken cancellationToken)
    {
        var classes = AssignedClasses();
        if (classId == null) return Ok(await classes.OrderBy(c => c.ClassName).Select(c => new {
            c.Id, c.ClassName, c.ClassCode, c.Status,
            Course = db.Courses.Where(course => course.Id == c.CourseId).Select(course => course.Title).FirstOrDefault(),
            Lessons = db.ClassLessons.Where(a => a.ClassId == c.Id).Select(a => a.LessonId).Distinct().Count()
        }).ToListAsync(cancellationToken));
        var classroom = await classes.Where(c => c.Id == classId).Select(c => new { c.Id, c.CourseId, c.ClassName, c.ClassCode }).SingleOrDefaultAsync(cancellationToken);
        if (classroom == null) return NotFound();
        var lessons = await db.LessonsByClass().Where(l => l.ClassId == classId).OrderBy(l => l.OrderIndex).ThenBy(l => l.Lesson.Id)
            .Select(l => new { l.Lesson.Id, l.Lesson.Title, l.OrderIndex, l.Lesson.ContentType, l.Lesson.UpdatedAt, l.Lesson.CreatedAt }).ToListAsync(cancellationToken);
        var available = await AvailableLessons(classroom.Id, classroom.CourseId)
            .OrderBy(l => l.OrderIndex).Select(l => new { l.Id, l.Title }).ToListAsync(cancellationToken);
        return Ok(new { Class = classroom, Lessons = lessons, Available = available, Token = antiforgery.GetAndStoreTokens(HttpContext).RequestToken });
    }

    [HttpPost("classes/{classId:int}/assign/{id:int}"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(int classId, int id, CancellationToken ct)
    {
        if (!await AssignedClasses().AnyAsync(c => c.Id == classId, ct)) return NotFound();
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT id FROM course_classes WHERE id = {classId} FOR UPDATE", ct);
        var classroom = await AssignedClasses().SingleOrDefaultAsync(c => c.Id == classId, ct);
        if (classroom == null) return NotFound();
        var order = (await db.LessonsByClass().Where(l => l.ClassId == classId).MaxAsync(l => (int?)l.OrderIndex, ct) ?? 0) + 1;
        if (await db.ClassLessons.AnyAsync(a => a.ClassId == classId && a.LessonId == id, ct))
            return Ok(new { id, message = "Bài giảng đã có trong lớp." });
        if (!await AvailableLessons(classId, classroom.CourseId).AnyAsync(l => l.Id == id, ct)) return NotFound();
        if (order > 100000) return Conflict(new { message = "Lớp đã đạt giới hạn thứ tự bài giảng." });
        db.ClassLessons.Add(new ClassLesson { LessonId = id, ClassId = classId, OrderIndex = order });
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Ok(new { id, message = "Đã gán bài giảng cho lớp." });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Detail(int id, int? classId, CancellationToken cancellationToken)
    {
        var classes = AssignedClasses();
        var lesson = await db.LessonsByClass().Where(a => a.Lesson.Id == id && classes.Any(c => c.Id == a.ClassId)
                && (classId == null || a.ClassId == classId)).OrderBy(a => a.ClassId)
            .Select(a => new { a.Lesson.Id, a.ClassId, a.Lesson.CourseId, a.Lesson.Title, a.OrderIndex,
                a.Lesson.ContentType, a.Lesson.Content, a.Lesson.CreatedAt, a.Lesson.UpdatedAt }).FirstOrDefaultAsync(cancellationToken);
        if (lesson == null) return NotFound();
        var document = LessonDocument.Read(lesson.Content);
        return Ok(new { Lesson = new { lesson.Id, lesson.ClassId, lesson.CourseId, lesson.Title, lesson.OrderIndex, lesson.ContentType,
            Content = document.Text, lesson.CreatedAt, lesson.UpdatedAt,
            Video = LessonDocument.View(id, document.Video, "video"), Pdf = LessonDocument.View(id, document.Pdf, "pdf") },
            Token = antiforgery.GetAndStoreTokens(HttpContext).RequestToken });
    }

    public sealed class LessonInput
    {
        [Range(1, int.MaxValue)] public int ClassId { get; set; }
        [Required, StringLength(200)] public string Title { get; set; } = "";
        [Range(1, 100000)] public int OrderIndex { get; set; }
        [Required, RegularExpression("^(vocabulary|grammar|listening|reading|speaking|writing|quiz)$")]
        public string ContentType { get; set; } = "";
        [StringLength(100000)] public string? Content { get; set; }
        public bool RemoveVideo { get; set; }
        public bool RemovePdf { get; set; }
        public DateTime? ExpectedUpdatedAt { get; set; }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Create([FromBody] LessonInput input, CancellationToken cancellationToken) => Save(null, input, cancellationToken);

    [HttpPut("{id:int}")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Update(int id, [FromBody] LessonInput input, CancellationToken cancellationToken) => Save(id, input, cancellationToken);

    [HttpPost("with-media"), ValidateAntiForgeryToken]
    [RequestSizeLimit(232000000), RequestFormLimits(MultipartBodyLengthLimit = 232000000)]
    public Task<IActionResult> CreateWithMedia([FromForm] LessonInput input, IFormFile? video, IFormFile? pdf, CancellationToken ct)
        => SaveWithMedia(null, input, video, pdf, ct);

    [HttpPut("{id:int}/with-media"), ValidateAntiForgeryToken]
    [RequestSizeLimit(232000000), RequestFormLimits(MultipartBodyLengthLimit = 232000000)]
    public Task<IActionResult> UpdateWithMedia(int id, [FromForm] LessonInput input, IFormFile? video, IFormFile? pdf, CancellationToken ct)
        => SaveWithMedia(id, input, video, pdf, ct);

    private async Task<IActionResult> SaveWithMedia(int? id, LessonInput input, IFormFile? video, IFormFile? pdf, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(new { message = "Kiểm tra thông tin bài giảng trước khi lưu." });
        if (!await AssignedClasses().AnyAsync(c => c.Id == input.ClassId, ct)) return NotFound();
        LessonAttachment? uploadedVideo = null, uploadedPdf = null;
        bool saved = false;
        try
        {
            if (video != null) uploadedVideo = await files.SaveAsync(video, "video", ct);
            if (pdf != null) uploadedPdf = await files.SaveAsync(pdf, "pdf", ct);
            var result = await Save(id, input, ct, uploadedVideo, uploadedPdf);
            saved = result is OkObjectResult;
            return result;
        }
        catch (InvalidDataException ex) { return BadRequest(new { message = ex.Message }); }
        finally
        {
            if (!saved)
            {
                if (uploadedVideo != null) await files.DeleteAsync(uploadedVideo);
                if (uploadedPdf != null) await files.DeleteAsync(uploadedPdf);
            }
        }
    }

    private async Task<IActionResult> Save(int? id, LessonInput input, CancellationToken ct, LessonAttachment? video = null, LessonAttachment? pdf = null)
    {
        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(input.Title))
            return BadRequest(new { message = "Kiểm tra tiêu đề (1–200 ký tự), thứ tự (1–100.000), loại bài và nội dung (tối đa 100.000 ký tự)." });
        if (!await AssignedClasses().AnyAsync(c => c.Id == input.ClassId, ct)) return NotFound();
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        // Serialize ordering checks within this class; recheck assignment after acquiring the lock.
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT id FROM course_classes WHERE id = {input.ClassId} FOR UPDATE", ct);
        var classroom = await AssignedClasses().SingleOrDefaultAsync(c => c.Id == input.ClassId, ct);
        if (classroom == null) return NotFound();
        Lesson? lesson = null;
        if (id != null)
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT id FROM lessons WHERE id = {id} FOR UPDATE", ct);
            lesson = await db.Lessons.SingleOrDefaultAsync(l => l.Id == id
                && db.ClassLessons.Any(a => a.LessonId == l.Id && a.ClassId == input.ClassId), ct);
            if (lesson == null) return NotFound();
            if (lesson.UpdatedAt != input.ExpectedUpdatedAt)
                return Conflict(new { message = "Bài giảng đã được sửa ở phiên khác. Hãy mở lại bài để xem nội dung mới trước khi lưu." });
        }
        if (await db.LessonsByClass().AnyAsync(l => l.ClassId == input.ClassId && l.OrderIndex == input.OrderIndex && (id == null || l.Lesson.Id != id), ct))
            return Conflict(new { message = "Thứ tự này đã được dùng trong lớp. Vui lòng chọn số khác." });
        if (lesson == null)
        {
            lesson = new Lesson { CourseId = classroom.CourseId, OrderIndex = input.OrderIndex };
            db.Lessons.Add(lesson);
        }
        lesson.Title = input.Title.Trim();
        lesson.ContentType = input.ContentType;
        var document = LessonDocument.Read(lesson.Content);
        lesson.Content = (document with
        {
            Text = input.Content?.Trim(),
            Video = video ?? (input.RemoveVideo ? null : document.Video),
            Pdf = pdf ?? (input.RemovePdf ? null : document.Pdf)
        }).Serialize();
        lesson.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        if (id == null)
        {
            db.ClassLessons.Add(new ClassLesson { ClassId = classroom.Id, LessonId = lesson.Id, OrderIndex = input.OrderIndex });
            await db.SaveChangesAsync(ct);
        }
        else
        {
            await db.ClassLessons.Where(a => a.ClassId == classroom.Id && a.LessonId == lesson.Id)
                .ExecuteUpdateAsync(set => set.SetProperty(a => a.OrderIndex, input.OrderIndex), ct);
        }
        await transaction.CommitAsync(ct);
        return Ok(new { lesson.Id, message = id == null ? "Đã thêm bài giảng." : "Đã lưu thay đổi bài giảng." });
    }
    [HttpPost("{id:int}/media/{kind}"), ValidateAntiForgeryToken]
    [RequestSizeLimit(211000000)]
    [RequestFormLimits(MultipartBodyLengthLimit = 211000000)]
    public async Task<IActionResult> Upload(int id, string kind, IFormFile? file, [FromForm] DateTime? expectedUpdatedAt, CancellationToken ct)
    {
        if (kind is not ("video" or "pdf") || file == null || !ModelState.IsValid) return BadRequest(new { message = "Chọn file video hoặc PDF hợp lệ." });
        var classes = AssignedClasses();
        var accessible = db.Lessons.Where(l => l.Id == id && db.ClassLessons.Any(a =>
            a.LessonId == l.Id && classes.Any(c => c.Id == a.ClassId)));
        if (!await accessible.AnyAsync(ct)) return NotFound();
        LessonAttachment? uploaded = null; bool committed = false;
        try
        {
            uploaded = await files.SaveAsync(file, kind, ct);
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT id FROM lessons WHERE id = {id} FOR UPDATE", ct);
            var lesson = await accessible.SingleOrDefaultAsync(ct);
            if (lesson == null) return NotFound();
            if (lesson.UpdatedAt != expectedUpdatedAt) return Conflict(new { message = "Bài giảng vừa thay đổi. Tải lại bài rồi chọn file để tải lên lại." });
            var document = LessonDocument.Read(lesson.Content);
            document = kind == "video" ? document with { Video = uploaded } : document with { Pdf = uploaded };
            lesson.Content = document.Serialize(); lesson.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct); committed = true;
            return Ok(new { message = kind == "video" ? "Đã tải video lên bài giảng." : "Đã tải tài liệu PDF lên bài giảng." });
        }
        catch (InvalidDataException ex) { return BadRequest(new { message = ex.Message }); }
        finally { if (!committed && uploaded != null) await files.DeleteAsync(uploaded); }
    }

}
