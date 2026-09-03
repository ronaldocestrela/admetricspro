using BuildingBlocks.Application.Messaging;

namespace Master.Application.FeatureFlags.Commands.DeactivateKillSwitch;

/// <summary>
/// Command to disengage an emergency Kill Switch, resuming normal system operations.
/// </summary>
/// <param name="Key">Unique key of the kill switch flag.</param>
/// <param name="Reason">Mandatory operational justification for restoration.</param>
/// <param name="TriggeredBy">Identifier or email of the user/system restoring operations.</param>
public sealed record DeactivateKillSwitchCommand(
    string Key,
    string Reason,
    string TriggeredBy) : ICommand;
