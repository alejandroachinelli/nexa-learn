using FluentAssertions;
using NexaLearn.Application.Students.DTOs;
using NexaLearn.Application.Students.Queries;
using NexaLearn.Application.Tests.Common.InMemory;
using NexaLearn.Domain.Aggregates.Courses;
using NexaLearn.Domain.Aggregates.Enrollments;
using NexaLearn.Domain.Aggregates.Students;
using NexaLearn.Domain.ValueObjects;

namespace NexaLearn.Application.Tests.Enrollments;

public class GetStudentProgressTests
{
    // --- Helpers ---

    private static (
        GetStudentProgressQueryHandler handler,
        InMemoryStudentRepository students,
        InMemoryEnrollmentRepository enrollments,
        InMemoryCourseRepository courses) BuildHandler()
    {
        var students = new InMemoryStudentRepository();
        var enrollments = new InMemoryEnrollmentRepository();
        var courses = new InMemoryCourseRepository();
        var handler = new GetStudentProgressQueryHandler(students, enrollments, courses);
        return (handler, students, enrollments, courses);
    }

    private static async Task<Student> SeedStudent(InMemoryStudentRepository repo)
    {
        var student = Student.Create(
            Guid.NewGuid(),
            Email.Create("alejandro@example.com").Value,
            "Alejandro Martin"
        ).Value;
        await repo.AddAsync(student, CancellationToken.None);
        return student;
    }

    private static async Task<(Course course, Lesson lesson1, Lesson lesson2)> SeedPublishedCourse(
        InMemoryCourseRepository repo)
    {
        var course = Course.Create(
            Guid.NewGuid(),
            CourseTitle.Create("Clean Architecture con .NET 8").Value,
            Money.Create(49.99m, "USD").Value
        ).Value;

        var lesson1 = Lesson.Create(Guid.NewGuid(), "Introducción", Duration.Create(20).Value).Value;
        var lesson2 = Lesson.Create(Guid.NewGuid(), "Capas", Duration.Create(30).Value).Value;
        var module = Module.Create(Guid.NewGuid(), "Fundamentos").Value;
        module.AddLesson(lesson1);
        module.AddLesson(lesson2);
        course.AddModule(module);
        course.Publish();

        await repo.AddAsync(course, CancellationToken.None);
        return (course, lesson1, lesson2);
    }

    // --- Handler ---

    [Fact]
    public async Task Handler_StudentWithEnrollments_ReturnsStudentProgress()
    {
        var (handler, students, enrollments, courses) = BuildHandler();
        var student = await SeedStudent(students);
        var (course, _, _) = await SeedPublishedCourse(courses);
        var enrollment = Enrollment.Create(Guid.NewGuid(), student.Id, course.Id, courseIsPublished: true).Value;
        await enrollments.AddAsync(enrollment, CancellationToken.None);

        var result = await handler.Handle(new GetStudentProgressQuery(student.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.StudentId.Should().Be(student.Id);
        result.Value.StudentName.Should().Be("Alejandro Martin");
        result.Value.Enrollments.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handler_StudentNotFound_ReturnsFailure()
    {
        var (handler, _, _, _) = BuildHandler();

        var result = await handler.Handle(new GetStudentProgressQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handler_CompletedLessonsReflectedInProgress()
    {
        var (handler, students, enrollments, courses) = BuildHandler();
        var student = await SeedStudent(students);
        var (course, lesson1, _) = await SeedPublishedCourse(courses);
        var enrollment = Enrollment.Create(Guid.NewGuid(), student.Id, course.Id, courseIsPublished: true).Value;
        enrollment.CompleteLesson(lesson1.Id, lessonBelongsToCourse: true);
        await enrollments.AddAsync(enrollment, CancellationToken.None);

        var result = await handler.Handle(new GetStudentProgressQuery(student.Id), CancellationToken.None);

        var progress = result.Value.Enrollments.Single();
        progress.CompletedLessons.Should().Be(1);
    }

    [Fact]
    public async Task Handler_TotalLessonsReflectsCourseContent()
    {
        var (handler, students, enrollments, courses) = BuildHandler();
        var student = await SeedStudent(students);
        var (course, _, _) = await SeedPublishedCourse(courses);
        var enrollment = Enrollment.Create(Guid.NewGuid(), student.Id, course.Id, courseIsPublished: true).Value;
        await enrollments.AddAsync(enrollment, CancellationToken.None);

        var result = await handler.Handle(new GetStudentProgressQuery(student.Id), CancellationToken.None);

        var progress = result.Value.Enrollments.Single();
        progress.TotalLessons.Should().Be(2);
        progress.CourseTitle.Should().Be("Clean Architecture con .NET 8");
    }
}
