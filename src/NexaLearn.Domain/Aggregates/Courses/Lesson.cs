using NexaLearn.Domain.Common;
using NexaLearn.Domain.ValueObjects;

namespace NexaLearn.Domain.Aggregates.Courses;

public sealed class Lesson : Entity<Guid>
{
    public string Title { get; }
    public Duration Duration { get; }

    private Lesson(Guid id, string title, Duration duration) : base(id)
    {
        Title = title;
        Duration = duration;
    }

    public static Result<Lesson> Create(Guid id, string title, Duration duration)
    {
        if (string.IsNullOrWhiteSpace(title))
            return Result<Lesson>.Failure("El título de la lección no puede estar vacío.");

        return Result<Lesson>.Success(new Lesson(id, title.Trim(), duration));
    }
}
