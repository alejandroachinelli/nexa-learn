using FluentAssertions;
using NexaLearn.Application.Courses.DTOs;
using NexaLearn.Application.Courses.Queries;
using NexaLearn.Application.Tests.Common.InMemory;
using NexaLearn.Domain.Aggregates.Courses;
using NexaLearn.Domain.ValueObjects;

namespace NexaLearn.Application.Tests.Courses;

public class ListPublishedCoursesTests
{
    // --- Helpers ---

    private static (ListPublishedCoursesQueryHandler handler, InMemoryCourseRepository repo) BuildHandler()
    {
        var repo = new InMemoryCourseRepository();
        var handler = new ListPublishedCoursesQueryHandler(repo);
        return (handler, repo);
    }

    private static async Task<Course> SeedPublishedCourse(InMemoryCourseRepository repo, string title)
    {
        var course = Course.Create(
            Guid.NewGuid(),
            CourseTitle.Create(title).Value,
            Money.Create(49.99m, "USD").Value
        ).Value;

        var module = Module.Create(Guid.NewGuid(), "Módulo 1").Value;
        module.AddLesson(Lesson.Create(Guid.NewGuid(), "Lección 1", Duration.Create(20).Value).Value);
        course.AddModule(module);
        course.Publish();

        await repo.AddAsync(course, CancellationToken.None);
        return course;
    }

    private static async Task<Course> SeedUnpublishedCourse(InMemoryCourseRepository repo, string title)
    {
        var course = Course.Create(
            Guid.NewGuid(),
            CourseTitle.Create(title).Value,
            Money.Create(29.99m, "USD").Value
        ).Value;
        await repo.AddAsync(course, CancellationToken.None);
        return course;
    }

    // --- Handler ---

    [Fact]
    public async Task Handler_OnlyReturnsPublishedCourses()
    {
        var (handler, repo) = BuildHandler();
        await SeedPublishedCourse(repo, "Clean Architecture con .NET 8");
        await SeedUnpublishedCourse(repo, "Domain-Driven Design en .NET");
        var query = new ListPublishedCoursesQuery();

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value.Should().AllSatisfy(dto => dto.IsPublished.Should().BeTrue());
    }

    [Fact]
    public async Task Handler_NoPublishedCourses_ReturnsEmptyList()
    {
        var (handler, repo) = BuildHandler();
        await SeedUnpublishedCourse(repo, "Domain-Driven Design en .NET");
        var query = new ListPublishedCoursesQuery();

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handler_MultiplePublishedCourses_ReturnsAll()
    {
        var (handler, repo) = BuildHandler();
        await SeedPublishedCourse(repo, "Clean Architecture con .NET 8");
        await SeedPublishedCourse(repo, "Domain-Driven Design en .NET");
        await SeedPublishedCourse(repo, "CQRS con MediatR");
        var query = new ListPublishedCoursesQuery();

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
        result.Value.Should().AllBeOfType<CourseDto>();
    }
}
