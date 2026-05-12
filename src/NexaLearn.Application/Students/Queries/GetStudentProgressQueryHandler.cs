using MediatR;
using NexaLearn.Application.Students.DTOs;
using NexaLearn.Domain.Common;
using NexaLearn.Domain.Interfaces;

namespace NexaLearn.Application.Students.Queries;

public class GetStudentProgressQueryHandler : IRequestHandler<GetStudentProgressQuery, Result<StudentProgressDto>>
{
    private readonly IStudentRepository _students;
    private readonly IEnrollmentRepository _enrollments;
    private readonly ICourseRepository _courses;

    public GetStudentProgressQueryHandler(
        IStudentRepository students,
        IEnrollmentRepository enrollments,
        ICourseRepository courses)
    {
        _students = students;
        _enrollments = enrollments;
        _courses = courses;
    }

    public async Task<Result<StudentProgressDto>> Handle(GetStudentProgressQuery request, CancellationToken cancellationToken)
    {
        var student = await _students.GetByIdAsync(request.StudentId, cancellationToken);
        if (student is null)
            return Result<StudentProgressDto>.Failure("Estudiante no encontrado.");

        var enrollments = await _enrollments.GetByStudentAsync(request.StudentId, cancellationToken);

        var enrollmentProgress = new List<EnrollmentProgressDto>();
        foreach (var enrollment in enrollments)
        {
            var course = await _courses.GetByIdAsync(enrollment.CourseId, cancellationToken);
            if (course is null)
                continue;

            var totalLessons = course.Modules.SelectMany(m => m.Lessons).Count();
            enrollmentProgress.Add(new EnrollmentProgressDto(
                CourseId: course.Id,
                CourseTitle: course.Title.Value,
                CompletedLessons: enrollment.CompletedLessonIds.Count,
                TotalLessons: totalLessons));
        }

        return Result<StudentProgressDto>.Success(new StudentProgressDto(
            StudentId: student.Id,
            StudentName: student.Name,
            Enrollments: enrollmentProgress));
    }
}
