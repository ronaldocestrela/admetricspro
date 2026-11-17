using BuildingBlocks.Application.Emails;
using BuildingBlocks.Domain.Primitives;

namespace Tenants.Application.Users.Services;

/// <summary>
/// Compositor de mensagens transacionais de convite de colaborador.
/// </summary>
public interface ITenantInvitationEmailComposer
{
    /// <summary>
    /// Monta o e-mail de convite com link de aceite.
    /// </summary>
    /// <param name="recipientEmail">E-mail do colaborador.</param>
    /// <param name="recipientName">Nome do colaborador.</param>
    /// <param name="agencyName">Nome da agência convidante.</param>
    /// <param name="subdomain">Subdomínio do inquilino.</param>
    /// <param name="rawToken">Token opaco para a URL de aceite.</param>
    /// <param name="expiresAtUtc">Data de expiração do convite.</param>
    /// <returns>Mensagem validada ou erro de validação.</returns>
    Result<EmailMessage> ComposeInvitationEmail(
        string recipientEmail,
        string recipientName,
        string agencyName,
        string subdomain,
        string rawToken,
        DateTime expiresAtUtc);
}