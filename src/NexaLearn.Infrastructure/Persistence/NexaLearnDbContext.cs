using Microsoft.EntityFrameworkCore;
using NexaLearn.Domain.Aggregates.Courses;
using NexaLearn.Domain.Aggregates.Enrollments;
using NexaLearn.Domain.Aggregates.Students;
using NexaLearn.Infrastructure.Persistence.Outbox;

namespace NexaLearn.Infrastructure.Persistence;

public class NexaLearnDbContext : DbContext
{
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<Module> Modules => Set<Module>();
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public NexaLearnDbContext(DbContextOptions<NexaLearnDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NexaLearnDbContext).Assembly);
    }
}
