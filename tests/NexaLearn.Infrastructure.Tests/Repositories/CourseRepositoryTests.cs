using FluentAssertions;
using NexaLearn.Domain.Aggregates.Courses;
using NexaLearn.Domain.ValueObjects;
using NexaLearn.Infrastructure.Persistence.Repositories;
using NexaLearn.Infrastructure.Tests.Common;

namespace NexaLearn.Infrastructure.Tests.Repositories;

public class CourseRepositoryTests : IntegrationTestBase
{
    // --- Helpers ---

    private static Course CreateCourse(string title = "Clean Architecture con .NET 8")
    {
        return Course.Create(
            Guid.NewGuid(),
            CourseTitle.Create(title).Value,
            Money.Create(49.99m, "USD").Value
        ).Value;
    }

    private static Course CreatePublishedCourse(string title = "Clean Architecture con .NET 8")
    {
        var course = CreateCourse(title);
        var module = Module.Create(Guid.NewGuid(), "Fundamentos").Value;
        module.AddLesson(Lesson.Create(Guid.NewGuid(), "Introducción", Duration.Create(30).Value).Value);
        course.AddModule(module);
        course.Publish();
        return course;
    }

    // --- Tests ---

    [Fact]
    public async Task AddAsync_ThenGetById_ReturnsCourse()
    {
        var course = CreateCourse();

        await using var context = CreateDbContext();
        var repo = new CourseRepository(context);
        await repo.AddAsync(course, CancellationToken.None);
        await CreateUnitOfWork(context).SaveChangesAsync(CancellationToken.None);

        await using var readContext = CreateDbContext();
        var readRepo = new CourseRepository(readContext);
        var result = await readRepo.GetByIdAsync(course.Id, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(course.Id);
        result.Title.Value.Should().Be("Clean Architecture con .NET 8");
        result.Price.Amount.Should().Be(49.99m);
        result.Price.Currency.Should().Be("USD");
        result.IsPublished.Should().BeFalse();
    }

    [Fact]
    public async Task AddAsync_ThenGetPublished_ReturnsOnlyPublished()
    {
        var published = CreatePublishedCourse("Curso Publicado");
        var unpublished = CreateCourse("Curso No Publicado");

        await using var context = CreateDbContext();
        var repo = new CourseRepository(context);
        await repo.AddAsync(published, CancellationToken.None);
        await repo.AddAsync(unpublished, CancellationToken.None);
        await CreateUnitOfWork(context).SaveChangesAsync(CancellationToken.None);

        await using var readContext = CreateDbContext();
        var readRepo = new CourseRepository(readContext);
        var results = await readRepo.GetPublishedAsync(CancellationToken.None);

        results.Should().Contain(c => c.Id == published.Id);
        results.Should().NotContain(c => c.Id == unpublished.Id);
    }

    [Fact]
    public async Task AddAsync_WithModulesAndLessons_PersistsFullAggregate()
    {
        var course = CreateCourse();
        var module = Module.Create(Guid.NewGuid(), "Módulo DDD").Value;
        var lesson1 = Lesson.Create(Guid.NewGuid(), "Introducción a DDD", Duration.Create(45).Value).Value;
        var lesson2 = Lesson.Create(Guid.NewGuid(), "Aggregates", Duration.Create(60).Value).Value;
        module.AddLesson(lesson1);
        module.AddLesson(lesson2);
        course.AddModule(module);

        await using var context = CreateDbContext();
        var repo = new CourseRepository(context);
        await repo.AddAsync(course, CancellationToken.None);
        await CreateUnitOfWork(context).SaveChangesAsync(CancellationToken.None);

        await using var readContext = CreateDbContext();
        var readRepo = new CourseRepository(readContext);
        var result = await readRepo.GetByIdAsync(course.Id, CancellationToken.None);

        result!.Modules.Should().HaveCount(1);
        result.Modules[0].Title.Should().Be("Módulo DDD");
        result.Modules[0].Lessons.Should().HaveCount(2);
        result.Modules[0].Lessons.Should().Contain(l => l.Title == "Introducción a DDD" && l.Duration.Minutes == 45);
        result.Modules[0].Lessons.Should().Contain(l => l.Title == "Aggregates" && l.Duration.Minutes == 60);
    }

    [Fact]
    public async Task Update_PublishCourse_PersistsIsPublishedTrue()
    {
        var course = CreatePublishedCourse();

        await using var writeContext = CreateDbContext();
        var writeRepo = new CourseRepository(writeContext);
        await writeRepo.AddAsync(course, CancellationToken.None);
        await CreateUnitOfWork(writeContext).SaveChangesAsync(CancellationToken.None);

        await using var readContext = CreateDbContext();
        var readRepo = new CourseRepository(readContext);
        var result = await readRepo.GetByIdAsync(course.Id, CancellationToken.None);

        result!.IsPublished.Should().BeTrue();
    }
}
