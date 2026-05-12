using FluentAssertions;
using NexaLearn.Application.Courses.Commands;
using NexaLearn.Application.Tests.Common.InMemory;
using NexaLearn.Domain.Aggregates.Courses;
using NexaLearn.Domain.ValueObjects;

namespace NexaLearn.Application.Tests.Courses;

public class PublishCourseTests
{
    // --- Helpers ---

    private static (PublishCourseCommandHandler handler, InMemoryCourseRepository repo) BuildHandler()
    {
        var repo = new InMemoryCourseRepository();
        var uow = new InMemoryUnitOfWork();
        var handler = new PublishCourseCommandHandler(repo, uow);
        return (handler, repo);
    }

    private static async Task<Course> CreateAndSeedCourseWithLesson(InMemoryCourseRepository repo)
    {
        var course = Course.Create(
            Guid.NewGuid(),
            CourseTitle.Create("Clean Architecture con .NET 8").Value,
            Money.Create(49.99m, "USD").Value
        ).Value;

        var module = Module.Create(Guid.NewGuid(), "Fundamentos").Value;
        module.AddLesson(Lesson.Create(Guid.NewGuid(), "Introducción", Duration.Create(30).Value).Value);
        course.AddModule(module);

        await repo.AddAsync(course, CancellationToken.None);
        return course;
    }

    // --- Handler ---

    [Fact]
    public async Task Handler_CourseWithModuleAndLesson_ReturnsSuccess()
    {
        var (handler, repo) = BuildHandler();
        var course = await CreateAndSeedCourseWithLesson(repo);
        var command = new PublishCourseCommand(course.Id);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handler_CourseNotFound_ReturnsFailure()
    {
        var (handler, _) = BuildHandler();
        var command = new PublishCourseCommand(Guid.NewGuid());

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handler_CourseWithNoModules_ReturnsFailure()
    {
        var (handler, repo) = BuildHandler();
        var course = Course.Create(
            Guid.NewGuid(),
            CourseTitle.Create("Clean Architecture con .NET 8").Value,
            Money.Create(49.99m, "USD").Value
        ).Value;
        await repo.AddAsync(course, CancellationToken.None);
        var command = new PublishCourseCommand(course.Id);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}
