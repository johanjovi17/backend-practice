using BackendPractice.Models;
using Microsoft.EntityFrameworkCore;

namespace BackendPractice.Data;

public sealed class CourseDbContext(DbContextOptions<CourseDbContext> options) : DbContext(options)
{
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<User> Users => Set<User>();

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
    }
}
