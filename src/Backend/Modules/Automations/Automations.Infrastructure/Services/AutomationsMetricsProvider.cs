using Automations.Application.Rules.Services;
using Automations.Domain.Services;
using BuildingBlocks.Domain.Automations;
using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Automations.Infrastructure.Services;

/// <summary>
/// Implementação concreta de <see cref="IAutomationsMetricsProvider"/> que consulta as métricas do inquilino
/// e consolida fotografias de desempenho em memória.
/// </summary>
public sealed class AutomationsMetricsProvider : IAutomationsMetricsProvider
{
    private readonly ITenantDbContextAccessor _contextAccessor;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="AutomationsMetricsProvider"/>.
    /// </summary>
    public AutomationsMetricsProvider(ITenantDbContextAccessor contextAccessor)
    {
        _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
    }

    /// <inheritdoc />
    public async Task<RuleEvaluationContext> LoadMetricsContextAsync(Guid workspaceId, CancellationToken cancellationToken = default)
    {
        var context = new RuleEvaluationContext(workspaceId);

        var dbContextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (dbContextResult.IsFailure)
        {
            return context;
        }

        var db = dbContextResult.Value;
        var now = DateTime.UtcNow;
        var cutoff = now.AddDays(-7);

        var metrics = await db.CampaignMetrics
            .Where(m => m.WorkspaceId == workspaceId && m.Date >= cutoff)
            .ToListAsync(cancellationToken);

        if (metrics.Count == 0)
        {
            return context;
        }

        var windows = new[] { 24, 48, 72, 168 };

        foreach (var window in windows)
        {
            var windowCutoff = now.AddHours(-window);
            var windowMetrics = metrics.Where(m => m.Date >= windowCutoff.Date).ToList();

            // 1. Agrupamento por Campanha
            var campaignGroups = windowMetrics.GroupBy(m => new { m.Platform, m.CampaignId });
            foreach (var cg in campaignGroups)
            {
                var spend = cg.Sum(m => m.Spend);
                var impressions = cg.Sum(m => m.Impressions);
                var clicks = cg.Sum(m => m.Clicks);
                var conversions = cg.Sum(m => m.Conversions);
                var convValue = cg.Sum(m => m.ConversionValue);

                context.AddSnapshot(new PerformanceMetricSnapshot(
                    cg.Key.Platform,
                    RuleScope.Campaign,
                    cg.Key.CampaignId,
                    window,
                    spend,
                    impressions,
                    clicks,
                    conversions,
                    convValue));
            }

            // 2. Agrupamento por Anúncio (quando AdId presente)
            var adGroups = windowMetrics.Where(m => m.AdId.HasValue).GroupBy(m => new { m.Platform, AdId = m.AdId!.Value });
            foreach (var ag in adGroups)
            {
                var spend = ag.Sum(m => m.Spend);
                var impressions = ag.Sum(m => m.Impressions);
                var clicks = ag.Sum(m => m.Clicks);
                var conversions = ag.Sum(m => m.Conversions);
                var convValue = ag.Sum(m => m.ConversionValue);

                context.AddSnapshot(new PerformanceMetricSnapshot(
                    ag.Key.Platform,
                    RuleScope.Ad,
                    ag.Key.AdId,
                    window,
                    spend,
                    impressions,
                    clicks,
                    conversions,
                    convValue));
            }

            // 3. Agrupamento por Plataforma (Workspace)
            var platformGroups = windowMetrics.GroupBy(m => m.Platform);
            foreach (var pg in platformGroups)
            {
                var spend = pg.Sum(m => m.Spend);
                var impressions = pg.Sum(m => m.Impressions);
                var clicks = pg.Sum(m => m.Clicks);
                var conversions = pg.Sum(m => m.Conversions);
                var convValue = pg.Sum(m => m.ConversionValue);

                context.AddSnapshot(new PerformanceMetricSnapshot(
                    pg.Key,
                    RuleScope.Workspace,
                    null,
                    window,
                    spend,
                    impressions,
                    clicks,
                    conversions,
                    convValue));
            }
        }

        return context;
    }
}
