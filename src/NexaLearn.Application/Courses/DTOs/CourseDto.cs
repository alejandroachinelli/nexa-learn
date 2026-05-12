using NexaLearn.Domain.Aggregates.Courses;

namespace NexaLearn.Application.Courses.DTOs;

public record CourseDto(
    Guid Id,
    string Title,
    decimal Price,
    string Currency,
    bool IsPublished,
    int ModuleCount)
{
    public static CourseDto FromDomain(Course course) => new(
        Id: course.Id,
        Title: course.Title.Value,
        Price: course.Price.Amount,
        Currency: course.Price.Currency,
        IsPublished: course.IsPublished,
        ModuleCount: course.Modules.Count);
}
