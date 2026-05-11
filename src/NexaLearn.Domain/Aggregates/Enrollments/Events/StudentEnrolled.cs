using NexaLearn.Domain.Common;

namespace NexaLearn.Domain.Aggregates.Enrollments.Events;

public record StudentEnrolled(
    Guid EnrollmentId,
    Guid StudentId,
    Guid CourseId,
    DateTimeOffset OccurredAt) : IDomainEvent;
