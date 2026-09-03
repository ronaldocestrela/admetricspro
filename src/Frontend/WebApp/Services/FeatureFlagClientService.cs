using BuildingBlocks.Domain.Primitives;
using Master.Application.FeatureFlags.Commands.ActivateKillSwitch;
using Master.Application.FeatureFlags.Commands.CreateFeatureFlag;
using Master.Application.FeatureFlags.Commands.DeactivateKillSwitch;
using Master.Application.FeatureFlags.Commands.UpdateFeatureFlag;
using Master.Application.FeatureFlags.DTOs;
using Master.Application.FeatureFlags.Queries.GetFeatureFlagByKey;
using Master.Application.FeatureFlags.Queries.GetFeatureFlags;
using Master.Application.FeatureFlags.Services;
using Master.Domain.FeatureFlags;
using Master.Domain.Integrations;
using MediatR;

namespace WebApp.Services;

/// <summary>
/// Implementação do serviço cliente de Feature Flags e Kill Switches para o Blazor Server.
/// Despacha comandos e consultas diretamente in-memory via MediatR e IFeatureFlagService.
/// </summary>
public sealed class FeatureFlagClientService : IFeatureFlagClientService
{
    private readonly ISender _sender;
    private readonly IFeatureFlagService _featureFlagService;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="FeatureFlagClientService"/>.
    /// </summary>
    /// <param name="sender">Mediador in-memory.</param>
    /// <param name="featureFlagService">Serviço de avaliação de flags.</param>
    public FeatureFlagClientService(ISender sender, IFeatureFlagService featureFlagService)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
        _featureFlagService = featureFlagService ?? throw new ArgumentNullException(nameof(featureFlagService));
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<FeatureFlagDto>>> GetAllFlagsAsync(CancellationToken cancellationToken = default)
    {
        return await _sender.Send(new GetFeatureFlagsQuery(), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<FeatureFlagDto>> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        return await _sender.Send(new GetFeatureFlagByKeyQuery(key), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> IsAutomationFrozenAsync(AdPlatform? platform = null, CancellationToken cancellationToken = default)
    {
        return await _featureFlagService.IsAutomationFrozenAsync(platform, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result> ActivateKillSwitchAsync(
        string key,
        string reason,
        string triggeredBy,
        CancellationToken cancellationToken = default)
    {
        return await _sender.Send(new ActivateKillSwitchCommand(key, reason, triggeredBy), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result> DeactivateKillSwitchAsync(
        string key,
        string reason,
        string triggeredBy,
        CancellationToken cancellationToken = default)
    {
        return await _sender.Send(new DeactivateKillSwitchCommand(key, reason, triggeredBy), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result> UpdateFlagAsync(
        Guid id,
        bool isEnabled,
        FeatureFlagTargetingType targetingType,
        int rolloutPercentage,
        IReadOnlyCollection<Guid>? targetTenantIds,
        string updatedBy,
        CancellationToken cancellationToken = default)
    {
        return await _sender.Send(
            new UpdateFeatureFlagCommand(id, isEnabled, targetingType, rolloutPercentage, targetTenantIds, updatedBy),
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> CreateFlagAsync(
        string key,
        string name,
        string description,
        bool isEnabled,
        bool isKillSwitch,
        FeatureFlagTargetingType targetingType,
        int rolloutPercentage,
        IReadOnlyCollection<Guid>? targetTenantIds,
        string createdBy,
        CancellationToken cancellationToken = default)
    {
        return await _sender.Send(
            new CreateFeatureFlagCommand(
                key,
                name,
                description,
                isEnabled,
                isKillSwitch,
                targetingType,
                rolloutPercentage,
                targetTenantIds,
                createdBy),
            cancellationToken);
    }
}
