using BuildingBlocks.Domain.Primitives;
using Tenants.Application.Squads.DTOs;
using Tenants.Application.Users.DTOs;
using WebApp.Models;

namespace WebApp.Services;

/// <summary>
/// Contrato do cliente HTTP para gestão de equipe e squads no banco dedicado do inquilino.
/// </summary>
public interface ITenantTeamClientService
{
    /// <summary>
    /// Cadastra ou convida um colaborador para o inquilino ativo.
    /// </summary>
    /// <param name="model">Dados do colaborador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado com identificador criado e status do envio de e-mail.</returns>
    Task<Result<InviteTenantUserResultDto>> InviteUserAsync(InviteTenantUserModel model, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reenvia o convite por e-mail para colaborador pendente.
    /// </summary>
    /// <param name="userId">Identificador do colaborador.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado do reenvio.</returns>
    Task<Result<InviteTenantUserResultDto>> ResendInvitationAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cadastra um novo squad interno na agência.
    /// </summary>
    /// <param name="model">Dados do squad.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Identificador do squad criado.</returns>
    Task<Result<Guid>> CreateSquadAsync(CreateSquadModel model, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista os colaboradores da agência.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Lista de colaboradores.</returns>
    Task<Result<IReadOnlyList<TenantUserDto>>> GetUsersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista os squads cadastrados na agência.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Lista de squads.</returns>
    Task<Result<IReadOnlyList<SquadSummaryDto>>> GetSquadsAsync(CancellationToken cancellationToken = default);
}
