using BackendPractice.Models;
using Microsoft.EntityFrameworkCore;

namespace BackendPractice.Data;

public sealed class CourseDbContext(DbContextOptions<CourseDbContext> options) : DbContext(options)
{
    public DbSet<Course> Courses => Set<Course>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Course>(entity =>
        {
            entity.Property(course => course.Status).HasConversion<string>();
            entity.Property(course => course.CreatedAt).HasColumnType("datetime2");
            entity.Property(course => course.UpdatedAt).HasColumnType("datetime2");
        });
    }
}
