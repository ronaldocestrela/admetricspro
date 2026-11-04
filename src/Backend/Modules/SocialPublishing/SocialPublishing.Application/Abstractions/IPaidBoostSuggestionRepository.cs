using SocialPublishing.Domain.Traction;

namespace SocialPublishing.Application.Abstractions;

/// <summary>
/// Repositório de sugestões de impulsionamento por tração orgânica.
/// </summary>
public interface IPaidBoostSuggestionRepository
{
    /// <summary>
    /// Adiciona uma sugestão ao contexto.
    /// </summary>
    /// <param name="suggestion">Entidade a persistir.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Tarefa de conclusão.</returns>
    Task AddAsync(PaidBoostSuggestion suggestion, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém os identificadores de posts com sugestão aberta no workspace.
    /// </summary>
    /// <param name="workspaceId">Workspace filtrado.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Conjunto de ids de posts orgânicos.</returns>
    Task<IReadOnlySet<Guid>> GetOpenOrganicPostIdsAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém uma sugestão pelo identificador.
    /// </summary>
    /// <param name="id">Identificador da sugestão.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Sugestão localizada ou nulo.</returns>
    Task<PaidBoostSuggestion?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista sugestões do workspace.
    /// </summary>
    /// <param name="workspaceId">Workspace filtrado.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Coleção de sugestões.</returns>
    Task<IReadOnlyList<PaidBoostSuggestion>> ListByWorkspaceAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marca a entidade como modificada no contexto.
    /// </summary>
    /// <param name="suggestion">Sugestão atualizada.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Tarefa de conclusão.</returns>
    Task UpdateAsync(PaidBoostSuggestion suggestion, CancellationToken cancellationToken = default);
}
