using NexaLearn.Domain.Common;

namespace NexaLearn.Domain.Aggregates.Courses;

public sealed class Module : Entity<Guid>
{
    private readonly List<Lesson> _lessons = [];

    public string Title { get; }
    public IReadOnlyList<Lesson> Lessons => _lessons.AsReadOnly();
    public bool HasLessons => _lessons.Count > 0;

    private Module(Guid id, string title) : base(id) => Title = title;

    public static Result<Module> Create(Guid id, string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return Result<Module>.Failure("El título del módulo no puede estar vacío.");

        return Result<Module>.Success(new Module(id, title.Trim()));
    }

    public Result AddLesson(Lesson lesson)
    {
        if (_lessons.Any(l => l.Id == lesson.Id))
            return Result.Failure($"La lección '{lesson.Id}' ya existe en este módulo.");

        _lessons.Add(lesson);
        return Result.Success();
    }
}
