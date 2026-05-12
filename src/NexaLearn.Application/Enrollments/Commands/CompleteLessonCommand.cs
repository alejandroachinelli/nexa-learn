using MediatR;
using NexaLearn.Domain.Common;

namespace NexaLearn.Application.Enrollments.Commands;

public record CompleteLessonCommand(Guid EnrollmentId, Guid LessonId) : IRequest<Result>;
