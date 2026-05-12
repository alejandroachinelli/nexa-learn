using FluentAssertions;
using NexaLearn.Application.Courses.DTOs;
using NexaLearn.Application.Enrollments.DTOs;
using NexaLearn.Application.Students.DTOs;
using NexaLearn.Domain.Aggregates.Courses;
using NexaLearn.Domain.Aggregates.Enrollments;
using NexaLearn.Domain.Aggregates.Students;
using NexaLearn.Domain.ValueObjects;

namespace NexaLearn.Application.Tests.Common;

public class DtoMappingTests
{
    // --- Helpers ---

    private static Course CreateCourseWithModules()
    {
        var course = Course.Create(
            Guid.NewGuid(),
            CourseTitle.Create("Clean Architecture con .NET 8").Value,
            Money.Create(49.99m, "USD").Value
        ).Value;

        var module1 = Module.Create(Guid.NewGuid(), "Fundamentos").Value;
        module1.AddLesson(Lesson.Create(Guid.NewGuid(), "Introducción", Duration.Create(15).Value).Value);
        module1.AddLesson(Lesson.Create(Guid.NewGuid(), "Capas", Duration.Create(20).Value).Value);

        var module2 = Module.Create(Guid.NewGuid(), "Implementación").Value;
        module2.AddLesson(Lesson.Create(Guid.NewGuid(), "Dominio", Duration.Create(30).Value).Value);

        course.AddModule(module1);
        course.AddModule(module2);
        course.Publish();

        return course;
    }

    private static Student CreateStudent() =>
        Student.Create(
            Guid.NewGuid(),
            Email.Create("alejandro@example.com").Value,
            "Alejandro Martín"
        ).Value;

    // --- CourseDto ---

    [Fact]
    public void CourseDto_FromDomain_MapsId()
    {
        var course = CreateCourseWithModules();

        var dto = CourseDto.FromDomain(course);

        dto.Id.Should().Be(course.Id);
    }

    [Fact]
    public void CourseDto_FromDomain_MapsTitle()
    {
        var course = CreateCourseWithModules();

        var dto = CourseDto.FromDomain(course);

        dto.Title.Should().Be(course.Title.Value);
    }

    [Fact]
    public void CourseDto_FromDomain_MapsPrice()
    {
        var course = CreateCourseWithModules();

        var dto = CourseDto.FromDomain(course);

        dto.Price.Should().Be(course.Price.Amount);
    }

    [Fact]
    public void CourseDto_FromDomain_MapsCurrency()
    {
        var course = CreateCourseWithModules();

        var dto = CourseDto.FromDomain(course);

        dto.Currency.Should().Be(course.Price.Currency);
    }

    [Fact]
    public void CourseDto_FromDomain_MapsIsPublished()
    {
        var course = CreateCourseWithModules();

        var dto = CourseDto.FromDomain(course);

        dto.IsPublished.Should().BeTrue();
    }

    [Fact]
    public void CourseDto_FromDomain_MapsModuleCount()
    {
        var course = CreateCourseWithModules();

        var dto = CourseDto.FromDomain(course);

        dto.ModuleCount.Should().Be(2);
    }

    [Fact]
    public void CourseDto_FromDomain_UnpublishedCourse_IsPublishedFalse()
    {
        var course = Course.Create(
            Guid.NewGuid(),
            CourseTitle.Create("Borrador").Value,
            Money.Free
        ).Value;

        var dto = CourseDto.FromDomain(course);

        dto.IsPublished.Should().BeFalse();
    }

    [Fact]
    public void CourseDto_FromDomain_NoModules_ModuleCountIsZero()
    {
        var course = Course.Create(
            Guid.NewGuid(),
            CourseTitle.Create("Sin módulos").Value,
            Money.Free
        ).Value;

        var dto = CourseDto.FromDomain(course);

        dto.ModuleCount.Should().Be(0);
    }

    // --- ModuleDto ---

    [Fact]
    public void ModuleDto_FromDomain_MapsId()
    {
        var module = Module.Create(Guid.NewGuid(), "Fundamentos").Value;
        module.AddLesson(Lesson.Create(Guid.NewGuid(), "Intro", Duration.Create(10).Value).Value);

        var dto = ModuleDto.FromDomain(module);

        dto.Id.Should().Be(module.Id);
    }

    [Fact]
    public void ModuleDto_FromDomain_MapsTitle()
    {
        var module = Module.Create(Guid.NewGuid(), "Fundamentos").Value;

        var dto = ModuleDto.FromDomain(module);

        dto.Title.Should().Be("Fundamentos");
    }

    [Fact]
    public void ModuleDto_FromDomain_MapsLessonCount()
    {
        var module = Module.Create(Guid.NewGuid(), "Módulo").Value;
        module.AddLesson(Lesson.Create(Guid.NewGuid(), "L1", Duration.Create(10).Value).Value);
        module.AddLesson(Lesson.Create(Guid.NewGuid(), "L2", Duration.Create(20).Value).Value);

        var dto = ModuleDto.FromDomain(module);

        dto.LessonCount.Should().Be(2);
    }

    [Fact]
    public void ModuleDto_FromDomain_NoLessons_LessonCountIsZero()
    {
        var module = Module.Create(Guid.NewGuid(), "Vacío").Value;

        var dto = ModuleDto.FromDomain(module);

        dto.LessonCount.Should().Be(0);
    }

    // --- LessonDto ---

    [Fact]
    public void LessonDto_FromDomain_MapsId()
    {
        var lesson = Lesson.Create(Guid.NewGuid(), "Introducción", Duration.Create(30).Value).Value;

        var dto = LessonDto.FromDomain(lesson);

        dto.Id.Should().Be(lesson.Id);
    }

    [Fact]
    public void LessonDto_FromDomain_MapsTitle()
    {
        var lesson = Lesson.Create(Guid.NewGuid(), "Introducción", Duration.Create(30).Value).Value;

        var dto = LessonDto.FromDomain(lesson);

        dto.Title.Should().Be("Introducción");
    }

    [Fact]
    public void LessonDto_FromDomain_MapsDurationMinutes()
    {
        var lesson = Lesson.Create(Guid.NewGuid(), "Clase", Duration.Create(45).Value).Value;

        var dto = LessonDto.FromDomain(lesson);

        dto.DurationMinutes.Should().Be(45);
    }

    // --- StudentDto ---

    [Fact]
    public void StudentDto_FromDomain_MapsId()
    {
        var student = CreateStudent();

        var dto = StudentDto.FromDomain(student);

        dto.Id.Should().Be(student.Id);
    }

    [Fact]
    public void StudentDto_FromDomain_MapsName()
    {
        var student = CreateStudent();

        var dto = StudentDto.FromDomain(student);

        dto.Name.Should().Be("Alejandro Martín");
    }

    [Fact]
    public void StudentDto_FromDomain_MapsEmail()
    {
        var student = CreateStudent();

        var dto = StudentDto.FromDomain(student);

        dto.Email.Should().Be("alejandro@example.com");
    }

    [Fact]
    public void StudentDto_FromDomain_EmailIsString_NotValueObject()
    {
        var student = CreateStudent();

        var dto = StudentDto.FromDomain(student);

        dto.Email.Should().BeOfType<string>();
    }

    // --- EnrollmentDto ---

    [Fact]
    public void EnrollmentDto_FromDomain_MapsId()
    {
        var enrollment = Enrollment.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), courseIsPublished: true
        ).Value;

        var dto = EnrollmentDto.FromDomain(enrollment);

        dto.Id.Should().Be(enrollment.Id);
    }

    [Fact]
    public void EnrollmentDto_FromDomain_MapsStudentId()
    {
        var studentId = Guid.NewGuid();
        var enrollment = Enrollment.Create(
            Guid.NewGuid(), studentId, Guid.NewGuid(), courseIsPublished: true
        ).Value;

        var dto = EnrollmentDto.FromDomain(enrollment);

        dto.StudentId.Should().Be(studentId);
    }

    [Fact]
    public void EnrollmentDto_FromDomain_MapsCourseId()
    {
        var courseId = Guid.NewGuid();
        var enrollment = Enrollment.Create(
            Guid.NewGuid(), Guid.NewGuid(), courseId, courseIsPublished: true
        ).Value;

        var dto = EnrollmentDto.FromDomain(enrollment);

        dto.CourseId.Should().Be(courseId);
    }

    [Fact]
    public void EnrollmentDto_FromDomain_MapsEnrolledAt()
    {
        var before = DateTimeOffset.UtcNow.AddSeconds(-1);
        var enrollment = Enrollment.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), courseIsPublished: true
        ).Value;

        var dto = EnrollmentDto.FromDomain(enrollment);

        dto.EnrolledAt.Should().BeAfter(before);
    }

    [Fact]
    public void EnrollmentDto_FromDomain_NoCompletedLessons_CountIsZero()
    {
        var enrollment = Enrollment.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), courseIsPublished: true
        ).Value;

        var dto = EnrollmentDto.FromDomain(enrollment);

        dto.CompletedLessonCount.Should().Be(0);
    }

    [Fact]
    public void EnrollmentDto_FromDomain_WithCompletedLessons_CountIsCorrect()
    {
        var enrollment = Enrollment.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), courseIsPublished: true
        ).Value;
        enrollment.CompleteLesson(Guid.NewGuid(), lessonBelongsToCourse: true);
        enrollment.CompleteLesson(Guid.NewGuid(), lessonBelongsToCourse: true);

        var dto = EnrollmentDto.FromDomain(enrollment);

        dto.CompletedLessonCount.Should().Be(2);
    }
}
