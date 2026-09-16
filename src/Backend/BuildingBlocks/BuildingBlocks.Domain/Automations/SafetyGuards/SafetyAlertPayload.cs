namespace BuildingBlocks.Domain.Automations.SafetyGuards;

/// <summary>
/// Modelo estruturado de carga de dados para disparo de alertas de segurança multi-canal.
/// </summary>
/// <param name="IncidentId">Identificador único do incidente registrado.</param>
/// <param name="WorkspaceId">Identificador do workspace associado.</param>
/// <param name="GuardType">Tipo da trava acionada (Overspending ou BrokenLandingPage).</param>
/// <param name="Severity">Nível de severidade do alerta.</param>
/// <param name="Title">Título resumido do alerta.</param>
/// <param name="Message">Mensagem detalhada com dados contextuais e justificativa da ação.</param>
/// <param name="TargetEntityName">Nome da campanha ou anúncio afetado.</param>
/// <param name="TargetEntityId">Identificador da entidade afetada.</param>
/// <param name="Platform">Nome da plataforma de anúncios (MetaAds, GoogleAds, TikTokAds, BingAds).</param>
/// <param name="ActionTaken">Ação preventiva executada (ex: Campanha Pausada, Anúncio Pausado).</param>
/// <param name="MetricsContext">Métricas ou dados adicionais de diagnóstico (ex: Spend, DailyBudget, StatusCode, DestinationUrl).</param>
/// <param name="TimestampUtc">Carimbo de data/hora UTC em que o incidente ocorreu.</param>
public sealed record SafetyAlertPayload(
    Guid IncidentId,
    Guid WorkspaceId,
    SafetyGuardType GuardType,
    SafetyAlertSeverity Severity,
    string Title,
    string Message,
    string TargetEntityName,
    Guid TargetEntityId,
    string Platform,
    string ActionTaken,
    IReadOnlyDictionary<string, string> MetricsContext,
    DateTime TimestampUtc);
