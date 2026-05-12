using MediatR;
using NexaLearn.Application.Common.Interfaces;
using NexaLearn.Domain.Aggregates.Courses;
using NexaLearn.Domain.Common;
using NexaLearn.Domain.Interfaces;
using NexaLearn.Domain.ValueObjects;

namespace NexaLearn.Application.Courses.Commands;

public class CreateCourseCommandHandler : IRequestHandler<CreateCourseCommand, Result<Guid>>
{
    private readonly ICourseRepository _courses;
    private readonly IUnitOfWork _uow;

    public CreateCourseCommandHandler(ICourseRepository courses, IUnitOfWork uow)
    {
        _courses = courses;
        _uow = uow;
    }

    public async Task<Result<Guid>> Handle(CreateCourseCommand request, CancellationToken cancellationToken)
    {
        var titleResult = CourseTitle.Create(request.Title);
        if (titleResult.IsFailure)
            return Result<Guid>.Failure(titleResult.Error);

        var priceResult = Money.Create(request.Price, request.Currency);
        if (priceResult.IsFailure)
            return Result<Guid>.Failure(priceResult.Error);

        var courseResult = Course.Create(Guid.NewGuid(), titleResult.Value, priceResult.Value);
        if (courseResult.IsFailure)
            return Result<Guid>.Failure(courseResult.Error);

        await _courses.AddAsync(courseResult.Value, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(courseResult.Value.Id);
    }
}
