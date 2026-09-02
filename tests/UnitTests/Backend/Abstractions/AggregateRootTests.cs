using BuildingBlocks.Domain.Abstractions;
using FluentAssertions;

namespace UnitTests.Backend.Abstractions;

/// <summary>
/// Unit tests for <see cref="AggregateRoot{TId}"/>.
/// </summary>
public sealed class AggregateRootTests
{
    private sealed record DummyDomainEvent(Guid EventId, DateTimeOffset OccurredOnUtc, string Payload) : IDomainEvent;

    private sealed class OrderAggregate : AggregateRoot<Guid>
    {
        public OrderAggregate(Guid id)
            : base(id)
        {
        }

        public void PlaceOrder(string orderNumber)
        {
            RaiseDomainEvent(new DummyDomainEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, orderNumber));
        }
    }

    /// <summary>
    /// Verifies domain events are empty upon initialization.
    /// </summary>
    [Fact]
    public void DomainEvents_ShouldBeEmpty_WhenAggregateIsInitialized()
    {
        // Arrange
        var aggregate = new OrderAggregate(Guid.NewGuid());

        // Act & Assert
        aggregate.DomainEvents.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies RaiseDomainEvent adds an event to collection.
    /// </summary>
    [Fact]
    public void RaiseDomainEvent_ShouldAddDomainEventToCollection()
    {
        // Arrange
        var aggregate = new OrderAggregate(Guid.NewGuid());

        // Act
        aggregate.PlaceOrder("ORD-001");

        // Assert
        aggregate.DomainEvents.Should().HaveCount(1);
        var domainEvent = aggregate.DomainEvents.First().Should().BeOfType<DummyDomainEvent>().Subject;
        domainEvent.Payload.Should().Be("ORD-001");
        domainEvent.EventId.Should().NotBeEmpty();
        domainEvent.OccurredOnUtc.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
    }

    /// <summary>
    /// Verifies ClearDomainEvents removes all events.
    /// </summary>
    [Fact]
    public void ClearDomainEvents_ShouldRemoveAllRegisteredDomainEvents()
    {
        // Arrange
        var aggregate = new OrderAggregate(Guid.NewGuid());
        aggregate.PlaceOrder("ORD-001");
        aggregate.PlaceOrder("ORD-002");
        aggregate.DomainEvents.Should().HaveCount(2);

        // Act
        aggregate.ClearDomainEvents();

        // Assert
        aggregate.DomainEvents.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies DomainEvents is exposed as a read-only collection.
    /// </summary>
    [Fact]
    public void DomainEvents_ShouldBeExposedAsReadOnlyCollection()
    {
        // Arrange
        var aggregate = new OrderAggregate(Guid.NewGuid());
        aggregate.PlaceOrder("ORD-001");

        // Act & Assert
        aggregate.DomainEvents.Should().BeAssignableTo<IReadOnlyCollection<IDomainEvent>>();
        aggregate.DomainEvents.Should().NotBeAssignableTo<List<IDomainEvent>>();
    }
}
