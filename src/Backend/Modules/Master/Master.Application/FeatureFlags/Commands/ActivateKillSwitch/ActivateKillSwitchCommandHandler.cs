using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;
using Master.Application.FeatureFlags.Services;

namespace Master.Application.FeatureFlags.Commands.ActivateKillSwitch;

/// <summary>
/// Command handler for engaging emergency operational kill switches.
/// </summary>
public sealed class ActivateKillSwitchCommandHandler : ICommandHandler<ActivateKillSwitchCommand>
{
    private readonly IFeatureFlagService _featureFlagService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ActivateKillSwitchCommandHandler"/> class.
    /// </summary>
    /// <param name="featureFlagService">Feature flag service.</param>
    public ActivateKillSwitchCommandHandler(IFeatureFlagService featureFlagService)
    {
        _featureFlagService = featureFlagService ?? throw new ArgumentNullException(nameof(featureFlagService));
    }

    /// <inheritdoc />
    public async Task<Result> Handle(ActivateKillSwitchCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        return await _featureFlagService.ActivateKillSwitchAsync(
            command.Key,
            command.Reason,
            command.TriggeredBy,
            cancellationToken);
    }
}
