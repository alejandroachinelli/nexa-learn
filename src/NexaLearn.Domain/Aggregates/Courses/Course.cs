using NexaLearn.Domain.Aggregates.Courses.Events;
using NexaLearn.Domain.Common;
using NexaLearn.Domain.ValueObjects;

namespace NexaLearn.Domain.Aggregates.Courses;

public sealed class Course : AggregateRoot<Guid>
{
    private readonly List<Module> _modules = [];

    public CourseTitle Title { get; }
    public Money Price { get; }
    public bool IsPublished { get; private set; }
    public IReadOnlyList<Module> Modules => _modules.AsReadOnly();

#pragma warning disable CS8618
    private Course() { } // for EF Core materialization — properties set via backing fields
#pragma warning restore CS8618

    private Course(Guid id, CourseTitle title, Money price) : base(id)
    {
        Title = title;
        Price = price;
        IsPublished = false;
    }

    public static Result<Course> Create(Guid id, CourseTitle title, Money price) =>
        Result<Course>.Success(new Course(id, title, price));

    public Result AddModule(Module module)
    {
        if (IsPublished)
            return Result.Failure("No se pueden agregar módulos a un curso publicado.");

        if (_modules.Any(m => m.Id == module.Id))
            return Result.Failure($"El módulo '{module.Id}' ya existe en este curso.");

        _modules.Add(module);
        return Result.Success();
    }

    public Result Publish()
    {
        if (IsPublished)
            return Result.Failure("El curso ya está publicado.");

        if (_modules.Count == 0)
            return Result.Failure("El curso debe tener al menos un módulo para publicarse.");

        if (!_modules.Any(m => m.HasLessons))
            return Result.Failure("Al menos un módulo debe tener lecciones para publicar el curso.");

        IsPublished = true;
        AddDomainEvent(new CoursePublished(Id, DateTimeOffset.UtcNow));
        return Result.Success();
    }
}
