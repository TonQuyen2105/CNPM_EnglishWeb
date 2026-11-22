using System;
using CNPM_EnglishLearning.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace CNPM_EnglishLearning.Data;

public partial class ApplicationDbContext : DbContext
{
    public ApplicationDbContext()
    {
    }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ClassLesson> ClassLessons { get; set; }

    public virtual DbSet<ClassSchedule> ClassSchedules { get; set; }

    public virtual DbSet<ClassTeacher> ClassTeachers { get; set; }

    public virtual DbSet<Classroom> Classrooms { get; set; }

    public virtual DbSet<Course> Courses { get; set; }

    public virtual DbSet<CourseClass> CourseClasses { get; set; }

    public virtual DbSet<CourseReview> CourseReviews { get; set; }

    public virtual DbSet<Enrollment> Enrollments { get; set; }

    public virtual DbSet<Exam> Exams { get; set; }

    public virtual DbSet<ExamQuestion> ExamQuestions { get; set; }

    public virtual DbSet<Flashcard> Flashcards { get; set; }

    public virtual DbSet<Lesson> Lessons { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Question> Questions { get; set; }

    public virtual DbSet<QuestionAnswer> QuestionAnswers { get; set; }

    public virtual DbSet<QuestionOption> QuestionOptions { get; set; }

    public virtual DbSet<Subject> Subjects { get; set; }

    public virtual DbSet<Teacher> Teachers { get; set; }

    public virtual DbSet<TimeSlot> TimeSlots { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserExam> UserExams { get; set; }

    public virtual DbSet<UserExamAnswer> UserExamAnswers { get; set; }

    public virtual DbSet<UserProgress> UserProgresses { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            // Connection string is supplied by the app configuration and dependency injection.
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Quy ước cột day_of_week trong PostgreSQL (class_schedules) dùng chung với bên Giáo viên & Học viên:
        // DB: 2 = Thứ 2, 3 = Thứ 3, 4 = Thứ 4, 5 = Thứ 5, 6 = Thứ 6, 7 = Thứ 7, 1 = Chủ Nhật (CN).
        // C# Admin nội bộ: 1 = Thứ 2 ... 6 = Thứ 7, 7 = Chủ Nhật (ISO).
        modelBuilder.Entity<ClassSchedule>()
            .Property(cs => cs.DayOfWeek)
            .HasConversion(
                isoDay => isoDay == 7 ? 1 : (isoDay >= 1 && isoDay <= 6 ? isoDay + 1 : isoDay),
                dbDay => dbDay == 1 ? 7 : (dbDay >= 2 && dbDay <= 7 ? dbDay - 1 : dbDay));
    }

    public decimal CalculateEstimatedRevenue()
    {
        if (Payments.AsNoTracking().Any())
        {
            return Payments.AsNoTracking()
                .Where(p => p.Status == "completed")
                .Sum(p => (decimal?)p.Amount) ?? 0m;
        }

        return (
            from e in Enrollments.AsNoTracking()
            join cc in CourseClasses.AsNoTracking() on e.ClassId equals cc.Id
            where (e.Status == "active" || e.Status == "completed") && cc.Status != "cancelled"
            select (decimal?)cc.Price
        ).Sum() ?? 0m;
    }
}
