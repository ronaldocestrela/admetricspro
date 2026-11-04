using BuildingBlocks.Domain.Integrations;

namespace Integrations.Application.Boost.Persistence;

/// <summary>
/// Repositório de rascunhos locais de impulsionamento originados de tração orgânica.
/// </summary>
public interface IPaidBoostDraftRepository
{
    /// <summary>
    /// Obtém um rascunho existente pela sugestão social.
    /// </summary>
    /// <param name="workspaceId">Workspace proprietário.</param>
    /// <param name="suggestionId">Identificador da sugestão aceita.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Rascunho localizado ou nulo.</returns>
    Task<PaidBoostDraft?> GetBySuggestionIdAsync(
        Guid workspaceId,
        Guid suggestionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adiciona um rascunho ao contexto operacional do tenant.
    /// </summary>
    /// <param name="draft">Entidade a persistir.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Tarefa de conclusão.</returns>
    Task AddAsync(PaidBoostDraft draft, CancellationToken cancellationToken = default);
}
