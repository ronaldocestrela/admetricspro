using Microsoft.EntityFrameworkCore;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Domain.Brand;

namespace SocialPublishing.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementação EF Core de <see cref="IBrandVoiceRepository"/>.
/// </summary>
public sealed class BrandVoiceRepository : IBrandVoiceRepository
{
    private readonly SocialPublishingDbContextAccessor _contextAccessor;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="BrandVoiceRepository"/>.
    /// </summary>
    /// <param name="contextAccessor">Acessor scoped do contexto social.</param>
    public BrandVoiceRepository(SocialPublishingDbContextAccessor contextAccessor)
    {
        _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
    }

    /// <inheritdoc />
    public async Task<BrandVoiceProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var contextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsFailure)
        {
            return null;
        }

        return await contextResult.Value.BrandVoiceProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<BrandVoiceProfile>> ListByWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var contextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsFailure)
        {
            return Array.Empty<BrandVoiceProfile>();
        }

        return await contextResult.Value.BrandVoiceProfiles
            .AsNoTracking()
            .Where(p => p.WorkspaceId == workspaceId)
            .OrderBy(p => p.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(BrandVoiceProfile profile, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var contextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsFailure)
        {
            throw new InvalidOperationException(contextResult.Error.Code);
        }

        await contextResult.Value.BrandVoiceProfiles.AddAsync(profile, cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(BrandVoiceProfile profile, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var contextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsFailure)
        {
            throw new InvalidOperationException(contextResult.Error.Code);
        }

        contextResult.Value.BrandVoiceProfiles.Update(profile);
        await Task.CompletedTask;
    }
}
