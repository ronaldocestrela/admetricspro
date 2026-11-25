namespace BuildingBlocks.Domain.Messaging;

/// <summary>
/// Tipo de trabalho assíncrono iniciado pelo usuário.
/// </summary>
public enum UserInitiatedJobKind
{
    /// <summary>
    /// Materialização de plano editorial com geração de cópia por slot.
    /// </summary>
    EditorialPlanGenerate = 1,

    /// <summary>
    /// Geração de imagem de feed (Nano Banana) para post aprovado.
    /// </summary>
    PostImageGenerate = 2,

    /// <summary>
    /// Compilação sob demanda de relatório executivo com PDF.
    /// </summary>
    ReportGenerate = 3,

    /// <summary>
    /// Disparo manual de agendamento de relatório (e-mail/WhatsApp).
    /// </summary>
    ReportDispatch = 4,

    /// <summary>
    /// Operação em massa com mutações externas enfileiradas por item.
    /// </summary>
    BulkCampaignOperation = 5,
}