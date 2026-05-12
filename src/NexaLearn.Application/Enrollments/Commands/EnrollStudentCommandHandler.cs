using MediatR;
using NexaLearn.Application.Common.Interfaces;
using NexaLearn.Domain.Aggregates.Enrollments;
using NexaLearn.Domain.Common;
using NexaLearn.Domain.Interfaces;

namespace NexaLearn.Application.Enrollments.Commands;

public class EnrollStudentCommandHandler : IRequestHandler<EnrollStudentCommand, Result<Guid>>
{
    private readonly ICourseRepository _courses;
    private readonly IStudentRepository _students;
    private readonly IEnrollmentRepository _enrollments;
    private readonly IUnitOfWork _uow;

    public EnrollStudentCommandHandler(
        ICourseRepository courses,
        IStudentRepository students,
        IEnrollmentRepository enrollments,
        IUnitOfWork uow)
    {
        _courses = courses;
        _students = students;
        _enrollments = enrollments;
        _uow = uow;
    }

    public async Task<Result<Guid>> Handle(EnrollStudentCommand request, CancellationToken cancellationToken)
    {
        var course = await _courses.GetByIdAsync(request.CourseId, cancellationToken);
        if (course is null)
            return Result<Guid>.Failure("Curso no encontrado.");

        var student = await _students.GetByIdAsync(request.StudentId, cancellationToken);
        if (student is null)
            return Result<Guid>.Failure("Estudiante no encontrado.");

        var existing = await _enrollments.GetByStudentAndCourseAsync(request.StudentId, request.CourseId, cancellationToken);
        if (existing is not null)
            return Result<Guid>.Failure("El estudiante ya está inscrito en este curso.");

        var enrollmentResult = Enrollment.Create(Guid.NewGuid(), request.StudentId, request.CourseId, course.IsPublished);
        if (enrollmentResult.IsFailure)
            return Result<Guid>.Failure(enrollmentResult.Error);

        await _enrollments.AddAsync(enrollmentResult.Value, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(enrollmentResult.Value.Id);
    }
}
