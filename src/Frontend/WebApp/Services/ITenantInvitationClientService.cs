using BuildingBlocks.Domain.Primitives;
using Tenants.Application.Users.DTOs;

namespace WebApp.Services;

/// <summary>
/// Cliente HTTP para aceite público de convites de colaboradores.
/// </summary>
public interface ITenantInvitationClientService
{
    /// <summary>
    /// Consulta os metadados do convite.
    /// </summary>
    /// <param name="token">Token opaco.</param>
    /// <param name="subdomain">Subdomínio da agência.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Pré-visualização do convite.</returns>
    Task<Result<TenantInvitationPreviewDto>> GetInvitationAsync(
        string token,
        string subdomain,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Aceita o convite definindo a senha.
    /// </summary>
    /// <param name="token">Token opaco.</param>
    /// <param name="subdomain">Subdomínio da agência.</param>
    /// <param name="password">Senha escolhida.</param>
    /// <param name="confirmPassword">Confirmação.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado da operação.</returns>
    Task<Result> AcceptInvitationAsync(
        string token,
        string subdomain,
        string password,
        string confirmPassword,
        CancellationToken cancellationToken = default);
}