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

    /// <summary>
    /// Evento operacional emitido após ingestão idempotente de métricas de uma conta conectada.
    /// </summary>
    public const string CampaignMetricsSynced = "integrations.campaign-metrics-synced.v1";

    /// <summary>
    /// Evento operacional emitido após persistência da hierarquia de campanhas de uma conta conectada.
    /// </summary>
    public const string CampaignHierarchySynced = "integrations.campaign-hierarchy-synced.v1";
}