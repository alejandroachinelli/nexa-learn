using FluentAssertions;
using NexaLearn.Domain.Aggregates.Courses;
using NexaLearn.Domain.Aggregates.Courses.Events;
using NexaLearn.Domain.ValueObjects;

namespace NexaLearn.Domain.Tests.Aggregates;

public class CourseTests
{
    // --- Helpers para construir objetos válidos ---

    private static Course CreateValidCourse() =>
        Course.Create(
            Guid.NewGuid(),
            CourseTitle.Create("Clean Architecture con .NET 8").Value,
            Money.Create(49.99m, "USD").Value
        ).Value;

    private static Module CreateValidModule(Guid? id = null) =>
        Module.Create(id ?? Guid.NewGuid(), "Introducción a Clean Architecture").Value;

    private static Lesson CreateValidLesson(Guid? id = null) =>
        Lesson.Create(
            id ?? Guid.NewGuid(),
            "¿Qué es Clean Architecture?",
            Duration.Create(30).Value
        ).Value;

    // --- Course.Create ---

    [Fact]
    public void Course_Create_IsNotPublished()
    {
        var course = CreateValidCourse();

        course.IsPublished.Should().BeFalse();
    }

    [Fact]
    public void Course_Create_HasNoModules()
    {
        var course = CreateValidCourse();

        course.Modules.Should().BeEmpty();
    }

    [Fact]
    public void Course_Create_HasCorrectTitle()
    {
        var title = CourseTitle.Create("Domain-Driven Design").Value;
        var course = Course.Create(Guid.NewGuid(), title, Money.Free).Value;

        course.Title.Should().Be(title);
    }

    [Fact]
    public void Course_Create_HasCorrectPrice()
    {
        var price = Money.Create(99m, "USD").Value;
        var course = Course.Create(Guid.NewGuid(), CourseTitle.Create("DDD").Value, price).Value;

        course.Price.Should().Be(price);
    }

    // --- Course.AddModule ---

    [Fact]
    public void Course_AddModule_AddsSuccessfully()
    {
        var course = CreateValidCourse();
        var module = CreateValidModule();

        var result = course.AddModule(module);

        result.IsSuccess.Should().BeTrue();
        course.Modules.Should().ContainSingle();
    }

    [Fact]
    public void Course_AddModule_DuplicateId_ReturnsFailure()
    {
        var course = CreateValidCourse();
        var id = Guid.NewGuid();
        var module = CreateValidModule(id);
        var duplicate = CreateValidModule(id);
        course.AddModule(module);

        var result = course.AddModule(duplicate);

        result.IsFailure.Should().BeTrue();
        course.Modules.Should().ContainSingle();
    }

    [Fact]
    public void Course_AddModule_WhenPublished_ReturnsFailure()
    {
        var course = CreateValidCourse();
        var module = CreateValidModule();
        module.AddLesson(CreateValidLesson());
        course.AddModule(module);
        course.Publish();

        var result = course.AddModule(CreateValidModule());

        result.IsFailure.Should().BeTrue();
    }

    // --- Course.Publish ---

    [Fact]
    public void Course_Publish_WithNoModules_ReturnsFailure()
    {
        var course = CreateValidCourse();

        var result = course.Publish();

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Course_Publish_WithModuleButNoLessons_ReturnsFailure()
    {
        var course = CreateValidCourse();
        course.AddModule(CreateValidModule());

        var result = course.Publish();

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Course_Publish_WithModuleAndLesson_Succeeds()
    {
        var course = CreateValidCourse();
        var module = CreateValidModule();
        module.AddLesson(CreateValidLesson());
        course.AddModule(module);

        var result = course.Publish();

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Course_Publish_SetsIsPublishedTrue()
    {
        var course = CreateValidCourse();
        var module = CreateValidModule();
        module.AddLesson(CreateValidLesson());
        course.AddModule(module);

        course.Publish();

        course.IsPublished.Should().BeTrue();
    }

    [Fact]
    public void Course_Publish_RaisesCoursePublishedEvent()
    {
        var course = CreateValidCourse();
        var module = CreateValidModule();
        module.AddLesson(CreateValidLesson());
        course.AddModule(module);

        course.Publish();

        course.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<CoursePublished>();
    }

    [Fact]
    public void Course_Publish_CoursePublishedEvent_HasCorrectCourseId()
    {
        var courseId = Guid.NewGuid();
        var course = Course.Create(
            courseId,
            CourseTitle.Create("DDD").Value,
            Money.Free
        ).Value;
        var module = CreateValidModule();
        module.AddLesson(CreateValidLesson());
        course.AddModule(module);

        course.Publish();

        var domainEvent = course.DomainEvents.OfType<CoursePublished>().Single();
        domainEvent.CourseId.Should().Be(courseId);
    }

    [Fact]
    public void Course_Publish_AlreadyPublished_ReturnsFailure()
    {
        var course = CreateValidCourse();
        var module = CreateValidModule();
        module.AddLesson(CreateValidLesson());
        course.AddModule(module);
        course.Publish();

        var result = course.Publish();

        result.IsFailure.Should().BeTrue();
    }

    // --- Module.AddLesson ---

    [Fact]
    public void Module_AddLesson_AddsSuccessfully()
    {
        var module = CreateValidModule();
        var lesson = CreateValidLesson();

        var result = module.AddLesson(lesson);

        result.IsSuccess.Should().BeTrue();
        module.Lessons.Should().ContainSingle();
    }

    [Fact]
    public void Module_AddLesson_DuplicateId_ReturnsFailure()
    {
        var module = CreateValidModule();
        var id = Guid.NewGuid();
        module.AddLesson(CreateValidLesson(id));

        var result = module.AddLesson(CreateValidLesson(id));

        result.IsFailure.Should().BeTrue();
        module.Lessons.Should().ContainSingle();
    }

    [Fact]
    public void Module_HasLessons_FalseWhenEmpty()
    {
        var module = CreateValidModule();

        module.HasLessons.Should().BeFalse();
    }

    [Fact]
    public void Module_HasLessons_TrueAfterAddingLesson()
    {
        var module = CreateValidModule();
        module.AddLesson(CreateValidLesson());

        module.HasLessons.Should().BeTrue();
    }

    // --- Lesson.Create ---

    [Fact]
    public void Lesson_Create_NullTitle_ReturnsFailure()
    {
        var result = Lesson.Create(Guid.NewGuid(), null!, Duration.Create(30).Value);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Lesson_Create_EmptyTitle_ReturnsFailure()
    {
        var result = Lesson.Create(Guid.NewGuid(), string.Empty, Duration.Create(30).Value);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Lesson_Create_ValidData_Succeeds()
    {
        var result = Lesson.Create(Guid.NewGuid(), "Introducción", Duration.Create(15).Value);

        result.IsSuccess.Should().BeTrue();
    }
}
