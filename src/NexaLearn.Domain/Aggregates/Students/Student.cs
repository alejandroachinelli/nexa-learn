using NexaLearn.Domain.Common;
using NexaLearn.Domain.ValueObjects;

namespace NexaLearn.Domain.Aggregates.Students;

public sealed class Student : AggregateRoot<Guid>
{
    public Email Email { get; }
    public string Name { get; }

    private Student(Guid id, Email email, string name) : base(id)
    {
        Email = email;
        Name = name;
    }

    public static Result<Student> Create(Guid id, Email email, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<Student>.Failure("El nombre del estudiante no puede estar vacío.");

        return Result<Student>.Success(new Student(id, email, name.Trim()));
    }
}
