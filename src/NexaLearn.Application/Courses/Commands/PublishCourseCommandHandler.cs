using MediatR;
using NexaLearn.Application.Common.Interfaces;
using NexaLearn.Domain.Common;
using NexaLearn.Domain.Interfaces;

namespace NexaLearn.Application.Courses.Commands;

public class PublishCourseCommandHandler : IRequestHandler<PublishCourseCommand, Result>
{
    private readonly ICourseRepository _courses;
    private readonly IUnitOfWork _uow;

    public PublishCourseCommandHandler(ICourseRepository courses, IUnitOfWork uow)
    {
        _courses = courses;
        _uow = uow;
    }

    public async Task<Result> Handle(PublishCourseCommand request, CancellationToken cancellationToken)
    {
        var course = await _courses.GetByIdAsync(request.CourseId, cancellationToken);
        if (course is null)
            return Result.Failure("Curso no encontrado.");

        var publishResult = course.Publish();
        if (publishResult.IsFailure)
            return publishResult;

        _courses.Update(course);
        await _uow.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
