using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;
using Master.Application.FeatureFlags.Repositories;
using Master.Domain.FeatureFlags;

namespace Master.Application.FeatureFlags.Commands.CreateFeatureFlag;

/// <summary>
/// Command handler for creating and persisting new feature flags in the Master catalog.
/// </summary>
public sealed class CreateFeatureFlagCommandHandler : ICommandHandler<CreateFeatureFlagCommand, Guid>
{
    private readonly IFeatureFlagRepository _repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateFeatureFlagCommandHandler"/> class.
    /// </summary>
    /// <param name="repository">Feature flag repository.</param>
    public CreateFeatureFlagCommandHandler(IFeatureFlagRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(CreateFeatureFlagCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var existing = await _repository.GetByKeyAsync(command.Key, cancellationToken);
        if (existing != null)
        {
            return Result<Guid>.Failure(Error.Conflict(
                "FeatureFlag.AlreadyExists",
                $"Já existe uma feature flag com a chave '{command.Key}'."));
        }

        var flagResult = FeatureFlag.Create(
            command.Key,
            command.Name,
            command.Description,
            command.IsEnabled,
            command.IsKillSwitch,
            command.TargetingType,
            command.RolloutPercentage,
            command.TargetTenantIds,
            command.CreatedBy);

        if (flagResult.IsFailure)
            return Result<Guid>.Failure(flagResult.Error);

        var flag = flagResult.Value;
        await _repository.AddAsync(flag, cancellationToken);

        return Result<Guid>.Success(flag.Id);
    }
}
