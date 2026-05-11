using NexaLearn.Domain.Aggregates.Enrollments.Events;
using NexaLearn.Domain.Common;

namespace NexaLearn.Domain.Aggregates.Enrollments;

public sealed class Enrollment : AggregateRoot<Guid>
{
    private readonly List<Guid> _completedLessonIds = [];

    public Guid StudentId { get; }
    public Guid CourseId { get; }
    public DateTimeOffset EnrolledAt { get; }
    public IReadOnlyList<Guid> CompletedLessonIds => _completedLessonIds.AsReadOnly();

    private Enrollment(Guid id, Guid studentId, Guid courseId, DateTimeOffset enrolledAt)
        : base(id)
    {
        StudentId = studentId;
        CourseId = courseId;
        EnrolledAt = enrolledAt;
    }

    public static Result<Enrollment> Create(Guid id, Guid studentId, Guid courseId, bool courseIsPublished)
    {
        if (!courseIsPublished)
            return Result<Enrollment>.Failure("No es posible inscribirse en un curso que no está publicado.");

        var enrolledAt = DateTimeOffset.UtcNow;
        var enrollment = new Enrollment(id, studentId, courseId, enrolledAt);

        enrollment.AddDomainEvent(new StudentEnrolled(id, studentId, courseId, enrolledAt));

        return Result<Enrollment>.Success(enrollment);
    }

    public Result CompleteLesson(Guid lessonId, bool lessonBelongsToCourse)
    {
        if (!lessonBelongsToCourse)
            return Result.Failure("La lección no pertenece al curso de esta inscripción.");

        if (_completedLessonIds.Contains(lessonId))
            return Result.Failure("La lección ya fue completada.");

        _completedLessonIds.Add(lessonId);
        AddDomainEvent(new LessonCompleted(Id, StudentId, lessonId, DateTimeOffset.UtcNow));

        return Result.Success();
    }

    public bool HasCompletedLesson(Guid lessonId) => _completedLessonIds.Contains(lessonId);
}
