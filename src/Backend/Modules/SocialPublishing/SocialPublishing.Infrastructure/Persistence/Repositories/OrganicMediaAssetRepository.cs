using Microsoft.EntityFrameworkCore;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Application.Media;
using SocialPublishing.Domain.Posts;

namespace SocialPublishing.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementação EF Core de <see cref="IOrganicMediaAssetRepository"/>.
/// </summary>
public sealed class OrganicMediaAssetRepository : IOrganicMediaAssetRepository
{
    private readonly SocialPublishingDbContextAccessor _contextAccessor;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="OrganicMediaAssetRepository"/>.
    /// </summary>
    /// <param name="contextAccessor">Acessor scoped do contexto social.</param>
    public OrganicMediaAssetRepository(SocialPublishingDbContextAccessor contextAccessor)
    {
        _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
    }

    /// <inheritdoc />
    public async Task<OrganicMediaAsset?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var contextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsFailure)
        {
            return null;
        }

        return await contextResult.Value.OrganicMediaAssets
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(OrganicMediaAsset asset, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var contextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsFailure)
        {
            return;
        }

        await contextResult.Value.OrganicMediaAssets.AddAsync(asset, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<OrganicMediaCatalogEntry>> ListObjectKeysForMigrationAuditAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var contextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsFailure)
        {
            return Array.Empty<OrganicMediaCatalogEntry>();
        }

        return await contextResult.Value.OrganicMediaAssets
            .AsNoTracking()
            .Where(a => a.ObjectKey != null && a.ObjectKey != string.Empty)
            .Select(a => new OrganicMediaCatalogEntry(a.ObjectKey!, a.MimeType))
            .ToListAsync(cancellationToken);
    }
}
