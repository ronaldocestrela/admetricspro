using BuildingBlocks.Domain.Automations.SafetyGuards;

namespace Automations.Domain.SafetyGuards;

/// <summary>
/// Resultado da execução da trava de verificação de integridade de Landing Pages (Detector 404/500).
/// </summary>
/// <param name="WorkspaceId">Identificador do workspace verificado.</param>
/// <param name="EvaluatedAdsCount">Total de anúncios ativos com URL de destino avaliados.</param>
/// <param name="HealthyAdsCount">Total de anúncios com páginas íntegras (HTTP 2xx/3xx).</param>
/// <param name="BrokenAdsCount">Total de anúncios com links quebrados (HTTP 4xx, 5xx ou inacessíveis).</param>
/// <param name="PausedAdsCount">Quantidade de anúncios pausados com sucesso via comando in-memory.</param>
/// <param name="Incidents">Coleção de incidentes gerados e registrados para auditoria.</param>
public sealed record LandingPageCheckResult(
    Guid WorkspaceId,
    int EvaluatedAdsCount,
    int HealthyAdsCount,
    int BrokenAdsCount,
    int PausedAdsCount,
    IReadOnlyList<SafetyGuardIncident> Incidents);
