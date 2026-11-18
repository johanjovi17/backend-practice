using BackendPractice.Models;
using Microsoft.EntityFrameworkCore;

namespace BackendPractice.Data;

public sealed class CourseDbContext(DbContextOptions<CourseDbContext> options) : DbContext(options)
{
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<ProcessedMessage> ProcessedMessages => Set<ProcessedMessage>();
    public DbSet<DeadLetterMessage> DeadLetterMessages => Set<DeadLetterMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Course>(entity =>
        {
            entity.Property(course => course.Status).HasConversion<string>();
            entity.Property(course => course.CreatedAt).HasColumnType("datetime2");
            entity.Property(course => course.UpdatedAt).HasColumnType("datetime2");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(user => user.Email).IsUnique();
            entity.Property(user => user.Name).HasMaxLength(100).IsRequired();
            entity.Property(user => user.Email).HasMaxLength(256).IsRequired();
            entity.Property(user => user.PasswordHash).HasMaxLength(512).IsRequired();
            entity.Property(user => user.Role).HasConversion<string>();
            entity.Property(user => user.CreatedAt).HasColumnType("datetime2");
            entity.Property(user => user.UpdatedAt).HasColumnType("datetime2");
        });

        modelBuilder.Entity<Enrollment>(entity =>
        {
            entity.HasIndex(enrollment => new { enrollment.StudentId, enrollment.CourseId }).IsUnique();
            entity.Property(enrollment => enrollment.Status).HasConversion<string>().HasMaxLength(50);
            entity.Property(enrollment => enrollment.EnrolledAt).HasColumnType("datetime2");
        });

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.HasIndex(message => new { message.Status, message.CreatedAt });
            entity.Property(message => message.EventType).HasMaxLength(100).IsRequired();
            entity.Property(message => message.Payload).IsRequired();
            entity.Property(message => message.Status).HasConversion<string>().HasMaxLength(50);
            entity.Property(message => message.CreatedAt).HasColumnType("datetime2");
            entity.Property(message => message.PublishedAt).HasColumnType("datetime2");
        });

        modelBuilder.Entity<ProcessedMessage>(entity =>
        {
            entity.HasKey(message => message.MessageId);
            entity.Property(message => message.Status).HasConversion<string>().HasMaxLength(50);
            entity.Property(message => message.ProcessedAt).HasColumnType("datetime2");
        });

        modelBuilder.Entity<DeadLetterMessage>(entity =>
        {
            entity.HasIndex(message => message.MessageId).IsUnique();
            entity.Property(message => message.EventType).HasMaxLength(100).IsRequired();
            entity.Property(message => message.Payload).IsRequired();
            entity.Property(message => message.Error).IsRequired();
            entity.Property(message => message.FailedAt).HasColumnType("datetime2");
        });
    }
}
