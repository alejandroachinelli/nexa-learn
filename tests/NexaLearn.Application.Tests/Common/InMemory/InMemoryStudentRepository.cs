using NexaLearn.Domain.Aggregates.Students;
using NexaLearn.Domain.Interfaces;
using NexaLearn.Domain.ValueObjects;

namespace NexaLearn.Application.Tests.Common.InMemory;

public class InMemoryStudentRepository : IStudentRepository
{
    private readonly List<Student> _students = [];

    public Task<Student?> GetByIdAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(_students.FirstOrDefault(s => s.Id == id));

    public Task<Student?> GetByEmailAsync(Email email, CancellationToken ct) =>
        Task.FromResult(_students.FirstOrDefault(s => s.Email.Value == email.Value));

    public Task AddAsync(Student student, CancellationToken ct)
    {
        _students.Add(student);
        return Task.CompletedTask;
    }
}
