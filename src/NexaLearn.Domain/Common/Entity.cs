namespace NexaLearn.Domain.Common;

/// <typeparam name="TId">
/// Tipo del identificador. La constraint <c>notnull</c> es obligatoria: sin ella el compilador
/// permite usar <c>null</c> como Id, lo que hace explotar <see cref="GetHashCode"/> en runtime
/// cuando se usa la entidad en colecciones o diccionarios. La constraint convierte ese error
/// de runtime en un error de compilación.
/// </typeparam>
public abstract class Entity<TId>
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public TId Id { get; }
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected Entity(TId id) => Id = id;

    protected void AddDomainEvent(IDomainEvent domainEvent) =>
        _domainEvents.Add(domainEvent);

    public override bool Equals(object? obj)
    {
        if (obj is not Entity<TId> other) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;
        return Id.Equals(other.Id);
    }

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);
}
