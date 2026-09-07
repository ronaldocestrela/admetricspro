using BuildingBlocks.Domain.Tenants;

namespace Tenants.Application.Users.Repositories;

/// <summary>
/// Contrato de repositório explícito para persistência e consulta da entidade <see cref="TenantUser"/> no banco dedicado do inquilino.
/// </summary>
public interface ITenantUserRepository
{
    /// <summary>
    /// Obtém um colaborador do inquilino pelo seu identificador único.
    /// </summary>
    /// <param name="id">Identificador único do usuário.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A entidade se encontrada; caso contrário, nulo.</returns>
    Task<TenantUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém um colaborador do inquilino pelo seu endereço de e-mail normalizado.
    /// </summary>
    /// <param name="email">E-mail do colaborador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A entidade se encontrada; caso contrário, nulo.</returns>
    Task<TenantUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica se existe um usuário ativo com o identificador informado.
    /// </summary>
    /// <param name="id">Identificador do usuário.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Verdadeiro se existir; caso contrário, falso.</returns>
    Task<bool> ExistsByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém todos os colaboradores do inquilino com filtro opcional por status ativo.
    /// </summary>
    /// <param name="activeOnly">Filtro opcional por status ativo.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Lista somente-leitura de colaboradores.</returns>
    Task<IReadOnlyList<TenantUser>> GetAllAsync(bool? activeOnly = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adiciona um novo colaborador ao contexto operacional do inquilino.
    /// </summary>
    /// <param name="user">Entidade de usuário.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    Task AddAsync(TenantUser user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atualiza os dados de um colaborador no contexto operacional.
    /// </summary>
    /// <param name="user">Entidade de usuário.</param>
    void Update(TenantUser user);
}
