using NexaLearn.Domain.Aggregates.Students;

namespace NexaLearn.Application.Students.DTOs;

public record StudentDto(
    Guid Id,
    string Name,
    string Email)
{
    public static StudentDto FromDomain(Student student) => new(
        Id: student.Id,
        Name: student.Name,
        Email: student.Email.Value);
}
