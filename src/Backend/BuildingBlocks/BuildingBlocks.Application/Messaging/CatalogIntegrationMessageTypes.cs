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
}