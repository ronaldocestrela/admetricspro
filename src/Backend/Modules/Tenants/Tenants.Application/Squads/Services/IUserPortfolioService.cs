using BuildingBlocks.Domain.Primitives;

namespace Tenants.Application.Squads.Services;

/// <summary>
/// Contrato de serviço responsável pela governança de acesso e isolamento por carteira (Portfolio Isolation)
/// entre colaboradores da agência e clientes gerenciados (Workspaces).
/// </summary>
public interface IUserPortfolioService
{
    /// <summary>
    /// Verifica se um determinado colaborador tem autorização para acessar os dados de um workspace específico.
    /// <para>
    /// Usuários com papel administrativo (<see cref="BuildingBlocks.Domain.Tenants.TenantRole.Owner"/> e 
    /// <see cref="BuildingBlocks.Domain.Tenants.TenantRole.Admin"/>) possuem acesso a todos os workspaces.
    /// Usuários operacionais necessitam pertencer a um squad ativo ao qual o workspace esteja alocado.
    /// </para>
    /// </summary>
    /// <param name="userId">Identificador do colaborador.</param>
    /// <param name="workspaceId">Identificador do cliente/workspace.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado booleano indicando se o acesso é autorizado ou falha caso o usuário não exista.</returns>
    Task<Result<bool>> HasAccessToWorkspaceAsync(Guid userId, Guid workspaceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retorna a lista de identificadores de workspaces que o colaborador tem autorização para visualizar.
    /// </summary>
    /// <param name="userId">Identificador do colaborador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Lista somente-leitura com os identificadores autorizados.</returns>
    Task<Result<IReadOnlyList<Guid>>> GetAccessibleWorkspaceIdsAsync(Guid userId, CancellationToken cancellationToken = default);
}
