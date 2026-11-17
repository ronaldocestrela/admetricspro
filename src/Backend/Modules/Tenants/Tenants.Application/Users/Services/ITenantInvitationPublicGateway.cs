using BuildingBlocks.Domain.Primitives;
using Tenants.Application.Users.DTOs;

namespace Tenants.Application.Users.Services;

/// <summary>
/// Porta de acesso público (sem contexto HTTP de inquilino) para consulta e aceite de convites.
/// </summary>
public interface ITenantInvitationPublicGateway
{
    /// <summary>
    /// Obtém os metadados públicos de um convite.
    /// </summary>
    /// <param name="token">Token opaco.</param>
    /// <param name="subdomain">Subdomínio da agência.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Pré-visualização do convite ou erro de negócio.</returns>
    Task<Result<TenantInvitationPreviewDto>> GetPreviewAsync(
        string token,
        string subdomain,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Aceita o convite e ativa a conta do colaborador.
    /// </summary>
    /// <param name="token">Token opaco.</param>
    /// <param name="subdomain">Subdomínio da agência.</param>
    /// <param name="password">Senha escolhida.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado da operação.</returns>
    Task<Result> AcceptAsync(
        string token,
        string subdomain,
        string password,
        CancellationToken cancellationToken = default);
}