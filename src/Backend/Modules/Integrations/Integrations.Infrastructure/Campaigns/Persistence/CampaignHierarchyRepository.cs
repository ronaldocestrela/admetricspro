using BuildingBlocks.Domain.Campaigns;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Infrastructure.Persistence;
using Integrations.Domain.Campaigns;
using Integrations.Domain.Campaigns.Models;
using Microsoft.EntityFrameworkCore;

namespace Integrations.Infrastructure.Campaigns.Persistence;

/// <summary>
/// Implementação concreta do repositório de hierarquia de campanhas persistindo no banco do inquilino (TenantDbContext).
/// </summary>
public sealed class CampaignHierarchyRepository : ICampaignHierarchyRepository
{
    private readonly ITenantDbContextAccessor _contextAccessor;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="CampaignHierarchyRepository"/>.
    /// </summary>
    /// <param name="contextAccessor">Acessor de contexto de banco do inquilino.</param>
    public CampaignHierarchyRepository(ITenantDbContextAccessor contextAccessor)
    {
        _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
    }

    /// <inheritdoc />
    public async Task<Result<int>> UpsertHierarchyBatchAsync(
        Guid workspaceId,
        Guid connectedAdAccountId,
        string platform,
        UnifiedCampaignHierarchy hierarchy,
        DateTime syncTimeUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(hierarchy);

        var contextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsFailure)
        {
            return Result<int>.Failure(contextResult.Error);
        }

        var context = contextResult.Value;

        // Carrega dados existentes para a conta conectada com relacionamentos
        var existingCampaigns = await context.Campaigns
            .Include(c => c.AdSets)
            .ThenInclude(s => s.Ads)
            .Where(c => c.ConnectedAdAccountId == connectedAdAccountId)
            .ToListAsync(cancellationToken);

        var existingCampaignsMap = existingCampaigns.ToDictionary(
            c => c.ExternalCampaignId,
            StringComparer.OrdinalIgnoreCase);

        var existingAdSetsMap = existingCampaigns
            .SelectMany(c => c.AdSets)
            .GroupBy(s => s.ExternalAdSetId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var existingAdsMap = existingCampaigns
            .SelectMany(c => c.AdSets)
            .SelectMany(s => s.Ads)
            .GroupBy(a => a.ExternalAdId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var entitiesTouched = 0;

        // 1. Upsert Campanhas
        var currentCampaignsMap = new Dictionary<string, Campaign>(StringComparer.OrdinalIgnoreCase);

        foreach (var cItem in hierarchy.Campaigns)
        {
            if (existingCampaignsMap.TryGetValue(cItem.ExternalCampaignId, out var existingCmp))
            {
                existingCmp.UpdateDetails(
                    cItem.Name,
                    cItem.Status,
                    cItem.Objective,
                    cItem.DailyBudget,
                    cItem.LifetimeBudget,
                    cItem.Currency,
                    cItem.StartDateUtc,
                    cItem.EndDateUtc,
                    syncTimeUtc);

                currentCampaignsMap[cItem.ExternalCampaignId] = existingCmp;
                entitiesTouched++;
            }
            else
            {
                var createResult = Campaign.Create(
                    Guid.NewGuid(),
                    workspaceId,
                    connectedAdAccountId,
                    platform,
                    cItem.ExternalCampaignId,
                    cItem.Name,
                    cItem.Status,
                    cItem.Objective,
                    cItem.DailyBudget,
                    cItem.LifetimeBudget,
                    cItem.Currency,
                    cItem.StartDateUtc,
                    cItem.EndDateUtc,
                    syncTimeUtc);

                if (createResult.IsSuccess)
                {
                    await context.Campaigns.AddAsync(createResult.Value, cancellationToken);
                    currentCampaignsMap[cItem.ExternalCampaignId] = createResult.Value;
                    existingCampaignsMap[cItem.ExternalCampaignId] = createResult.Value;
                    entitiesTouched++;
                }
            }
        }

        // 2. Upsert Conjuntos (AdSets)
        var currentAdSetsMap = new Dictionary<string, AdSet>(StringComparer.OrdinalIgnoreCase);

        foreach (var sItem in hierarchy.AdSets)
        {
            if (!currentCampaignsMap.TryGetValue(sItem.ExternalCampaignId, out var parentCampaign))
            {
                continue;
            }

            if (existingAdSetsMap.TryGetValue(sItem.ExternalAdSetId, out var existingSet))
            {
                existingSet.UpdateDetails(
                    sItem.Name,
                    sItem.Status,
                    sItem.BidStrategy,
                    sItem.OptimizationGoal,
                    sItem.DailyBudget,
                    sItem.LifetimeBudget,
                    sItem.TargetingSummary,
                    sItem.StartDateUtc,
                    sItem.EndDateUtc,
                    syncTimeUtc);

                currentAdSetsMap[sItem.ExternalAdSetId] = existingSet;
                entitiesTouched++;
            }
            else
            {
                var createResult = AdSet.Create(
                    Guid.NewGuid(),
                    parentCampaign.Id,
                    connectedAdAccountId,
                    sItem.ExternalAdSetId,
                    sItem.Name,
                    sItem.Status,
                    sItem.BidStrategy,
                    sItem.OptimizationGoal,
                    sItem.DailyBudget,
                    sItem.LifetimeBudget,
                    sItem.TargetingSummary,
                    sItem.StartDateUtc,
                    sItem.EndDateUtc,
                    syncTimeUtc);

                if (createResult.IsSuccess)
                {
                    await context.AdSets.AddAsync(createResult.Value, cancellationToken);
                    currentAdSetsMap[sItem.ExternalAdSetId] = createResult.Value;
                    existingAdSetsMap[sItem.ExternalAdSetId] = createResult.Value;
                    entitiesTouched++;
                }
            }
        }

        // 3. Upsert Anúncios (Ads)
        foreach (var aItem in hierarchy.Ads)
        {
            if (!currentAdSetsMap.TryGetValue(aItem.ExternalAdSetId, out var parentSet))
            {
                continue;
            }

            if (existingAdsMap.TryGetValue(aItem.ExternalAdId, out var existingAd))
            {
                existingAd.UpdateDetails(
                    aItem.Name,
                    aItem.Status,
                    aItem.CreativeType,
                    aItem.Headline,
                    aItem.Body,
                    aItem.DestinationUrl,
                    aItem.PreviewUrl,
                    aItem.CallToAction,
                    syncTimeUtc);

                entitiesTouched++;
            }
            else
            {
                var createResult = Ad.Create(
                    Guid.NewGuid(),
                    parentSet.Id,
                    parentSet.CampaignId,
                    connectedAdAccountId,
                    aItem.ExternalAdId,
                    aItem.Name,
                    aItem.Status,
                    aItem.CreativeType,
                    aItem.Headline,
                    aItem.Body,
                    aItem.DestinationUrl,
                    aItem.PreviewUrl,
                    aItem.CallToAction,
                    syncTimeUtc);

                if (createResult.IsSuccess)
                {
                    await context.Ads.AddAsync(createResult.Value, cancellationToken);
                    existingAdsMap[aItem.ExternalAdId] = createResult.Value;
                    entitiesTouched++;
                }
            }
        }

        return Result<int>.Success(entitiesTouched);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Campaign>> GetCampaignsByWorkspaceAsync(
        Guid workspaceId,
        Guid? connectedAdAccountId = null,
        string? platform = null,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        var contextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsFailure)
        {
            return Array.Empty<Campaign>();
        }

        var query = contextResult.Value.Campaigns
            .Include(c => c.AdSets)
            .ThenInclude(s => s.Ads)
            .Where(c => c.WorkspaceId == workspaceId);

        if (connectedAdAccountId.HasValue && connectedAdAccountId.Value != Guid.Empty)
        {
            query = query.Where(c => c.ConnectedAdAccountId == connectedAdAccountId.Value);
        }

        if (!string.IsNullOrWhiteSpace(platform))
        {
            query = query.Where(c => c.Platform == platform);
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<CampaignStatus>(status, true, out var parsedStatus))
        {
            query = query.Where(c => c.Status == parsedStatus);
        }

        return await query
            .OrderByDescending(c => c.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Campaign?> GetCampaignWithHierarchyByIdAsync(
        Guid campaignId,
        CancellationToken cancellationToken = default)
    {
        var contextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsFailure)
        {
            return null;
        }

        return await contextResult.Value.Campaigns
            .Include(c => c.AdSets)
            .ThenInclude(s => s.Ads)
            .FirstOrDefaultAsync(c => c.Id == campaignId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<BuildingBlocks.Domain.Tenants.ConnectedAdAccount>> GetAccountsForSyncAsync(
        Guid workspaceId,
        Guid? connectedAdAccountId = null,
        string? platform = null,
        CancellationToken cancellationToken = default)
    {
        var contextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsFailure)
        {
            return Array.Empty<BuildingBlocks.Domain.Tenants.ConnectedAdAccount>();
        }

        var query = contextResult.Value.ConnectedAdAccounts
            .Where(a => a.WorkspaceId == workspaceId);

        if (connectedAdAccountId.HasValue && connectedAdAccountId.Value != Guid.Empty)
        {
            query = query.Where(a => a.Id == connectedAdAccountId.Value);
        }

        if (!string.IsNullOrWhiteSpace(platform))
        {
            query = query.Where(a => a.Platform == platform);
        }

        return await query.ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Campaign?> GetCampaignByIdAsync(
        Guid campaignId,
        CancellationToken cancellationToken = default)
    {
        var contextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsFailure)
        {
            return null;
        }

        return await contextResult.Value.Campaigns
            .FirstOrDefaultAsync(c => c.Id == campaignId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Ad?> GetAdByIdAsync(
        Guid adId,
        CancellationToken cancellationToken = default)
    {
        var contextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsFailure)
        {
            return null;
        }

        return await contextResult.Value.Ads
            .FirstOrDefaultAsync(a => a.Id == adId, cancellationToken);
    }
}
