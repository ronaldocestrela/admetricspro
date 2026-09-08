using BuildingBlocks.Application.Emails;
using Master.Domain.Tenants;

namespace Master.Application.Emails;

/// <summary>
/// Contrato para renderização de templates de e-mails transacionais em HTML e texto plano para o AdMetricsPro.
/// </summary>
public interface ITransactionalEmailTemplateRenderer
{
    /// <summary>
    /// Renderiza o e-mail de boas-vindas para o gestor com o link direto para o subdomínio da agência.
    /// </summary>
    /// <param name="recipientEmail">Endereço de e-mail de destino.</param>
    /// <param name="recipientName">Nome completo do gestor inicial.</param>
    /// <param name="companyName">Razão social ou nome fantasia da agência.</param>
    /// <param name="subdomain">Subdomínio de acesso atribuído ao tenant.</param>
    /// <param name="customDomain">Domínio CNAME customizado opcional.</param>
    /// <param name="tier">Plano contratado ou nível de trial.</param>
    /// <returns>Mensagem de e-mail pronta para envio.</returns>
    EmailMessage RenderWelcomeEmail(
        string recipientEmail,
        string recipientName,
        string companyName,
        string subdomain,
        string? customDomain,
        SubscriptionTier tier);

    /// <summary>
    /// Renderiza os e-mails de lembrete da régua de trial conforme o marco temporal (7, 3 ou 1 dia(s) restantes ou expirado).
    /// </summary>
    /// <param name="recipientEmail">Endereço de e-mail de destino.</param>
    /// <param name="recipientName">Nome completo do gestor.</param>
    /// <param name="companyName">Razão social da agência.</param>
    /// <param name="subdomain">Subdomínio da agência.</param>
    /// <param name="customDomain">Domínio CNAME customizado opcional.</param>
    /// <param name="noticeType">Tipo do marco de trial a ser comunicado.</param>
    /// <param name="expirationUtc">Timestamp de expiração do trial.</param>
    /// <param name="daysRemaining">Dias restantes calculados.</param>
    /// <returns>Mensagem de e-mail pronta para envio.</returns>
    EmailMessage RenderTrialNoticeEmail(
        string recipientEmail,
        string recipientName,
        string companyName,
        string subdomain,
        string? customDomain,
        TrialNoticeType noticeType,
        DateTime expirationUtc,
        double daysRemaining);
}
