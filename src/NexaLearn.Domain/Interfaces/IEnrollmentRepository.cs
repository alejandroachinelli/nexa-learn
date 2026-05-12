using NexaLearn.Domain.Aggregates.Enrollments;

namespace NexaLearn.Domain.Interfaces;

public interface IEnrollmentRepository
{
    Task<Enrollment?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Enrollment?> GetByStudentAndCourseAsync(Guid studentId, Guid courseId, CancellationToken ct);
    Task<IReadOnlyList<Enrollment>> GetByStudentAsync(Guid studentId, CancellationToken ct);
    Task AddAsync(Enrollment enrollment, CancellationToken ct);
    void Update(Enrollment enrollment);
}
