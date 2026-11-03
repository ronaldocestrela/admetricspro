using Microsoft.EntityFrameworkCore;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Domain.Posts;

namespace SocialPublishing.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementação EF Core de <see cref="IOrganicPostRepository"/>.
/// </summary>
public sealed class OrganicPostRepository : IOrganicPostRepository
{
    private readonly SocialPublishingDbContextAccessor _contextAccessor;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="OrganicPostRepository"/>.
    /// </summary>
    /// <param name="contextAccessor">Acessor scoped do contexto social.</param>
    public OrganicPostRepository(SocialPublishingDbContextAccessor contextAccessor)
    {
        _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
    }

    /// <inheritdoc />
    public async Task<OrganicPost?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var contextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsFailure)
        {
            return null;
        }

        return await contextResult.Value.OrganicPosts
            .AsNoTracking()
            .Include("_media")
            .Include("_scheduleRevisions")
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<OrganicPost>> ListByWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var contextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsFailure)
        {
            return Array.Empty<OrganicPost>();
        }

        return await contextResult.Value.OrganicPosts
            .AsNoTracking()
            .Include("_media")
            .Where(p => p.WorkspaceId == workspaceId)
            .OrderBy(p => p.ScheduledAtUtc)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<OrganicPost>> ListByWorkspaceScheduledRangeAsync(
        Guid workspaceId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var contextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsFailure)
        {
            return Array.Empty<OrganicPost>();
        }

        return await contextResult.Value.OrganicPosts
            .AsNoTracking()
            .Include("_media")
            .Where(p => p.WorkspaceId == workspaceId &&
                        p.ScheduledAtUtc >= fromUtc &&
                        p.ScheduledAtUtc <= toUtc)
            .OrderBy(p => p.ScheduledAtUtc)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(OrganicPost post, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var contextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsFailure)
        {
            throw new InvalidOperationException(contextResult.Error.Code);
        }

        await contextResult.Value.OrganicPosts.AddAsync(post, cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(OrganicPost post, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var contextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsFailure)
        {
            throw new InvalidOperationException(contextResult.Error.Code);
        }

        contextResult.Value.OrganicPosts.Update(post);
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<int> CountByWorkspaceAndStatusAsync(
        Guid workspaceId,
        OrganicPostStatus status,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var contextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsFailure)
        {
            throw new InvalidOperationException(contextResult.Error.Code);
        }

        return await contextResult.Value.OrganicPosts
            .AsNoTracking()
            .CountAsync(p => p.WorkspaceId == workspaceId && p.Status == status, cancellationToken);
    }
}
