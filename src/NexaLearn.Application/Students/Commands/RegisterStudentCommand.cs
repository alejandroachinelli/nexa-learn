using MediatR;
using NexaLearn.Domain.Common;

namespace NexaLearn.Application.Students.Commands;

public record RegisterStudentCommand(string Name, string Email) : IRequest<Result<Guid>>;
