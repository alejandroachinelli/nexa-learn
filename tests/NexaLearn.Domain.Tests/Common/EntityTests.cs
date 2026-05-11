using FluentAssertions;
using NexaLearn.Domain.Common;

namespace NexaLearn.Domain.Tests.Common;

public class EntityTests
{
    // --- Clases concretas de prueba ---

    private class TestEntity : Entity<Guid>
    {
        public TestEntity(Guid id) : base(id) { }
        public void Raise(IDomainEvent domainEvent) => AddDomainEvent(domainEvent);
    }

    private class TestAggregateRoot : AggregateRoot<Guid>
    {
        public TestAggregateRoot(Guid id) : base(id) { }

        public void RaiseEvent(IDomainEvent domainEvent) => AddDomainEvent(domainEvent);
    }

    private record TestDomainEvent(string Name) : IDomainEvent;

    // --- Entity<TId> ---

    [Fact]
    public void Entity_SameId_AreEqual()
    {
        var id = Guid.NewGuid();
        var a = new TestEntity(id);
        var b = new TestEntity(id);

        a.Should().Be(b);
    }

    [Fact]
    public void Entity_DifferentId_AreNotEqual()
    {
        var a = new TestEntity(Guid.NewGuid());
        var b = new TestEntity(Guid.NewGuid());

        a.Should().NotBe(b);
    }

    [Fact]
    public void Entity_DomainEvents_InitiallyEmpty()
    {
        var entity = new TestEntity(Guid.NewGuid());

        entity.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Entity_AddDomainEvent_EventIsInCollection()
    {
        var entity = new TestEntity(Guid.NewGuid());
        var domainEvent = new TestDomainEvent("CoursePublished");

        entity.Raise(domainEvent);

        entity.DomainEvents.Should().ContainSingle()
            .Which.Should().Be(domainEvent);
    }

    [Fact]
    public void Entity_AddDomainEvent_MultipleEvents_AllPresent()
    {
        var entity = new TestEntity(Guid.NewGuid());
        var first = new TestDomainEvent("First");
        var second = new TestDomainEvent("Second");

        entity.Raise(first);
        entity.Raise(second);

        entity.DomainEvents.Should().HaveCount(2);
    }

    [Fact]
    public void Entity_DomainEvents_IsReadOnly()
    {
        var entity = new TestEntity(Guid.NewGuid());

        entity.DomainEvents.Should().BeAssignableTo<IReadOnlyList<IDomainEvent>>();
    }

    // --- AggregateRoot<TId> hereda de Entity<TId> ---

    [Fact]
    public void AggregateRoot_IsAnEntity()
    {
        var root = new TestAggregateRoot(Guid.NewGuid());

        root.Should().BeAssignableTo<Entity<Guid>>();
    }

    [Fact]
    public void AggregateRoot_SameId_AreEqual()
    {
        var id = Guid.NewGuid();
        var a = new TestAggregateRoot(id);
        var b = new TestAggregateRoot(id);

        a.Should().Be(b);
    }

    [Fact]
    public void AggregateRoot_RaisedEvent_IsInDomainEvents()
    {
        var root = new TestAggregateRoot(Guid.NewGuid());
        var domainEvent = new TestDomainEvent("StudentEnrolled");

        root.RaiseEvent(domainEvent);

        root.DomainEvents.Should().ContainSingle()
            .Which.Should().Be(domainEvent);
    }
}
