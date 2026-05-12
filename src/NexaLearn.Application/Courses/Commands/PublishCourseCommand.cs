using MediatR;
using NexaLearn.Domain.Common;

namespace NexaLearn.Application.Courses.Commands;

public record PublishCourseCommand(Guid CourseId) : IRequest<Result>;
