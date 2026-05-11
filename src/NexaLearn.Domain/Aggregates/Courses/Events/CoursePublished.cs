using NexaLearn.Domain.Common;

namespace NexaLearn.Domain.Aggregates.Courses.Events;

public record CoursePublished(Guid CourseId, DateTimeOffset OccurredAt) : IDomainEvent;
