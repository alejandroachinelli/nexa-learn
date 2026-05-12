using MediatR;
using NexaLearn.Application.Common.Interfaces;
using NexaLearn.Domain.Common;
using NexaLearn.Domain.Interfaces;

namespace NexaLearn.Application.Enrollments.Commands;

public class CompleteLessonCommandHandler : IRequestHandler<CompleteLessonCommand, Result>
{
    private readonly IEnrollmentRepository _enrollments;
    private readonly ICourseRepository _courses;
    private readonly IUnitOfWork _uow;

    public CompleteLessonCommandHandler(
        ICourseRepository courses,
        IEnrollmentRepository enrollments,
        IUnitOfWork uow)
    {
        _enrollments = enrollments;
        _courses = courses;
        _uow = uow;
    }

    public async Task<Result> Handle(CompleteLessonCommand request, CancellationToken cancellationToken)
    {
        var enrollment = await _enrollments.GetByIdAsync(request.EnrollmentId, cancellationToken);
        if (enrollment is null)
            return Result.Failure("Inscripción no encontrada.");

        var course = await _courses.GetByIdAsync(enrollment.CourseId, cancellationToken);
        if (course is null)
            return Result.Failure("Curso de la inscripción no encontrado.");

        bool lessonBelongs = course.Modules
            .SelectMany(m => m.Lessons)
            .Any(l => l.Id == request.LessonId);

        var result = enrollment.CompleteLesson(request.LessonId, lessonBelongs);
        if (result.IsFailure)
            return result;

        _enrollments.Update(enrollment);
        await _uow.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
