using NexaLearn.Domain.Aggregates.Students;
using NexaLearn.Domain.ValueObjects;

namespace NexaLearn.Domain.Interfaces;

public interface IStudentRepository
{
    Task<Student?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Student?> GetByEmailAsync(Email email, CancellationToken ct);
    Task AddAsync(Student student, CancellationToken ct);
}
