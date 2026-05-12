using FluentAssertions;
using NexaLearn.Application.Courses.DTOs;
using NexaLearn.Application.Courses.Queries;
using NexaLearn.Application.Tests.Common.InMemory;
using NexaLearn.Domain.Aggregates.Courses;
using NexaLearn.Domain.ValueObjects;

namespace NexaLearn.Application.Tests.Courses;

public class GetCourseByIdTests
{
    // --- Helpers ---

    private static (GetCourseByIdQueryHandler handler, InMemoryCourseRepository repo) BuildHandler()
    {
        var repo = new InMemoryCourseRepository();
        var handler = new GetCourseByIdQueryHandler(repo);
        return (handler, repo);
    }

    private static async Task<Course> SeedCourse(InMemoryCourseRepository repo)
    {
        var course = Course.Create(
            Guid.NewGuid(),
            CourseTitle.Create("Domain-Driven Design en .NET").Value,
            Money.Create(59.99m, "EUR").Value
        ).Value;
        await repo.AddAsync(course, CancellationToken.None);
        return course;
    }

    // --- Handler ---

    [Fact]
    public async Task Handler_CourseExists_ReturnsSuccessWithDto()
    {
        var (handler, repo) = BuildHandler();
        var course = await SeedCourse(repo);
        var query = new GetCourseByIdQuery(course.Id);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeOfType<CourseDto>();
    }

    [Fact]
    public async Task Handler_CourseNotFound_ReturnsFailure()
    {
        var (handler, _) = BuildHandler();
        var query = new GetCourseByIdQuery(Guid.NewGuid());

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handler_CourseExists_DtoHasCorrectData()
    {
        var (handler, repo) = BuildHandler();
        var course = await SeedCourse(repo);
        var query = new GetCourseByIdQuery(course.Id);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Value.Id.Should().Be(course.Id);
        result.Value.Title.Should().Be("Domain-Driven Design en .NET");
        result.Value.Price.Should().Be(59.99m);
        result.Value.Currency.Should().Be("EUR");
        result.Value.IsPublished.Should().BeFalse();
        result.Value.ModuleCount.Should().Be(0);
    }
}
