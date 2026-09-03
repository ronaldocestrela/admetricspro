using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;
using Master.Application.FeatureFlags.Repositories;
using Master.Domain.FeatureFlags;

namespace Master.Application.FeatureFlags.Commands.UpdateFeatureFlag;

/// <summary>
/// Command handler for updating feature flags.
/// </summary>
public sealed class UpdateFeatureFlagCommandHandler : ICommandHandler<UpdateFeatureFlagCommand>
{
    private readonly IFeatureFlagRepository _repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateFeatureFlagCommandHandler"/> class.
    /// </summary>
    /// <param name="repository">Feature flag repository.</param>
    public UpdateFeatureFlagCommandHandler(IFeatureFlagRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    /// <inheritdoc />
    public async Task<Result> Handle(UpdateFeatureFlagCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var flag = await _repository.GetByIdAsync(command.Id, cancellationToken);
        if (flag is null)
            return Result.Failure(Error.NotFound("FeatureFlag.NotFound", $"Feature flag com ID '{command.Id}' não encontrada."));

        var now = DateTime.UtcNow;

        if (command.IsEnabled)
            flag.Enable(command.UpdatedBy, now);
        else
            flag.Disable(command.UpdatedBy, now);

        if (command.TargetingType == FeatureFlagTargetingType.PercentageRollout)
        {
            var rollResult = flag.SetRolloutPercentage(command.RolloutPercentage, command.UpdatedBy, now);
            if (rollResult.IsFailure)
                return rollResult;
        }
        else if (command.TargetingType == FeatureFlagTargetingType.TenantList)
        {
            flag.SetTenantTargeting(command.TargetTenantIds ?? Enumerable.Empty<Guid>(), command.UpdatedBy, now);
        }

        await _repository.UpdateAsync(flag, cancellationToken);
        return Result.Success();
    }
}
