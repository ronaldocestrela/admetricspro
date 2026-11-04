using BuildingBlocks.Domain.Integrations;
using BuildingBlocks.Infrastructure.Persistence;
using Integrations.Application.Boost.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Integrations.Infrastructure.Boost.Persistence;

/// <summary>
/// Implementação EF Core de <see cref="IPaidBoostDraftRepository"/> no banco operacional do tenant.
/// </summary>
public sealed class PaidBoostDraftRepository : IPaidBoostDraftRepository
{
    private readonly ITenantDbContextAccessor _contextAccessor;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="PaidBoostDraftRepository"/>.
    /// </summary>
    /// <param name="contextAccessor">Acessor do contexto do tenant.</param>
    public PaidBoostDraftRepository(ITenantDbContextAccessor contextAccessor)
    {
        _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
    }

    /// <inheritdoc />
    public async Task<PaidBoostDraft?> GetBySuggestionIdAsync(
        Guid workspaceId,
        Guid suggestionId,
        CancellationToken cancellationToken = default)
    {
        var contextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsFailure)
        {
            return null;
        }

        return await contextResult.Value.PaidBoostDrafts
            .AsNoTracking()
            .FirstOrDefaultAsync(
                d => d.WorkspaceId == workspaceId && d.SuggestionId == suggestionId,
                cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(PaidBoostDraft draft, CancellationToken cancellationToken = default)
    {
        var contextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsFailure)
        {
            throw new InvalidOperationException(contextResult.Error.Code);
        }

        await contextResult.Value.PaidBoostDrafts.AddAsync(draft, cancellationToken);
    }
}
