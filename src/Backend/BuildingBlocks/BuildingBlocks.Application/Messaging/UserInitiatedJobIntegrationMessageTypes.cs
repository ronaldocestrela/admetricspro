namespace BuildingBlocks.Application.Messaging;

/// <summary>
/// Tipos versionados de execução de jobs iniciados pelo usuário.
/// </summary>
public static class UserInitiatedJobIntegrationMessageTypes
{
    /// <summary>
    /// Materialização de plano editorial com cópia por slot.
    /// </summary>
    public const string EditorialPlanGenerate = "social.editorial-plan-generate.v1";

    /// <summary>
    /// Geração de imagem de feed para post aprovado.
    /// </summary>
    public const string PostImageGenerate = "social.post-image-generate.v1";

    /// <summary>
    /// Compilação sob demanda de relatório executivo.
    /// </summary>
    public const string ReportGenerate = "analytics.report-generate.v1";

    /// <summary>
    /// Disparo manual de agendamento de relatório.
    /// </summary>
    public const string ReportDispatch = "analytics.report-dispatch.v1";

    /// <summary>
    /// Mutação externa de um item de operação em massa.
    /// </summary>
    public const string BulkCampaignMutation = "integrations.bulk-campaign-mutation.v1";
}