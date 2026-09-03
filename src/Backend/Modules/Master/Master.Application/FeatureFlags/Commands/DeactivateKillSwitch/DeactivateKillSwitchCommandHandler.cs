using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;
using Master.Application.FeatureFlags.Services;

namespace Master.Application.FeatureFlags.Commands.DeactivateKillSwitch;

/// <summary>
/// Command handler for restoring normal operations by disengaging an active kill switch.
/// </summary>
public sealed class DeactivateKillSwitchCommandHandler : ICommandHandler<DeactivateKillSwitchCommand>
{
    private readonly IFeatureFlagService _featureFlagService;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeactivateKillSwitchCommandHandler"/> class.
    /// </summary>
    /// <param name="featureFlagService">Feature flag service.</param>
    public DeactivateKillSwitchCommandHandler(IFeatureFlagService featureFlagService)
    {
        _featureFlagService = featureFlagService ?? throw new ArgumentNullException(nameof(featureFlagService));
    }

    /// <inheritdoc />
    public async Task<Result> Handle(DeactivateKillSwitchCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        return await _featureFlagService.DeactivateKillSwitchAsync(
            command.Key,
            command.Reason,
            command.TriggeredBy,
            cancellationToken);
    }
}
