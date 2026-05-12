using MediatR;
using NexaLearn.Domain.Common;

namespace NexaLearn.Application.Enrollments.Commands;

public record EnrollStudentCommand(Guid StudentId, Guid CourseId) : IRequest<Result<Guid>>;
