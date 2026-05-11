using NexaLearn.Domain.Common;

namespace NexaLearn.Domain.Aggregates.Enrollments.Events;

public record LessonCompleted(
    Guid EnrollmentId,
    Guid StudentId,
    Guid LessonId,
    DateTimeOffset OccurredAt) : IDomainEvent;
