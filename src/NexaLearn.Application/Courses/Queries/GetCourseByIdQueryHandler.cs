using MediatR;
using NexaLearn.Application.Courses.DTOs;
using NexaLearn.Domain.Common;
using NexaLearn.Domain.Interfaces;

namespace NexaLearn.Application.Courses.Queries;

public class GetCourseByIdQueryHandler : IRequestHandler<GetCourseByIdQuery, Result<CourseDto>>
{
    private readonly ICourseRepository _courses;

    public GetCourseByIdQueryHandler(ICourseRepository courses)
    {
        _courses = courses;
    }

    public async Task<Result<CourseDto>> Handle(GetCourseByIdQuery request, CancellationToken cancellationToken)
    {
        var course = await _courses.GetByIdAsync(request.CourseId, cancellationToken);
        if (course is null)
            return Result<CourseDto>.Failure("Curso no encontrado.");

        return Result<CourseDto>.Success(CourseDto.FromDomain(course));
    }
}
