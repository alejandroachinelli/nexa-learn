using NexaLearn.Domain.Aggregates.Enrollments;

namespace NexaLearn.Application.Enrollments.DTOs;

public record EnrollmentDto(
    Guid Id,
    Guid StudentId,
    Guid CourseId,
    DateTimeOffset EnrolledAt,
    int CompletedLessonCount)
{
    public static EnrollmentDto FromDomain(Enrollment enrollment) => new(
        Id: enrollment.Id,
        StudentId: enrollment.StudentId,
        CourseId: enrollment.CourseId,
        EnrolledAt: enrollment.EnrolledAt,
        CompletedLessonCount: enrollment.CompletedLessonIds.Count);
}
