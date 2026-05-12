using FluentAssertions;
using NexaLearn.Application.Enrollments.Commands;
using NexaLearn.Application.Tests.Common.InMemory;
using NexaLearn.Domain.Aggregates.Courses;
using NexaLearn.Domain.Aggregates.Enrollments;
using NexaLearn.Domain.Aggregates.Students;
using NexaLearn.Domain.ValueObjects;

namespace NexaLearn.Application.Tests.Enrollments;

public class CompleteLessonTests
{
    // --- Helpers ---

    private static (
        CompleteLessonCommandHandler handler,
        InMemoryCourseRepository courses,
        InMemoryEnrollmentRepository enrollments) BuildHandler()
    {
        var courses = new InMemoryCourseRepository();
        var enrollments = new InMemoryEnrollmentRepository();
        var uow = new InMemoryUnitOfWork();
        var handler = new CompleteLessonCommandHandler(courses, enrollments, uow);
        return (handler, courses, enrollments);
    }

    private static async Task<(Course course, Lesson lesson, Enrollment enrollment)> SeedCourseAndEnrollment(
        InMemoryCourseRepository courseRepo,
        InMemoryEnrollmentRepository enrollmentRepo)
    {
        var course = Course.Create(
            Guid.NewGuid(),
            CourseTitle.Create("Clean Architecture con .NET 8").Value,
            Money.Create(49.99m, "USD").Value
        ).Value;

        var lesson = Lesson.Create(Guid.NewGuid(), "Introducción", Duration.Create(30).Value).Value;
        var module = Module.Create(Guid.NewGuid(), "Fundamentos").Value;
        module.AddLesson(lesson);
        course.AddModule(module);
        course.Publish();
        await courseRepo.AddAsync(course, CancellationToken.None);

        var enrollment = Enrollment.Create(Guid.NewGuid(), Guid.NewGuid(), course.Id, courseIsPublished: true).Value;
        await enrollmentRepo.AddAsync(enrollment, CancellationToken.None);

        return (course, lesson, enrollment);
    }

    // --- Handler ---

    [Fact]
    public async Task Handler_ValidLesson_ReturnsSuccess()
    {
        var (handler, courses, enrollments) = BuildHandler();
        var (_, lesson, enrollment) = await SeedCourseAndEnrollment(courses, enrollments);
        var command = new CompleteLessonCommand(enrollment.Id, lesson.Id);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handler_EnrollmentNotFound_ReturnsFailure()
    {
        var (handler, _, _) = BuildHandler();
        var command = new CompleteLessonCommand(Guid.NewGuid(), Guid.NewGuid());

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handler_LessonNotInCourse_ReturnsFailure()
    {
        var (handler, courses, enrollments) = BuildHandler();
        var (_, _, enrollment) = await SeedCourseAndEnrollment(courses, enrollments);
        var foreignLessonId = Guid.NewGuid();
        var command = new CompleteLessonCommand(enrollment.Id, foreignLessonId);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handler_LessonAlreadyCompleted_ReturnsFailure()
    {
        var (handler, courses, enrollments) = BuildHandler();
        var (_, lesson, enrollment) = await SeedCourseAndEnrollment(courses, enrollments);
        var command = new CompleteLessonCommand(enrollment.Id, lesson.Id);

        await handler.Handle(command, CancellationToken.None);
        var duplicate = await handler.Handle(command, CancellationToken.None);

        duplicate.IsFailure.Should().BeTrue();
    }
}
