using MediatR;
using NexaLearn.Application.Courses.DTOs;
using NexaLearn.Domain.Common;

namespace NexaLearn.Application.Courses.Queries;

public record GetCourseByIdQuery(Guid CourseId) : IRequest<Result<CourseDto>>;
