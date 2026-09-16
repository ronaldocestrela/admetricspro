using BuildingBlocks.Domain.Campaigns;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Infrastructure.Persistence;
using Integrations.Domain.Campaigns;
using Microsoft.EntityFrameworkCore;

namespace Integrations.Infrastructure.Campaigns.Persistence;

/// <summary>
/// Repositório operacional de métricas analíticas de campanhas com suporte a persistência atômica,
/// idempotência estrita (evitando duplicações em re-execuções) e consultas filtradas.
/// </summary>
public sealed class CampaignMetricsRepository : ICampaignMetricsRepository
{
    private readonly ITenantDbContextAccessor _contextAccessor;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="CampaignMetricsRepository"/>.
    /// </summary>
    /// <param name="contextAccessor">Acessor de contexto de banco do inquilino.</param>
    public CampaignMetricsRepository(ITenantDbContextAccessor contextAccessor)
    {
        _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
    }

    /// <inheritdoc />
    public async Task<Result<int>> UpsertMetricsBatchAsync(
        Guid workspaceId,
        IReadOnlyList<CampaignMetric> metrics,
        CancellationToken cancellationToken = default)
    {
        if (metrics is null || metrics.Count == 0)
        {
            return Result<int>.Success(0);
        }

        var contextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsFailure)
        {
            return Result<int>.Failure(contextResult.Error);
        }

        var context = contextResult.Value;

        // Extrai o conjunto de datas e contas para carregar registros existentes em lote
        var accountIds = metrics.Select(m => m.ConnectedAdAccountId).Distinct().ToList();
        var minDate = metrics.Min(m => m.Date);
        var maxDate = metrics.Max(m => m.Date);

        var existingMetrics = await context.CampaignMetrics
            .Where(m => m.WorkspaceId == workspaceId &&
                        accountIds.Contains(m.ConnectedAdAccountId) &&
                        m.Date >= minDate && m.Date <= maxDate)
            .ToListAsync(cancellationToken);

        // Mapeia chave composta de unicidade: (AccountId, ExternalCmpId, ExternalAdSetId, ExternalAdId, Date, Hour, Granularity)
        var existingMap = existingMetrics.ToDictionary(
            BuildKey,
            StringComparer.OrdinalIgnoreCase);

        var now = DateTime.UtcNow;
        var processedCount = 0;

        foreach (var incoming in metrics)
        {
            var key = BuildKey(incoming);

            if (existingMap.TryGetValue(key, out var existing))
            {
                // Registro existente: Atualiza idempotentemente com novos números consolidados
                var updateResult = existing.UpdateMetrics(
                    incoming.Spend,
                    incoming.Impressions,
                    incoming.Clicks,
                    incoming.Conversions,
                    incoming.ConversionValue,
                    incoming.SyncedAtUtc != default ? incoming.SyncedAtUtc : now);

                if (updateResult.IsFailure)
                {
                    return Result<int>.Failure(updateResult.Error);
                }
            }
            else
            {
                // Novo registro: Adiciona ao contexto e ao mapa local para prevenir colisões no mesmo lote
                await context.CampaignMetrics.AddAsync(incoming, cancellationToken);
                existingMap[key] = incoming;
            }

            processedCount++;
        }

        await context.SaveChangesAsync(cancellationToken);

        return Result<int>.Success(processedCount);
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<CampaignMetric>>> GetMetricsAsync(
        Guid workspaceId,
        DateTime startDateUtc,
        DateTime endDateUtc,
        MetricGranularity? granularity = null,
        Guid? campaignId = null,
        Guid? connectedAdAccountId = null,
        CancellationToken cancellationToken = default)
    {
        var contextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsFailure)
        {
            return Result<IReadOnlyList<CampaignMetric>>.Failure(contextResult.Error);
        }

        var context = contextResult.Value;

        var normalizedStart = DateTime.SpecifyKind(startDateUtc.Date, DateTimeKind.Utc);
        var normalizedEnd = DateTime.SpecifyKind(endDateUtc.Date, DateTimeKind.Utc);

        var query = context.CampaignMetrics
            .AsNoTracking()
            .Where(m => m.WorkspaceId == workspaceId &&
                        m.Date >= normalizedStart &&
                        m.Date <= normalizedEnd);

        if (granularity.HasValue)
        {
            query = query.Where(m => m.Granularity == granularity.Value);
        }

        if (campaignId.HasValue)
        {
            query = query.Where(m => m.CampaignId == campaignId.Value);
        }

        if (connectedAdAccountId.HasValue)
        {
            query = query.Where(m => m.ConnectedAdAccountId == connectedAdAccountId.Value);
        }

        var list = await query
            .OrderBy(m => m.Date)
            .ThenBy(m => m.Hour)
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<CampaignMetric>>.Success(list);
    }

    private static string BuildKey(CampaignMetric m)
    {
        return $"{m.ConnectedAdAccountId}_{m.ExternalCampaignId}_{m.ExternalAdSetId ?? ""}_{m.ExternalAdId ?? ""}_{m.Date:yyyyMMdd}_{m.Hour}_{m.Granularity}";
    }
}
