using NexaLearn.Domain.Aggregates.Courses;

namespace NexaLearn.Domain.Interfaces;

public interface ICourseRepository
{
    Task<Course?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<Course>> GetPublishedAsync(CancellationToken ct);
    Task AddAsync(Course course, CancellationToken ct);
    void Update(Course course);
}
