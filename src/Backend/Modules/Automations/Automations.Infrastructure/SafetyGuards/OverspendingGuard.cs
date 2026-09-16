using Automations.Domain.SafetyGuards;
using BuildingBlocks.Application.Campaigns.Commands;
using BuildingBlocks.Domain.Automations.SafetyGuards;
using BuildingBlocks.Domain.Campaigns;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Automations.Infrastructure.SafetyGuards;

/// <summary>
/// Implementação concreta do serviço de proteção contra estouro orçamentário diário (OverspendingGuard).
/// Avalia as campanhas ativas contra o limiar de 120% da verba programada e executa pausas automáticas preventivas.
/// </summary>
public sealed class OverspendingGuard : IOverspendingGuard
{
    private readonly ITenantDbContextAccessor _contextAccessor;
    private readonly ISender _sender;
    private readonly ISecurityAlertNotifier _alertNotifier;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="OverspendingGuard"/>.
    /// </summary>
    /// <param name="contextAccessor">Acessor do banco dedicado do inquilino (TenantDbContext).</param>
    /// <param name="sender">Mediador in-memory MediatR para envio de comandos desacoplados.</param>
    /// <param name="alertNotifier">Despachador de notificações e alarmes multi-canal.</param>
    public OverspendingGuard(
        ITenantDbContextAccessor contextAccessor,
        ISender sender,
        ISecurityAlertNotifier alertNotifier)
    {
        _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
        _alertNotifier = alertNotifier ?? throw new ArgumentNullException(nameof(alertNotifier));
    }

    /// <inheritdoc />
    public async Task<Result<OverspendingCheckResult>> CheckAndMitigateOverspendingAsync(
        Guid workspaceId,
        decimal thresholdMultiplier = IOverspendingGuard.DefaultOverspendingThreshold,
        CancellationToken cancellationToken = default)
    {
        if (workspaceId == Guid.Empty)
        {
            return Result<OverspendingCheckResult>.Failure(
                Error.Validation("OverspendingGuard.InvalidWorkspace", "O identificador do workspace é obrigatório."));
        }

        var dbContextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (dbContextResult.IsFailure)
        {
            return Result<OverspendingCheckResult>.Failure(dbContextResult.Error);
        }

        var db = dbContextResult.Value;

        // 1. Carregar campanhas ativas que possuam orçamento diário positivo
        var activeCampaigns = await db.Campaigns
            .Where(c => c.WorkspaceId == workspaceId &&
                        c.Status == CampaignStatus.Active &&
                        c.DailyBudget.HasValue &&
                        c.DailyBudget.Value > 0)
            .ToListAsync(cancellationToken);

        if (activeCampaigns.Count == 0)
        {
            return Result<OverspendingCheckResult>.Success(new OverspendingCheckResult(
                workspaceId,
                EvaluatedCampaignsCount: 0,
                ViolatedCampaignsCount: 0,
                PausedCampaignsCount: 0,
                Incidents: Array.Empty<SafetyGuardIncident>()));
        }

        var campaignIds = activeCampaigns.Select(c => c.Id).ToList();
        var today = DateTime.UtcNow.Date;

        // 2. Consolidar o investimento do dia corrente
        var todayMetrics = await db.CampaignMetrics
            .Where(m => m.WorkspaceId == workspaceId &&
                        m.Date >= today &&
                        campaignIds.Contains(m.CampaignId))
            .ToListAsync(cancellationToken);

        var spendByCampaign = todayMetrics
            .GroupBy(m => m.CampaignId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Spend));

        var evaluatedCount = activeCampaigns.Count;
        var violatedCount = 0;
        var pausedCount = 0;
        var incidents = new List<SafetyGuardIncident>();

        foreach (var campaign in activeCampaigns)
        {
            spendByCampaign.TryGetValue(campaign.Id, out var currentSpend);
            var dailyBudget = campaign.DailyBudget!.Value;
            var maxAllowedSpend = dailyBudget * thresholdMultiplier;

            if (currentSpend > maxAllowedSpend)
            {
                violatedCount++;
                var percentage = dailyBudget > 0 ? (currentSpend / dailyBudget) * 100m : 0m;
                var reason = $"Trava de Segurança: Gasto diário de R$ {currentSpend:F2} superou 120% do orçamento programado de R$ {dailyBudget:F2} ({percentage:F1}% atingido).";

                // 3. Executar pausa imediata via MediatR
                var pauseCommand = new PauseCampaignCommand(workspaceId, campaign.Id, reason);
                var pauseResult = await _sender.Send(pauseCommand, cancellationToken);
                if (pauseResult.IsSuccess)
                {
                    pausedCount++;
                }

                // 4. Registrar incidente de segurança
                var incidentResult = SafetyGuardIncident.CreateOverspendingIncident(
                    workspaceId,
                    campaign.Name,
                    campaign.Id,
                    campaign.Platform,
                    currentSpend,
                    dailyBudget,
                    pauseResult.IsSuccess ? "Campanha Pausada Preventivamente" : "Falha ao Pausar Campanha");

                if (incidentResult.IsSuccess)
                {
                    var incident = incidentResult.Value;
                    incidents.Add(incident);
                    db.SafetyGuardIncidents.Add(incident);

                    // 5. Despachar alarme multi-canal
                    var alertPayload = new SafetyAlertPayload(
                        IncidentId: incident.Id,
                        WorkspaceId: workspaceId,
                        GuardType: SafetyGuardType.Overspending,
                        Severity: SafetyAlertSeverity.Critical,
                        Title: $"🚨 ALERTA CRÍTICO: Overspending Detectado na Campanha '{campaign.Name}'",
                        Message: reason,
                        TargetEntityName: campaign.Name,
                        TargetEntityId: campaign.Id,
                        Platform: campaign.Platform,
                        ActionTaken: incident.ActionTaken,
                        MetricsContext: new Dictionary<string, string>
                        {
                            { "CurrentSpend", currentSpend.ToString("F2") },
                            { "DailyBudget", dailyBudget.ToString("F2") },
                            { "Percentage", $"{percentage:F1}%" },
                            { "Platform", campaign.Platform }
                        },
                        TimestampUtc: DateTime.UtcNow);

                    await _alertNotifier.DispatchAlertAsync(alertPayload, cancellationToken);
                }
            }
        }

        if (incidents.Count > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        return Result<OverspendingCheckResult>.Success(new OverspendingCheckResult(
            workspaceId,
            evaluatedCount,
            violatedCount,
            pausedCount,
            incidents));
    }
}
