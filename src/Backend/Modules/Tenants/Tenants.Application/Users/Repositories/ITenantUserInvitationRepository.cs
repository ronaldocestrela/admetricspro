using BuildingBlocks.Domain.Tenants;

namespace Tenants.Application.Users.Repositories;

/// <summary>
/// Contrato de repositório para convites de colaboradores no banco dedicado do inquilino.
/// </summary>
public interface ITenantUserInvitationRepository
{
    /// <summary>
    /// Obtém um convite pelo hash do token.
    /// </summary>
    /// <param name="tokenHash">Hash SHA-256 do token.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Convite encontrado ou nulo.</returns>
    Task<TenantUserInvitation?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém o convite aberto (não aceito e não revogado) de um colaborador.
    /// </summary>
    /// <param name="userId">Identificador do colaborador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Convite aberto ou nulo.</returns>
    Task<TenantUserInvitation?> GetOpenByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista convites abertos para os colaboradores informados.
    /// </summary>
    /// <param name="userIds">Identificadores dos colaboradores.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Lista de convites ainda válidos.</returns>
    Task<IReadOnlyList<TenantUserInvitation>> GetOpenByUserIdsAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Persiste um novo convite.
    /// </summary>
    /// <param name="invitation">Entidade de convite.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    Task AddAsync(TenantUserInvitation invitation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atualiza um convite existente.
    /// </summary>
    /// <param name="invitation">Entidade de convite.</param>
    void Update(TenantUserInvitation invitation);
}