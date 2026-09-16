using Analytics.Domain.Copilot;
using BuildingBlocks.Domain.Campaigns;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace Analytics.Infrastructure.Copilot;

/// <summary>
/// Provedor de dados operacionais e executor de ações do Copiloto de IA sobre o banco dedicado do tenant.
/// </summary>
public sealed class CopilotDataProvider : ICopilotDataProvider
{
    private readonly ITenantDbContextAccessor _contextAccessor;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="CopilotDataProvider"/>.
    /// </summary>
    /// <param name="contextAccessor">Acessor do DbContext dedicado do inquilino contextual.</param>
    public CopilotDataProvider(ITenantDbContextAccessor contextAccessor)
    {
        _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<AdSetAudienceTargeting>>> GetMetaAdSetsForAuditAsync(
        Guid workspaceId,
        DateTime startDateUtc,
        DateTime endDateUtc,
        CancellationToken cancellationToken = default)
    {
        var dbContextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (dbContextResult.IsFailure)
        {
            return Result<IReadOnlyList<AdSetAudienceTargeting>>.Failure(dbContextResult.Error);
        }

        var db = dbContextResult.Value;

        // Obter campanhas do Meta Ads no workspace
        var metaCampaigns = await db.Campaigns
            .AsNoTracking()
            .Where(c => c.WorkspaceId == workspaceId && c.Platform == "MetaAds")
            .Include(c => c.AdSets)
            .ToListAsync(cancellationToken);

        if (metaCampaigns.Count == 0)
        {
            return Result<IReadOnlyList<AdSetAudienceTargeting>>.Success(Array.Empty<AdSetAudienceTargeting>());
        }

        var allAdSets = metaCampaigns.SelectMany(c => c.AdSets.Select(a => new { Campaign = c, AdSet = a })).ToList();
        var adSetIds = allAdSets.Select(x => x.AdSet.Id).ToList();

        // Obter métricas consolidadas do período para cada conjunto
        var start = startDateUtc.Date;
        var end = endDateUtc.Date;

        var metrics = await db.CampaignMetrics
            .AsNoTracking()
            .Where(m => m.WorkspaceId == workspaceId &&
                        m.AdSetId.HasValue &&
                        adSetIds.Contains(m.AdSetId.Value) &&
                        m.Date >= start &&
                        m.Date <= end)
            .GroupBy(m => m.AdSetId!.Value)
            .Select(g => new
            {
                AdSetId = g.Key,
                Spend = g.Sum(m => m.Spend),
                Impressions = g.Sum(m => m.Impressions),
                Clicks = g.Sum(m => m.Clicks),
                Conversions = g.Sum(m => m.Conversions)
            })
            .ToDictionaryAsync(x => x.AdSetId, cancellationToken);

        var resultList = new List<AdSetAudienceTargeting>();

        foreach (var item in allAdSets)
        {
            metrics.TryGetValue(item.AdSet.Id, out var m);

            var spend = m?.Spend ?? 0m;
            var impressions = m?.Impressions ?? 0;
            var conversions = m?.Conversions ?? 0m;

            var cpm = impressions > 0 ? Math.Round((spend / impressions) * 1000m, 2) : 0m;
            var cpa = conversions > 0 ? Math.Round(spend / conversions, 2) : 0m;

            var tags = ExtractTargetingTags(item.AdSet.TargetingSummary, item.AdSet.Name);

            resultList.Add(new AdSetAudienceTargeting(
                adSetId: item.AdSet.Id,
                adSetName: item.AdSet.Name,
                campaignId: item.Campaign.Id,
                campaignName: item.Campaign.Name,
                status: item.AdSet.Status.ToString(),
                spend: spend,
                impressions: impressions,
                cpm: cpm,
                cpa: cpa,
                targetingTags: tags
            ));
        }

        return Result<IReadOnlyList<AdSetAudienceTargeting>>.Success(resultList);
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<SearchKeywordPerformance>>> GetSearchKeywordsForAuditAsync(
        Guid workspaceId,
        DateTime startDateUtc,
        DateTime endDateUtc,
        CancellationToken cancellationToken = default)
    {
        var dbContextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (dbContextResult.IsFailure)
        {
            return Result<IReadOnlyList<SearchKeywordPerformance>>.Failure(dbContextResult.Error);
        }

        var db = dbContextResult.Value;

        // Obter campanhas de Search (GoogleAds e BingAds)
        var searchCampaigns = await db.Campaigns
            .AsNoTracking()
            .Where(c => c.WorkspaceId == workspaceId && (c.Platform == "GoogleAds" || c.Platform == "BingAds"))
            .Include(c => c.AdSets)
            .ToListAsync(cancellationToken);

        if (searchCampaigns.Count == 0)
        {
            return Result<IReadOnlyList<SearchKeywordPerformance>>.Success(Array.Empty<SearchKeywordPerformance>());
        }

        var allAdSets = searchCampaigns.SelectMany(c => c.AdSets.Select(a => new { Campaign = c, AdSet = a })).ToList();
        var adSetIds = allAdSets.Select(x => x.AdSet.Id).ToList();

        var start = startDateUtc.Date;
        var end = endDateUtc.Date;

        var metrics = await db.CampaignMetrics
            .AsNoTracking()
            .Where(m => m.WorkspaceId == workspaceId &&
                        m.AdSetId.HasValue &&
                        adSetIds.Contains(m.AdSetId.Value) &&
                        m.Date >= start &&
                        m.Date <= end)
            .GroupBy(m => m.AdSetId!.Value)
            .Select(g => new
            {
                AdSetId = g.Key,
                Spend = g.Sum(m => m.Spend),
                Clicks = g.Sum(m => m.Clicks),
                Conversions = g.Sum(m => m.Conversions)
            })
            .ToDictionaryAsync(x => x.AdSetId, cancellationToken);

        var resultList = new List<SearchKeywordPerformance>();

        foreach (var item in allAdSets)
        {
            metrics.TryGetValue(item.AdSet.Id, out var m);

            var spend = m?.Spend ?? 0m;
            var clicks = m?.Clicks ?? 0;
            var conversions = m?.Conversions ?? 0m;

            var cpc = clicks > 0 ? Math.Round(spend / clicks, 2) : 0m;
            var cpa = conversions > 0 ? Math.Round(spend / conversions, 2) : 0m;

            var keyword = ExtractSearchKeyword(item.AdSet.TargetingSummary, item.AdSet.Name);

            resultList.Add(new SearchKeywordPerformance(
                platform: item.Campaign.Platform,
                campaignId: item.Campaign.Id,
                campaignName: item.Campaign.Name,
                adGroupId: item.AdSet.Id,
                adGroupName: item.AdSet.Name,
                keyword: keyword,
                matchType: "Phrase",
                spend: spend,
                clicks: clicks,
                conversions: conversions,
                cpc: cpc,
                cpa: cpa
            ));
        }

        return Result<IReadOnlyList<SearchKeywordPerformance>>.Success(resultList);
    }

    /// <inheritdoc />
    public async Task<Result<bool>> ExecuteRecommendationActionAsync(
        Guid workspaceId,
        CopilotRecommendationAction action,
        CancellationToken cancellationToken = default)
    {
        if (action is null)
        {
            return Result<bool>.Failure(Error.Validation("Copilot.NullAction", "A ação de remediação não pode ser nula."));
        }

        var dbContextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (dbContextResult.IsFailure)
        {
            return Result<bool>.Failure(dbContextResult.Error);
        }

        var db = dbContextResult.Value;

        // Se for pausa de conjunto de anúncios
        if (action.ActionType == CopilotActionType.PauseAdSet)
        {
            var adSet = await db.AdSets.FirstOrDefaultAsync(a => a.Id == action.TargetEntityId, cancellationToken);
            if (adSet != null)
            {
                adSet.UpdateDetails(
                    adSet.Name,
                    AdSetStatus.Paused,
                    adSet.BidStrategy,
                    adSet.OptimizationGoal,
                    adSet.DailyBudget,
                    adSet.LifetimeBudget,
                    adSet.TargetingSummary,
                    adSet.StartDateUtc,
                    adSet.EndDateUtc,
                    DateTime.UtcNow);
            }
        }

        // Registrar auditoria imutável no Tenant
        var auditLogResult = TenantAuditLog.Create(
            id: Guid.NewGuid(),
            userId: workspaceId,
            userEmail: "copilot@admetricspro.internal",
            action: $"CopilotAction.{action.ActionType}",
            resource: action.Platform,
            resourceId: action.TargetEntityId.ToString(),
            details: $"Ação executada em 1 clique pelo Copiloto: {action.Title}. Detalhes: {action.Description}",
            ipAddress: "127.0.0.1",
            createdAtUtc: DateTime.UtcNow
        );

        if (auditLogResult.IsSuccess)
        {
            await db.TenantAuditLogs.AddAsync(auditLogResult.Value, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
        }

        return Result<bool>.Success(true);
    }

    private static List<string> ExtractTargetingTags(string? targetingSummary, string adSetName)
    {
        var tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(targetingSummary))
        {
            // Extrai palavras e termos separados por vírgula, barra ou aspas
            var matches = Regex.Matches(targetingSummary, @"[A-Za-zÀ-ÿ0-9\s]{3,}");
            foreach (Match match in matches)
            {
                var val = match.Value.Trim().ToLowerInvariant();
                if (val.Length > 2 && !val.Contains("{") && !val.Contains("}") && !val.Contains("\""))
                {
                    tags.Add(val);
                }
            }
        }

        if (tags.Count == 0 && !string.IsNullOrWhiteSpace(adSetName))
        {
            var words = adSetName.Split(new[] { ' ', '-', '_', '|', '/' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var w in words.Where(x => x.Length >= 4))
            {
                tags.Add(w.Trim().ToLowerInvariant());
            }
        }

        return tags.ToList();
    }

    private static string ExtractSearchKeyword(string? targetingSummary, string adGroupName)
    {
        if (!string.IsNullOrWhiteSpace(targetingSummary))
        {
            var match = Regex.Match(targetingSummary, @"(?:keyword|termo|busca)[\s:=""]+([^"",}]+)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return match.Groups[1].Value.Trim();
            }
        }

        return adGroupName.Trim();
    }
}
