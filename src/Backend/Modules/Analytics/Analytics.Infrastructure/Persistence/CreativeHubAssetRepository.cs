using Analytics.Domain.Creatives;
using BuildingBlocks.Domain.Primitives;
using Microsoft.EntityFrameworkCore;

namespace Analytics.Infrastructure.Persistence;

/// <summary>
/// Implementação EF Core de <see cref="ICreativeHubAssetRepository"/>.
/// </summary>
public sealed class CreativeHubAssetRepository : ICreativeHubAssetRepository
{
    private readonly AnalyticsDbContextAccessor _contextAccessor;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="CreativeHubAssetRepository"/>.
    /// </summary>
    /// <param name="contextAccessor">Acessor scoped do contexto analytics.</param>
    public CreativeHubAssetRepository(AnalyticsDbContextAccessor contextAccessor)
    {
        _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> UpsertByFingerprintAsync(
        CreativeHubAssetUpsertRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var contextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsFailure)
        {
            return Result<Guid>.Failure(contextResult.Error);
        }

        var context = contextResult.Value;
        var existing = await context.CreativeHubAssets
            .FirstOrDefaultAsync(
                a => a.WorkspaceId == request.WorkspaceId && a.MediaFingerprint == request.MediaFingerprint,
                cancellationToken);

        if (existing is null)
        {
            var asset = new CreativeHubAsset(
                Guid.NewGuid(),
                request.WorkspaceId,
                request.MediaFingerprint,
                request.OrganicPostId,
                request.Network,
                request.Permalink,
                request.Impressions,
                request.EngagementRate,
                request.BaselineMedianEngagement,
                request.EngagementMultiple,
                request.DetectedAtUtc);

            await context.CreativeHubAssets.AddAsync(asset, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            return Result<Guid>.Success(asset.Id);
        }

        existing.UpdateOrganicTraction(
            request.OrganicPostId,
            request.Network,
            request.Permalink,
            request.Impressions,
            request.EngagementRate,
            request.BaselineMedianEngagement,
            request.EngagementMultiple,
            request.DetectedAtUtc);

        await context.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(existing.Id);
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<CreativeHubAsset>>> ListOrganicByWorkspaceAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var contextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsFailure)
        {
            return Result<IReadOnlyList<CreativeHubAsset>>.Failure(contextResult.Error);
        }

        var context = contextResult.Value;
        var assets = await context.CreativeHubAssets
            .AsNoTracking()
            .Where(a => a.WorkspaceId == workspaceId && a.OrganicPostId != null)
            .OrderByDescending(a => a.DetectedAtUtc)
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<CreativeHubAsset>>.Success(assets);
    }
}
