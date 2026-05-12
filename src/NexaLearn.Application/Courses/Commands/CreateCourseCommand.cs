using MediatR;
using NexaLearn.Domain.Common;

namespace NexaLearn.Application.Courses.Commands;

public record CreateCourseCommand(string Title, decimal Price, string Currency) : IRequest<Result<Guid>>;
