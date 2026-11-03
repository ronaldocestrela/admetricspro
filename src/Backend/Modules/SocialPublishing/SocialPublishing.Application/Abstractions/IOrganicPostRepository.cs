using SocialPublishing.Domain.Posts;

namespace SocialPublishing.Application.Abstractions;

/// <summary>
/// Repositório de persistência do agregado <see cref="OrganicPost"/> no banco do tenant.
/// </summary>
public interface IOrganicPostRepository
{
    /// <summary>
    /// Obtém um post orgânico pelo identificador, incluindo mídias associadas.
    /// </summary>
    /// <param name="id">Identificador do post.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O post ou <c>null</c> se não existir.</returns>
    Task<OrganicPost?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista posts orgânicos de um workspace ordenados por horário agendado.
    /// </summary>
    /// <param name="workspaceId">Identificador do workspace.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Lista de posts do workspace.</returns>
    Task<IReadOnlyList<OrganicPost>> ListByWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adiciona um novo post orgânico ao contexto.
    /// </summary>
    /// <param name="post">Agregado a persistir.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Tarefa de conclusão.</returns>
    Task AddAsync(OrganicPost post, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marca um post existente como modificado no contexto.
    /// </summary>
    /// <param name="post">Agregado atualizado.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Tarefa de conclusão.</returns>
    Task UpdateAsync(OrganicPost post, CancellationToken cancellationToken = default);
}
