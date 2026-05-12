using MediatR;
using NexaLearn.Application.Common.Interfaces;
using NexaLearn.Domain.Aggregates.Students;
using NexaLearn.Domain.Common;
using NexaLearn.Domain.Interfaces;
using NexaLearn.Domain.ValueObjects;

namespace NexaLearn.Application.Students.Commands;

public class RegisterStudentCommandHandler : IRequestHandler<RegisterStudentCommand, Result<Guid>>
{
    private readonly IStudentRepository _students;
    private readonly IUnitOfWork _uow;

    public RegisterStudentCommandHandler(IStudentRepository students, IUnitOfWork uow)
    {
        _students = students;
        _uow = uow;
    }

    public async Task<Result<Guid>> Handle(RegisterStudentCommand request, CancellationToken cancellationToken)
    {
        var emailResult = Email.Create(request.Email);
        if (emailResult.IsFailure)
            return Result<Guid>.Failure(emailResult.Error);

        var existing = await _students.GetByEmailAsync(emailResult.Value, cancellationToken);
        if (existing is not null)
            return Result<Guid>.Failure("Ya existe un estudiante registrado con ese email.");

        var studentResult = Student.Create(Guid.NewGuid(), emailResult.Value, request.Name);
        if (studentResult.IsFailure)
            return Result<Guid>.Failure(studentResult.Error);

        await _students.AddAsync(studentResult.Value, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(studentResult.Value.Id);
    }
}
