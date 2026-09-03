using BuildingBlocks.Application.Messaging;

namespace Master.Application.FeatureFlags.Commands.ActivateKillSwitch;

/// <summary>
/// Command to engage an operational emergency Kill Switch, halting the affected subsystem.
/// </summary>
/// <param name="Key">Unique key of the kill switch flag.</param>
/// <param name="Reason">Mandatory operational justification.</param>
/// <param name="TriggeredBy">Identifier or email of the user/system executing the emergency stop.</param>
public sealed record ActivateKillSwitchCommand(
    string Key,
    string Reason,
    string TriggeredBy) : ICommand;
