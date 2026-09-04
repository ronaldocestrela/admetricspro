using BuildingBlocks.Domain.Primitives;
using Master.Application.FeatureFlags.DTOs;
using Master.Domain.FeatureFlags;
using Master.Domain.Integrations;

namespace BackofficeApp.Services;

/// <summary>
/// Contrato do serviço cliente para gerenciamento de Feature Flags e Kill Switches no frontend Blazor Server.
/// </summary>
public interface IFeatureFlagClientService
{
    /// <summary>
    /// Lista todas as feature flags e disjuntores operacionais.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado com a lista de feature flags.</returns>
    Task<Result<IReadOnlyList<FeatureFlagDto>>> GetAllFlagsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém uma feature flag pela chave identificadora.
    /// </summary>
    /// <param name="key">Chave única da flag.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Dados da flag.</returns>
    Task<Result<FeatureFlagDto>> GetByKeyAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica se as automações estão congeladas por um Kill Switch ativo.
    /// </summary>
    /// <param name="platform">Plataforma específica opcional.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Verdadeiro se estiver congelado.</returns>
    Task<bool> IsAutomationFrozenAsync(AdPlatform? platform = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Aciona um Kill Switch emergencial com justificativa.
    /// </summary>
    /// <param name="key">Chave do Kill Switch.</param>
    /// <param name="reason">Motivo obrigatório.</param>
    /// <param name="triggeredBy">Operador responsável.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado da operação.</returns>
    Task<Result> ActivateKillSwitchAsync(string key, string reason, string triggeredBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Desativa um Kill Switch com justificativa e restaura o funcionamento.
    /// </summary>
    /// <param name="key">Chave do Kill Switch.</param>
    /// <param name="reason">Motivo da restauração.</param>
    /// <param name="triggeredBy">Operador responsável.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado da operação.</returns>
    Task<Result> DeactivateKillSwitchAsync(string key, string reason, string triggeredBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atualiza configurações e percentual de rollout de uma flag.
    /// </summary>
    /// <param name="id">ID da flag.</param>
    /// <param name="isEnabled">Estado da flag.</param>
    /// <param name="targetingType">Tipo de segmentação.</param>
    /// <param name="rolloutPercentage">Percentual de rollout.</param>
    /// <param name="targetTenantIds">Lista de inquilinos autorizados.</param>
    /// <param name="updatedBy">Operador que realizou a alteração.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado da operação.</returns>
    Task<Result> UpdateFlagAsync(
        Guid id,
        bool isEnabled,
        FeatureFlagTargetingType targetingType,
        int rolloutPercentage,
        IReadOnlyCollection<Guid>? targetTenantIds,
        string updatedBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cadastra uma nova feature flag ou kill switch.
    /// </summary>
    /// <param name="key">Chave única.</param>
    /// <param name="name">Nome da flag.</param>
    /// <param name="description">Descrição.</param>
    /// <param name="isEnabled">Estado inicial.</param>
    /// <param name="isKillSwitch">Indica se é um Kill Switch.</param>
    /// <param name="targetingType">Tipo de segmentação.</param>
    /// <param name="rolloutPercentage">Percentual de rollout.</param>
    /// <param name="targetTenantIds">Inquilinos alvos.</param>
    /// <param name="createdBy">Criador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>ID da flag criada.</returns>
    Task<Result<Guid>> CreateFlagAsync(
        string key,
        string name,
        string description,
        bool isEnabled,
        bool isKillSwitch,
        FeatureFlagTargetingType targetingType,
        int rolloutPercentage,
        IReadOnlyCollection<Guid>? targetTenantIds,
        string createdBy,
        CancellationToken cancellationToken = default);
}
