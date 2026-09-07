using BuildingBlocks.Domain.Tenants;

namespace Tenants.Application.Integrations.Repositories;

/// <summary>
/// Contrato de repositório para persistência e consulta da entidade <see cref="ConnectedAdAccount"/> no banco dedicado do inquilino.
/// </summary>
public interface IConnectedAdAccountRepository
{
    /// <summary>
    /// Obtém uma conta de anúncios pelo seu identificador único.
    /// </summary>
    /// <param name="id">Identificador da conta.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A conta se encontrada; caso contrário, nulo.</returns>
    Task<ConnectedAdAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém todas as contas de anúncios vinculadas a um determinado workspace.
    /// </summary>
    /// <param name="workspaceId">Identificador do workspace.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Lista de contas conectadas.</returns>
    Task<IReadOnlyList<ConnectedAdAccount>> GetByWorkspaceIdAsync(Guid workspaceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém todas as contas de anúncios conectadas em todo o inquilino.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Lista de todas as contas conectadas.</returns>
    Task<IReadOnlyList<ConnectedAdAccount>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retorna a contagem total de contas de anúncios conectadas (incluindo modo demonstração).
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Número total de contas conectadas.</returns>
    Task<int> CountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adiciona uma nova conta de anúncios ao contexto operacional do inquilino.
    /// </summary>
    /// <param name="account">Instância da conta a ser adicionada.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    Task AddAsync(ConnectedAdAccount account, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atualiza uma conta de anúncios existente no contexto operacional.
    /// </summary>
    /// <param name="account">Instância modificada.</param>
    void Update(ConnectedAdAccount account);
}
