namespace NexaLearn.Domain.Common;

public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : notnull
{
    protected AggregateRoot() { } // for EF Core materialization

    protected AggregateRoot(TId id) : base(id) { }
}
