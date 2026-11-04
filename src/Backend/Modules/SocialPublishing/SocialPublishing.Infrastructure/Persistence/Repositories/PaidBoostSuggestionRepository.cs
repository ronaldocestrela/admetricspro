using Microsoft.EntityFrameworkCore;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Domain.Traction;

namespace SocialPublishing.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementação EF Core de <see cref="IPaidBoostSuggestionRepository"/>.
/// </summary>
public sealed class PaidBoostSuggestionRepository : IPaidBoostSuggestionRepository
{
    private readonly SocialPublishingDbContextAccessor _contextAccessor;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="PaidBoostSuggestionRepository"/>.
    /// </summary>
    /// <param name="contextAccessor">Acessor scoped do contexto social.</param>
    public PaidBoostSuggestionRepository(SocialPublishingDbContextAccessor contextAccessor)
    {
        _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
    }

    /// <inheritdoc />
    public async Task AddAsync(PaidBoostSuggestion suggestion, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var contextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsFailure)
        {
            throw new InvalidOperationException(contextResult.Error.Code);
        }

        await contextResult.Value.PaidBoostSuggestions.AddAsync(suggestion, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlySet<Guid>> GetOpenOrganicPostIdsAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var contextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsFailure)
        {
            return new HashSet<Guid>();
        }

        var ids = await contextResult.Value.PaidBoostSuggestions
            .AsNoTracking()
            .Where(s => s.WorkspaceId == workspaceId && s.Status == PaidBoostSuggestionStatus.Open)
            .Select(s => s.OrganicPostId)
            .ToListAsync(cancellationToken);

        return ids.ToHashSet();
    }
}
