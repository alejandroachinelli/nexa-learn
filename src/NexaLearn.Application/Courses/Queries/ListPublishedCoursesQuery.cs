using MediatR;
using NexaLearn.Application.Courses.DTOs;
using NexaLearn.Domain.Common;

namespace NexaLearn.Application.Courses.Queries;

public record ListPublishedCoursesQuery : IRequest<Result<IReadOnlyList<CourseDto>>>;
