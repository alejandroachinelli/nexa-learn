using FluentAssertions;
using NexaLearn.Application.Tests.Common.InMemory;
using NexaLearn.Domain.Aggregates.Courses;
using NexaLearn.Domain.Aggregates.Enrollments;
using NexaLearn.Domain.Aggregates.Students;
using NexaLearn.Domain.Interfaces;
using NexaLearn.Domain.ValueObjects;

namespace NexaLearn.Application.Tests.Common.InMemory;

public class InMemoryRepositoryTests
{
    // --- Helpers ---

    private static Course CreatePublishedCourse()
    {
        var course = Course.Create(
            Guid.NewGuid(),
            CourseTitle.Create("Clean Architecture").Value,
            Money.Free
        ).Value;
        var module = Module.Create(Guid.NewGuid(), "Módulo 1").Value;
        module.AddLesson(Lesson.Create(Guid.NewGuid(), "Lección 1", Duration.Create(30).Value).Value);
        course.AddModule(module);
        course.Publish();
        return course;
    }

    private static Student CreateStudent() =>
        Student.Create(
            Guid.NewGuid(),
            Email.Create("student@example.com").Value,
            "Alejandro"
        ).Value;

    // --- ICourseRepository ---

    [Fact]
    public void InMemoryCourseRepository_ImplementsICourseRepository()
    {
        var repo = new InMemoryCourseRepository();

        repo.Should().BeAssignableTo<ICourseRepository>();
    }

    [Fact]
    public async Task InMemoryCourseRepository_AddAndGetById_ReturnsCorrectCourse()
    {
        var repo = new InMemoryCourseRepository();
        var course = Course.Create(
            Guid.NewGuid(),
            CourseTitle.Create("DDD Fundamentals").Value,
            Money.Free
        ).Value;

        await repo.AddAsync(course, CancellationToken.None);
        var retrieved = await repo.GetByIdAsync(course.Id, CancellationToken.None);

        retrieved.Should().NotBeNull();
        retrieved!.Id.Should().Be(course.Id);
    }

    [Fact]
    public async Task InMemoryCourseRepository_GetById_NotFound_ReturnsNull()
    {
        var repo = new InMemoryCourseRepository();

        var result = await repo.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task InMemoryCourseRepository_GetPublished_ReturnsOnlyPublishedCourses()
    {
        var repo = new InMemoryCourseRepository();
        var published = CreatePublishedCourse();
        var draft = Course.Create(Guid.NewGuid(), CourseTitle.Create("Borrador").Value, Money.Free).Value;

        await repo.AddAsync(published, CancellationToken.None);
        await repo.AddAsync(draft, CancellationToken.None);

        var result = await repo.GetPublishedAsync(CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Id.Should().Be(published.Id);
    }

    // --- IStudentRepository ---

    [Fact]
    public void InMemoryStudentRepository_ImplementsIStudentRepository()
    {
        var repo = new InMemoryStudentRepository();

        repo.Should().BeAssignableTo<IStudentRepository>();
    }

    [Fact]
    public async Task InMemoryStudentRepository_AddAndGetById_ReturnsCorrectStudent()
    {
        var repo = new InMemoryStudentRepository();
        var student = CreateStudent();

        await repo.AddAsync(student, CancellationToken.None);
        var retrieved = await repo.GetByIdAsync(student.Id, CancellationToken.None);

        retrieved.Should().NotBeNull();
        retrieved!.Id.Should().Be(student.Id);
    }

    [Fact]
    public async Task InMemoryStudentRepository_GetByEmail_ReturnsCorrectStudent()
    {
        var repo = new InMemoryStudentRepository();
        var email = Email.Create("student@example.com").Value;
        var student = Student.Create(Guid.NewGuid(), email, "Alejandro").Value;

        await repo.AddAsync(student, CancellationToken.None);
        var retrieved = await repo.GetByEmailAsync(email, CancellationToken.None);

        retrieved.Should().NotBeNull();
        retrieved!.Email.Should().Be(email);
    }

    [Fact]
    public async Task InMemoryStudentRepository_GetByEmail_NotFound_ReturnsNull()
    {
        var repo = new InMemoryStudentRepository();

        var result = await repo.GetByEmailAsync(
            Email.Create("unknown@example.com").Value,
            CancellationToken.None);

        result.Should().BeNull();
    }

    // --- IEnrollmentRepository ---

    [Fact]
    public void InMemoryEnrollmentRepository_ImplementsIEnrollmentRepository()
    {
        var repo = new InMemoryEnrollmentRepository();

        repo.Should().BeAssignableTo<IEnrollmentRepository>();
    }

    [Fact]
    public async Task InMemoryEnrollmentRepository_AddAndGetById_ReturnsCorrectEnrollment()
    {
        var repo = new InMemoryEnrollmentRepository();
        var enrollment = Enrollment.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            courseIsPublished: true
        ).Value;

        await repo.AddAsync(enrollment, CancellationToken.None);
        var retrieved = await repo.GetByIdAsync(enrollment.Id, CancellationToken.None);

        retrieved.Should().NotBeNull();
        retrieved!.Id.Should().Be(enrollment.Id);
    }

    [Fact]
    public async Task InMemoryEnrollmentRepository_GetByStudentAndCourse_ReturnsCorrectEnrollment()
    {
        var repo = new InMemoryEnrollmentRepository();
        var studentId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var enrollment = Enrollment.Create(
            Guid.NewGuid(), studentId, courseId,
            courseIsPublished: true
        ).Value;

        await repo.AddAsync(enrollment, CancellationToken.None);
        var retrieved = await repo.GetByStudentAndCourseAsync(studentId, courseId, CancellationToken.None);

        retrieved.Should().NotBeNull();
        retrieved!.StudentId.Should().Be(studentId);
        retrieved.CourseId.Should().Be(courseId);
    }

    [Fact]
    public async Task InMemoryEnrollmentRepository_GetByStudentAndCourse_WrongIds_ReturnsNull()
    {
        var repo = new InMemoryEnrollmentRepository();
        var enrollment = Enrollment.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            courseIsPublished: true
        ).Value;

        await repo.AddAsync(enrollment, CancellationToken.None);
        var result = await repo.GetByStudentAndCourseAsync(
            Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        result.Should().BeNull();
    }
}
