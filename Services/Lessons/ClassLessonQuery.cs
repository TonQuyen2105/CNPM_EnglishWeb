using CNPM_EnglishLearning.Data;
using CNPM_EnglishLearning.Models.Entities;

namespace CNPM_EnglishLearning.Services.Lessons;

public sealed record AssignedLesson
{
    public required Lesson Lesson { get; init; }
    public int ClassId { get; init; }
    public int OrderIndex { get; init; }
}

public static class ClassLessonQuery
{
    public static IQueryable<AssignedLesson> LessonsByClass(this ApplicationDbContext db)
    {
        // A lesson can have several scheduled sessions in the same class.
        var assignments = from link in db.ClassLessons
                          group link by new { link.ClassId, link.LessonId } into sessions
                          select new { sessions.Key.ClassId, sessions.Key.LessonId,
                              OrderIndex = sessions.Min(s => s.OrderIndex) };
        return from link in assignments
               join lesson in db.Lessons on link.LessonId equals lesson.Id
               select new AssignedLesson { Lesson = lesson, ClassId = link.ClassId,
                   OrderIndex = link.OrderIndex ?? lesson.OrderIndex };
    }
}
