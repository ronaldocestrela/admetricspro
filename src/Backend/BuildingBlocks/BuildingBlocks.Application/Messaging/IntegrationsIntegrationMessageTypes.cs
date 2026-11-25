namespace BuildingBlocks.Application.Messaging;

/// <summary>
/// Tipos versionados de integração operacional (tenant) publicados no exchange <c>admetricspro.integration</c>.
/// </summary>
public static class IntegrationsIntegrationMessageTypes
{
    /// <summary>
    /// Uma mensagem por conta de anúncio elegível para sincronizar hierarquia e métricas.
    /// </summary>
    public const string AdAccountSync = "integrations.ad-account-sync.v1";
}