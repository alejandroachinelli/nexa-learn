using Microsoft.EntityFrameworkCore;
using NexaLearn.Domain.Aggregates.Courses;
using NexaLearn.Domain.Aggregates.Enrollments;
using NexaLearn.Domain.Aggregates.Students;

namespace NexaLearn.Infrastructure.Persistence;

public class NexaLearnDbContext : DbContext
{
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();

    public NexaLearnDbContext(DbContextOptions<NexaLearnDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NexaLearnDbContext).Assembly);
    }
}
