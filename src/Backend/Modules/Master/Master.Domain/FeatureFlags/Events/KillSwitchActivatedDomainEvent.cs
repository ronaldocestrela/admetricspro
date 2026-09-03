using BuildingBlocks.Domain.Abstractions;

namespace Master.Domain.FeatureFlags.Events;

/// <summary>
/// Domain event emitted when an operational emergency Kill Switch is activated/engaged,
/// halting the associated subsystem or automation engine.
/// </summary>
/// <param name="Key">Unique key of the kill switch flag.</param>
/// <param name="Name">Human readable name of the kill switch.</param>
/// <param name="Reason">Mandatory operational justification for the freeze.</param>
/// <param name="TriggeredBy">Identifier or email of the operator/admin who triggered the kill switch.</param>
/// <param name="ActivatedAtUtc">Timestamp when the kill switch was engaged.</param>
public sealed record KillSwitchActivatedDomainEvent(
    string Key,
    string Name,
    string Reason,
    string TriggeredBy,
    DateTime ActivatedAtUtc) : IDomainEvent;
