using BuildingBlocks.Domain.Abstractions;

namespace Master.Domain.FeatureFlags.Events;

/// <summary>
/// Domain event emitted when an operational emergency Kill Switch is deactivated/disengaged,
/// restoring standard subsystem operations.
/// </summary>
/// <param name="Key">Unique key of the kill switch flag.</param>
/// <param name="Name">Human readable name of the kill switch.</param>
/// <param name="Reason">Operational justification for restoring service.</param>
/// <param name="TriggeredBy">Identifier or email of the operator/admin who restored the service.</param>
/// <param name="DeactivatedAtUtc">Timestamp when the kill switch was deactivated.</param>
public sealed record KillSwitchDeactivatedDomainEvent(
    string Key,
    string Name,
    string Reason,
    string TriggeredBy,
    DateTime DeactivatedAtUtc) : IDomainEvent;
