namespace BuildingBlocks.Domain.Abstractions;

/// <summary>
/// Marker contract for domain events, optionally exposing identifier and occurrence timestamp.
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// Gets the unique identifier for the domain event.
    /// </summary>
    Guid EventId => Guid.NewGuid();

    /// <summary>
    /// Gets the UTC timestamp when the domain event occurred.
    /// </summary>
    DateTimeOffset OccurredOnUtc => DateTimeOffset.UtcNow;
}