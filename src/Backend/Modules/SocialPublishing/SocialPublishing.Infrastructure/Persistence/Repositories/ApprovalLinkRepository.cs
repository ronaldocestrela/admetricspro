using Microsoft.EntityFrameworkCore;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Domain.Approvals;

namespace SocialPublishing.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositório EF Core de <see cref="ApprovalLink"/>.
/// </summary>
public sealed class ApprovalLinkRepository : IApprovalLinkRepository
{
    private readonly SocialPublishingDbContextAccessor _contextAccessor;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="ApprovalLinkRepository"/>.
    /// </summary>
    /// <param name="contextAccessor">Acessor do contexto scoped do tenant.</param>
    public ApprovalLinkRepository(SocialPublishingDbContextAccessor contextAccessor)
    {
        _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
    }

    /// <inheritdoc />
    public async Task<ApprovalLink?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        var contextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsFailure)
        {
            return null;
        }

        return await contextResult.Value.ApprovalLinks
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.TokenHash == tokenHash, cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(ApprovalLink link, CancellationToken cancellationToken = default)
    {
        var contextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsFailure)
        {
            throw new InvalidOperationException(contextResult.Error.Code);
        }

        await contextResult.Value.ApprovalLinks.AddAsync(link, cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(ApprovalLink link, CancellationToken cancellationToken = default)
    {
        var contextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsFailure)
        {
            throw new InvalidOperationException(contextResult.Error.Code);
        }

        contextResult.Value.ApprovalLinks.Update(link);
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<int> RevokeActiveByWorkspaceAsync(
        Guid workspaceId,
        DateTime revokedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var contextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsFailure)
        {
            throw new InvalidOperationException(contextResult.Error.Code);
        }

        var activeLinks = await contextResult.Value.ApprovalLinks
            .Where(l => l.WorkspaceId == workspaceId && l.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var link in activeLinks)
        {
            link.Revoke(revokedAtUtc);
        }

        return activeLinks.Count;
    }
}
