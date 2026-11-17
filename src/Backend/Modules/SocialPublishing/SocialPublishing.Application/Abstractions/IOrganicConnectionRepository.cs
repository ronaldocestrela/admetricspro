using SocialPublishing.Domain.Connections;

namespace SocialPublishing.Application.Abstractions;

/// <summary>
/// Repositório de persistência do agregado <see cref="OrganicConnection"/> no banco do tenant.
/// </summary>
public interface IOrganicConnectionRepository
{
    /// <summary>
    /// Obtém uma conexão orgânica pelo identificador.
    /// </summary>
    /// <param name="id">Identificador da conexão.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A conexão ou <c>null</c> se não existir.</returns>
    Task<OrganicConnection?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém uma conexão pelo trio workspace, plataforma e identificador externo da conta.
    /// </summary>
    /// <param name="workspaceId">Workspace proprietário.</param>
    /// <param name="platform">Plataforma orgânica.</param>
    /// <param name="externalAccountId">Identificador externo da conta na rede.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A conexão ou <c>null</c> se não existir.</returns>
    Task<OrganicConnection?> GetByWorkspacePlatformAndExternalAccountAsync(
        Guid workspaceId,
        OrganicPlatform platform,
        string externalAccountId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista conexões orgânicas de um workspace.
    /// </summary>
    /// <param name="workspaceId">Identificador do workspace.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Lista de conexões do workspace.</returns>
    Task<IReadOnlyList<OrganicConnection>> ListByWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adiciona uma nova conexão orgânica ao contexto.
    /// </summary>
    /// <param name="connection">Agregado a persistir.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Tarefa de conclusão.</returns>
    Task AddAsync(OrganicConnection connection, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marca uma conexão existente como modificada no contexto.
    /// </summary>
    /// <param name="connection">Agregado atualizado.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Tarefa de conclusão.</returns>
    Task UpdateAsync(OrganicConnection connection, CancellationToken cancellationToken = default);

    /// <summary>
    /// Conta conexões orgânicas de um workspace.
    /// </summary>
    /// <param name="workspaceId">Identificador do workspace.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Quantidade de conexões.</returns>
    Task<int> CountByWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default);
}
