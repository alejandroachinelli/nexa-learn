using NexaLearn.Domain.Aggregates.Courses;

namespace NexaLearn.Application.Courses.DTOs;

public record ModuleDto(
    Guid Id,
    string Title,
    int LessonCount)
{
    public static ModuleDto FromDomain(Module module) => new(
        Id: module.Id,
        Title: module.Title,
        LessonCount: module.Lessons.Count);
}
