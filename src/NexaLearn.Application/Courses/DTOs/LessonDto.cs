using NexaLearn.Domain.Aggregates.Courses;

namespace NexaLearn.Application.Courses.DTOs;

public record LessonDto(
    Guid Id,
    string Title,
    int DurationMinutes)
{
    public static LessonDto FromDomain(Lesson lesson) => new(
        Id: lesson.Id,
        Title: lesson.Title,
        DurationMinutes: lesson.Duration.Minutes);
}
