using Master.Domain.FeatureFlags;
using Master.Domain.Integrations;

namespace WebApi.Models;

/// <summary>
/// Payload para criação de nova feature flag ou disjuntor operacional.
/// </summary>
/// <param name="Key">Chave identificadora única (ex: 'feature.analytics.mer-v2').</param>
/// <param name="Name">Nome legível da flag.</param>
/// <param name="Description">Descrição funcional ou técnica.</param>
/// <param name="IsEnabled">Estado inicial ativado/desativado.</param>
/// <param name="IsKillSwitch">Indica se a flag atua como Kill Switch de segurança.</param>
/// <param name="TargetingType">Tipo de segmentação (Global, PercentageRollout, TenantList).</param>
/// <param name="RolloutPercentage">Percentual de liberação progressiva (0 a 100).</param>
/// <param name="TargetTenantIds">Lista de inquilinos autorizados quando a segmentação for TenantList.</param>
public sealed record CreateFeatureFlagApiRequest(
    string Key,
    string Name,
    string Description,
    bool IsEnabled,
    bool IsKillSwitch,
    FeatureFlagTargetingType TargetingType,
    int RolloutPercentage,
    IReadOnlyCollection<Guid>? TargetTenantIds);

/// <summary>
/// Payload para atualização de configurações de uma feature flag existente.
/// </summary>
/// <param name="IsEnabled">Estado da flag.</param>
/// <param name="TargetingType">Tipo de segmentação.</param>
/// <param name="RolloutPercentage">Percentual de rollout (0 a 100).</param>
/// <param name="TargetTenantIds">Lista de inquilinos alvos.</param>
public sealed record UpdateFeatureFlagApiRequest(
    bool IsEnabled,
    FeatureFlagTargetingType TargetingType,
    int RolloutPercentage,
    IReadOnlyCollection<Guid>? TargetTenantIds);

/// <summary>
/// Payload para acionamento emergencial de um Kill Switch.
/// </summary>
/// <param name="Reason">Justificativa operacional obrigatória para congelar o subsistema.</param>
/// <param name="TriggeredBy">Identificador do operador ou serviço responsável.</param>
public sealed record ActivateKillSwitchApiRequest(
    string Reason,
    string? TriggeredBy = null);

/// <summary>
/// Payload para desativação/restauração de um Kill Switch.
/// </summary>
/// <param name="Reason">Justificativa operacional para restauração do subsistema.</param>
/// <param name="TriggeredBy">Identificador do operador ou serviço responsável.</param>
public sealed record DeactivateKillSwitchApiRequest(
    string Reason,
    string? TriggeredBy = null);

/// <summary>
/// Resumo do status operacional de congelamento do motor de automações.
/// </summary>
/// <param name="IsFrozen">Se verdadeiro, a execução de automações está bloqueada.</param>
/// <param name="Platform">Plataforma consultada (ou nulo para status global).</param>
/// <param name="ActiveKillSwitchKey">Chave do Kill Switch que causou o congelamento, se houver.</param>
/// <param name="CheckedAtUtc">Timestamp da verificação em UTC.</param>
public sealed record AutomationEngineStatusDto(
    bool IsFrozen,
    AdPlatform? Platform,
    string? ActiveKillSwitchKey,
    DateTime CheckedAtUtc);
