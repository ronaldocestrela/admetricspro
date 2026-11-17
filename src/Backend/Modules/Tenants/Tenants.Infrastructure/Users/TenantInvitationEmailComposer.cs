using BuildingBlocks.Application.Emails;
using BuildingBlocks.Domain.Primitives;
using Microsoft.Extensions.Options;
using Tenants.Application.Users.Options;
using Tenants.Application.Users.Services;

namespace Tenants.Infrastructure.Users;

/// <summary>
/// Implementação de <see cref="ITenantInvitationEmailComposer"/>.
/// </summary>
public sealed class TenantInvitationEmailComposer : ITenantInvitationEmailComposer
{
    private readonly TenantInvitationOptions _options;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="TenantInvitationEmailComposer"/>.
    /// </summary>
    /// <param name="options">Opções de URL e validade dos convites.</param>
    public TenantInvitationEmailComposer(IOptions<TenantInvitationOptions> options)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public Result<EmailMessage> ComposeInvitationEmail(
        string recipientEmail,
        string recipientName,
        string agencyName,
        string subdomain,
        string rawToken,
        DateTime expiresAtUtc)
    {
        if (string.IsNullOrWhiteSpace(_options.WebAppBaseUrl))
        {
            return Result<EmailMessage>.Failure(
                Error.Validation("TenantInvitation.WebAppBaseUrlMissing", "A URL base do WebApp não está configurada."));
        }

        var baseUrl = _options.WebAppBaseUrl.TrimEnd('/');
        var inviteUrl = $"{baseUrl}/convite/{Uri.EscapeDataString(rawToken)}?subdomain={Uri.EscapeDataString(subdomain)}";
        var subject = $"{agencyName} convidou você para o AdMetricsPro";
        var plainText =
            $"Olá {recipientName},\n\n" +
            $"Você foi convidado(a) para colaborar na agência {agencyName}.\n" +
            $"Defina sua senha acessando: {inviteUrl}\n\n" +
            $"O convite expira em {expiresAtUtc:dd/MM/yyyy HH:mm} UTC.\n";

        var htmlBody =
            $"<p>Olá <strong>{recipientName}</strong>,</p>" +
            $"<p>Você foi convidado(a) para colaborar na agência <strong>{agencyName}</strong>.</p>" +
            $"<p><a href=\"{inviteUrl}\">Clique aqui para definir sua senha</a></p>" +
            $"<p>Este convite expira em <strong>{expiresAtUtc:dd/MM/yyyy HH:mm} UTC</strong>.</p>";

        return EmailMessage.Create(recipientEmail.Trim(), subject, htmlBody, plainText);
    }
}