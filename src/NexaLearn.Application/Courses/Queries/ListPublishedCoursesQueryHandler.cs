using MediatR;
using NexaLearn.Application.Courses.DTOs;
using NexaLearn.Domain.Common;
using NexaLearn.Domain.Interfaces;

namespace NexaLearn.Application.Courses.Queries;

public class ListPublishedCoursesQueryHandler : IRequestHandler<ListPublishedCoursesQuery, Result<IReadOnlyList<CourseDto>>>
{
    private readonly ICourseRepository _courses;

    public ListPublishedCoursesQueryHandler(ICourseRepository courses)
    {
        _courses = courses;
    }

    public async Task<Result<IReadOnlyList<CourseDto>>> Handle(ListPublishedCoursesQuery request, CancellationToken cancellationToken)
    {
        var courses = await _courses.GetPublishedAsync(cancellationToken);
        var dtos = courses.Select(CourseDto.FromDomain).ToList();
        return Result<IReadOnlyList<CourseDto>>.Success(dtos);
    }
}
