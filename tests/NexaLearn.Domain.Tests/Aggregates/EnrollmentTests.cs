using FluentAssertions;
using NexaLearn.Domain.Aggregates.Enrollments;
using NexaLearn.Domain.Aggregates.Enrollments.Events;

namespace NexaLearn.Domain.Tests.Aggregates;

public class EnrollmentTests
{
    // --- Enrollment.Create ---

    [Fact]
    public void Enrollment_Create_PublishedCourse_Succeeds()
    {
        var result = Enrollment.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), courseIsPublished: true);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Enrollment_Create_UnpublishedCourse_ReturnsFailure()
    {
        var result = Enrollment.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), courseIsPublished: false);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Enrollment_Create_RaisesStudentEnrolledEvent()
    {
        var result = Enrollment.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), courseIsPublished: true);

        result.Value.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<StudentEnrolled>();
    }

    [Fact]
    public void Enrollment_Create_StudentEnrolledEvent_HasCorrectIds()
    {
        var enrollmentId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var courseId = Guid.NewGuid();

        var result = Enrollment.Create(enrollmentId, studentId, courseId, courseIsPublished: true);

        var domainEvent = result.Value.DomainEvents.OfType<StudentEnrolled>().Single();
        domainEvent.EnrollmentId.Should().Be(enrollmentId);
        domainEvent.StudentId.Should().Be(studentId);
        domainEvent.CourseId.Should().Be(courseId);
    }

    [Fact]
    public void Enrollment_Create_ExposesCorrectStudentAndCourseId()
    {
        var studentId = Guid.NewGuid();
        var courseId = Guid.NewGuid();

        var result = Enrollment.Create(Guid.NewGuid(), studentId, courseId, courseIsPublished: true);

        result.Value.StudentId.Should().Be(studentId);
        result.Value.CourseId.Should().Be(courseId);
    }

    [Fact]
    public void Enrollment_Create_CompletedLessons_InitiallyEmpty()
    {
        var result = Enrollment.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), courseIsPublished: true);

        result.Value.CompletedLessonIds.Should().BeEmpty();
    }

    // --- Enrollment.CompleteLesson ---

    [Fact]
    public void Enrollment_CompleteLesson_ValidLesson_Succeeds()
    {
        var enrollment = Enrollment.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), courseIsPublished: true).Value;

        var result = enrollment.CompleteLesson(Guid.NewGuid(), lessonBelongsToCourse: true);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Enrollment_CompleteLesson_LessonNotInCourse_ReturnsFailure()
    {
        var enrollment = Enrollment.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), courseIsPublished: true).Value;

        var result = enrollment.CompleteLesson(Guid.NewGuid(), lessonBelongsToCourse: false);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Enrollment_CompleteLesson_AlreadyCompleted_ReturnsFailure()
    {
        var enrollment = Enrollment.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), courseIsPublished: true).Value;
        var lessonId = Guid.NewGuid();
        enrollment.CompleteLesson(lessonId, lessonBelongsToCourse: true);

        var result = enrollment.CompleteLesson(lessonId, lessonBelongsToCourse: true);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Enrollment_CompleteLesson_RaisesLessonCompletedEvent()
    {
        var enrollment = Enrollment.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), courseIsPublished: true).Value;
        var lessonId = Guid.NewGuid();

        enrollment.CompleteLesson(lessonId, lessonBelongsToCourse: true);

        enrollment.DomainEvents.OfType<LessonCompleted>()
            .Should().ContainSingle()
            .Which.LessonId.Should().Be(lessonId);
    }

    [Fact]
    public void Enrollment_CompleteLesson_LessonCompletedEvent_HasCorrectIds()
    {
        var enrollmentId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var lessonId = Guid.NewGuid();
        var enrollment = Enrollment.Create(enrollmentId, studentId, Guid.NewGuid(), courseIsPublished: true).Value;

        enrollment.CompleteLesson(lessonId, lessonBelongsToCourse: true);

        var domainEvent = enrollment.DomainEvents.OfType<LessonCompleted>().Single();
        domainEvent.EnrollmentId.Should().Be(enrollmentId);
        domainEvent.StudentId.Should().Be(studentId);
        domainEvent.LessonId.Should().Be(lessonId);
    }

    // --- Enrollment.HasCompletedLesson ---

    [Fact]
    public void Enrollment_HasCompletedLesson_ReturnsFalseInitially()
    {
        var enrollment = Enrollment.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), courseIsPublished: true).Value;

        enrollment.HasCompletedLesson(Guid.NewGuid()).Should().BeFalse();
    }

    [Fact]
    public void Enrollment_HasCompletedLesson_ReturnsTrueAfterCompletion()
    {
        var enrollment = Enrollment.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), courseIsPublished: true).Value;
        var lessonId = Guid.NewGuid();
        enrollment.CompleteLesson(lessonId, lessonBelongsToCourse: true);

        enrollment.HasCompletedLesson(lessonId).Should().BeTrue();
    }

    [Fact]
    public void Enrollment_HasCompletedLesson_OtherLesson_ReturnsFalse()
    {
        var enrollment = Enrollment.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), courseIsPublished: true).Value;
        enrollment.CompleteLesson(Guid.NewGuid(), lessonBelongsToCourse: true);

        enrollment.HasCompletedLesson(Guid.NewGuid()).Should().BeFalse();
    }

    [Fact]
    public void Enrollment_CompletedLessonIds_ContainsCompletedLesson()
    {
        var enrollment = Enrollment.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), courseIsPublished: true).Value;
        var lessonId = Guid.NewGuid();

        enrollment.CompleteLesson(lessonId, lessonBelongsToCourse: true);

        enrollment.CompletedLessonIds.Should().Contain(lessonId);
    }
}
