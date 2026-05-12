using NexaLearn.Domain.Aggregates.Enrollments;
using NexaLearn.Domain.Interfaces;

namespace NexaLearn.Application.Tests.Common.InMemory;

public class InMemoryEnrollmentRepository : IEnrollmentRepository
{
    private readonly List<Enrollment> _enrollments = [];

    public Task<Enrollment?> GetByIdAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(_enrollments.FirstOrDefault(e => e.Id == id));

    public Task<Enrollment?> GetByStudentAndCourseAsync(Guid studentId, Guid courseId, CancellationToken ct) =>
        Task.FromResult(_enrollments.FirstOrDefault(e =>
            e.StudentId == studentId && e.CourseId == courseId));

    public Task<IReadOnlyList<Enrollment>> GetByStudentAsync(Guid studentId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Enrollment>>(
            _enrollments.Where(e => e.StudentId == studentId).ToList());

    public Task AddAsync(Enrollment enrollment, CancellationToken ct)
    {
        _enrollments.Add(enrollment);
        return Task.CompletedTask;
    }

    public void Update(Enrollment enrollment) { }
}
