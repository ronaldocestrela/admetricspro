namespace BuildingBlocks.Application.Messaging;

/// <summary>
/// Tipos versionados de integração do catálogo Master publicados no exchange <c>admetricspro.integration</c>.
/// </summary>
public static class CatalogIntegrationMessageTypes
{
    /// <summary>
    /// Disparo assíncrono de e-mail de boas-vindas após provisionamento.
    /// </summary>
    public const string TenantWelcomeEmail = "master.tenant-welcome-email.v1";

    /// <summary>
    /// Confirmação de assinatura paga após liquidação no checkout ou webhook.
    /// </summary>
    public const string SubscriptionConfirmationEmail = "master.subscription-confirmation-email.v1";

    /// <summary>
    /// Disparo assíncrono de um marco da régua de trial (7, 3, 1 dia ou expirado).
    /// </summary>
    public const string TrialNoticeEmail = "master.trial-notice-email.v1";

    /// <summary>
    /// Disparo assíncrono ao transicionar estágio de inadimplência (D+3, D+7, D+14).
    /// </summary>
    public const string DunningStageEmail = "master.dunning-stage-email.v1";
}