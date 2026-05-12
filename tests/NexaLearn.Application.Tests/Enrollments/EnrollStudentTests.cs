using FluentAssertions;
using NexaLearn.Application.Enrollments.Commands;
using NexaLearn.Application.Tests.Common.InMemory;
using NexaLearn.Domain.Aggregates.Courses;
using NexaLearn.Domain.Aggregates.Students;
using NexaLearn.Domain.ValueObjects;

namespace NexaLearn.Application.Tests.Enrollments;

public class EnrollStudentTests
{
    // --- Helpers ---

    private static (
        EnrollStudentCommandHandler handler,
        InMemoryCourseRepository courses,
        InMemoryStudentRepository students,
        InMemoryEnrollmentRepository enrollments) BuildHandler()
    {
        var courses = new InMemoryCourseRepository();
        var students = new InMemoryStudentRepository();
        var enrollments = new InMemoryEnrollmentRepository();
        var uow = new InMemoryUnitOfWork();
        var handler = new EnrollStudentCommandHandler(courses, students, enrollments, uow);
        return (handler, courses, students, enrollments);
    }

    private static async Task<Course> CreatePublishedCourse(InMemoryCourseRepository repo)
    {
        var course = Course.Create(
            Guid.NewGuid(),
            CourseTitle.Create("Clean Architecture con .NET 8").Value,
            Money.Create(49.99m, "USD").Value
        ).Value;

        var module = Module.Create(Guid.NewGuid(), "Fundamentos").Value;
        module.AddLesson(Lesson.Create(Guid.NewGuid(), "Introducción", Duration.Create(30).Value).Value);
        course.AddModule(module);
        course.Publish();

        await repo.AddAsync(course, CancellationToken.None);
        return course;
    }

    private static async Task<Student> CreateStudent(InMemoryStudentRepository repo)
    {
        var student = Student.Create(
            Guid.NewGuid(),
            Email.Create("alejandro@example.com").Value,
            "Alejandro Martin"
        ).Value;

        await repo.AddAsync(student, CancellationToken.None);
        return student;
    }

    // --- Handler ---

    [Fact]
    public async Task Handler_ValidData_ReturnsSuccessWithEnrollmentId()
    {
        var (handler, courses, students, _) = BuildHandler();
        var course = await CreatePublishedCourse(courses);
        var student = await CreateStudent(students);
        var command = new EnrollStudentCommand(student.Id, course.Id);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handler_CourseNotFound_ReturnsFailure()
    {
        var (handler, _, students, _) = BuildHandler();
        var student = await CreateStudent(students);
        var command = new EnrollStudentCommand(student.Id, Guid.NewGuid());

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handler_StudentNotFound_ReturnsFailure()
    {
        var (handler, courses, _, _) = BuildHandler();
        var course = await CreatePublishedCourse(courses);
        var command = new EnrollStudentCommand(Guid.NewGuid(), course.Id);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handler_UnpublishedCourse_ReturnsFailure()
    {
        var (handler, courses, students, _) = BuildHandler();
        var unpublished = Course.Create(
            Guid.NewGuid(),
            CourseTitle.Create("Clean Architecture con .NET 8").Value,
            Money.Create(49.99m, "USD").Value
        ).Value;
        await courses.AddAsync(unpublished, CancellationToken.None);
        var student = await CreateStudent(students);
        var command = new EnrollStudentCommand(student.Id, unpublished.Id);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handler_DuplicateEnrollment_ReturnsFailure()
    {
        var (handler, courses, students, _) = BuildHandler();
        var course = await CreatePublishedCourse(courses);
        var student = await CreateStudent(students);
        var command = new EnrollStudentCommand(student.Id, course.Id);

        await handler.Handle(command, CancellationToken.None);
        var duplicate = await handler.Handle(command, CancellationToken.None);

        duplicate.IsFailure.Should().BeTrue();
    }
}
