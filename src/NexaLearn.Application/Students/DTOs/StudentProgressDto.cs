namespace NexaLearn.Application.Students.DTOs;

public record EnrollmentProgressDto(
    Guid CourseId,
    string CourseTitle,
    int CompletedLessons,
    int TotalLessons);

public record StudentProgressDto(
    Guid StudentId,
    string StudentName,
    IReadOnlyList<EnrollmentProgressDto> Enrollments);
