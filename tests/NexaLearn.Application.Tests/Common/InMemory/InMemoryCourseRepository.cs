using NexaLearn.Domain.Aggregates.Courses;
using NexaLearn.Domain.Interfaces;

namespace NexaLearn.Application.Tests.Common.InMemory;

public class InMemoryCourseRepository : ICourseRepository
{
    private readonly List<Course> _courses = [];

    public Task<Course?> GetByIdAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(_courses.FirstOrDefault(c => c.Id == id));

    public Task<IReadOnlyList<Course>> GetPublishedAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Course>>(
            _courses.Where(c => c.IsPublished).ToList());

    public Task AddAsync(Course course, CancellationToken ct)
    {
        _courses.Add(course);
        return Task.CompletedTask;
    }

    public void Update(Course course) { }
}
