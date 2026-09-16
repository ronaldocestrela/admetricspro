using BuildingBlocks.Domain.Automations.SafetyGuards;

namespace Automations.Application.SafetyGuards.DTOs;

/// <summary>
/// DTO que representa um incidente registrado por travas de segurança operacional.
/// </summary>
/// <param name="Id">Identificador único do incidente.</param>
/// <param name="WorkspaceId">Identificador do workspace associado.</param>
/// <param name="GuardType">Tipo da trava acionada (Overspending ou BrokenLandingPage).</param>
/// <param name="Severity">Severidade do incidente.</param>
/// <param name="Status">Status do incidente (Detected, Mitigated, Acknowledged, Resolved).</param>
/// <param name="TargetEntityName">Nome da campanha ou anúncio.</param>
/// <param name="TargetEntityId">Identificador da entidade.</param>
/// <param name="Platform">Plataforma de anúncios.</param>
/// <param name="ActionTaken">Ação de mitigação executada.</param>
/// <param name="Reason">Motivo descritivo.</param>
/// <param name="CurrentSpend">Gasto acumulado no momento da trava.</param>
/// <param name="DailyBudget">Orçamento diário programado.</param>
/// <param name="HttpStatusCode">Código de status HTTP.</param>
/// <param name="TargetUrl">URL de destino verificada.</param>
/// <param name="DetectedAtUtc">Data e hora UTC de detecção.</param>
/// <param name="ResolvedAtUtc">Data e hora UTC de resolução.</param>
public sealed record SafetyIncidentDto(
    Guid Id,
    Guid WorkspaceId,
    SafetyGuardType GuardType,
    SafetyAlertSeverity Severity,
    SafetyIncidentStatus Status,
    string TargetEntityName,
    Guid TargetEntityId,
    string Platform,
    string ActionTaken,
    string Reason,
    decimal? CurrentSpend,
    decimal? DailyBudget,
    int? HttpStatusCode,
    string? TargetUrl,
    DateTime DetectedAtUtc,
    DateTime? ResolvedAtUtc)
{
    /// <summary>
    /// Mapeia a entidade de domínio para o DTO de apresentação.
    /// </summary>
    /// <param name="entity">Entidade SafetyGuardIncident.</param>
    /// <returns>Instância do DTO.</returns>
    public static SafetyIncidentDto FromEntity(SafetyGuardIncident entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new SafetyIncidentDto(
            entity.Id,
            entity.WorkspaceId,
            entity.GuardType,
            entity.Severity,
            entity.Status,
            entity.TargetEntityName,
            entity.TargetEntityId,
            entity.Platform,
            entity.ActionTaken,
            entity.Reason,
            entity.CurrentSpend,
            entity.DailyBudget,
            entity.HttpStatusCode,
            entity.TargetUrl,
            entity.DetectedAtUtc,
            entity.ResolvedAtUtc);
    }
}

/// <summary>
/// DTO de resposta da execução da trava de Overspending.
/// </summary>
/// <param name="WorkspaceId">Identificador do workspace verificado.</param>
/// <param name="EvaluatedCampaignsCount">Campanhas ativas avaliadas.</param>
/// <param name="ViolatedCampaignsCount">Campanhas que superaram o limiar de 120%.</param>
/// <param name="PausedCampaignsCount">Campanhas pausadas preventivamente.</param>
/// <param name="Incidents">Incidentes registrados.</param>
public sealed record CheckOverspendingResultDto(
    Guid WorkspaceId,
    int EvaluatedCampaignsCount,
    int ViolatedCampaignsCount,
    int PausedCampaignsCount,
    IReadOnlyList<SafetyIncidentDto> Incidents);

/// <summary>
/// DTO de resposta da execução da trava de Landing Pages (Detector 404/500).
/// </summary>
/// <param name="WorkspaceId">Identificador do workspace verificado.</param>
/// <param name="EvaluatedAdsCount">Anúncios ativos avaliados.</param>
/// <param name="HealthyAdsCount">Anúncios com links íntegros.</param>
/// <param name="BrokenAdsCount">Anúncios com links quebrados ou inacessíveis.</param>
/// <param name="PausedAdsCount">Anúncios pausados preventivamente.</param>
/// <param name="Incidents">Incidentes registrados.</param>
public sealed record CheckLandingPagesResultDto(
    Guid WorkspaceId,
    int EvaluatedAdsCount,
    int HealthyAdsCount,
    int BrokenAdsCount,
    int PausedAdsCount,
    IReadOnlyList<SafetyIncidentDto> Incidents);
