namespace BuildingBlocks.Domain.Automations.SafetyGuards;

/// <summary>
/// Tipos de travas de segurança operacional suportadas pelo sistema.
/// </summary>
public enum SafetyGuardType
{
    /// <summary>
    /// Trava de estouro orçamentário diário (Overspending).
    /// </summary>
    Overspending = 1,

    /// <summary>
    /// Trava de integridade de links e páginas de destino inacessíveis (404/500/Timeout).
    /// </summary>
    BrokenLandingPage = 2
}

/// <summary>
/// Níveis de severidade para incidentes de segurança e alertas operacionais.
/// </summary>
public enum SafetyAlertSeverity
{
    /// <summary>
    /// Alerta informativo ou de aviso preventivo.
    /// </summary>
    Warning = 1,

    /// <summary>
    /// Alerta crítico que exige intervenção imediata ou pausa automática preventiva.
    /// </summary>
    Critical = 2,

    /// <summary>
    /// Alerta de emergência financeira ou operacional.
    /// </summary>
    Emergency = 3
}

/// <summary>
/// Canais de notificação disponíveis para despacho de alertas de segurança.
/// </summary>
public enum SafetyAlertChannel
{
    /// <summary>
    /// Notificação via Webhook corporativo do Slack.
    /// </summary>
    Slack = 1,

    /// <summary>
    /// Notificação via WhatsApp comercial.
    /// </summary>
    WhatsApp = 2,

    /// <summary>
    /// Notificação via e-mail transacional.
    /// </summary>
    Email = 3,

    /// <summary>
    /// Notificação via Webhook genérico HTTP POST.
    /// </summary>
    Webhook = 4
}

/// <summary>
/// Status do ciclo de vida de um incidente de segurança.
/// </summary>
public enum SafetyIncidentStatus
{
    /// <summary>
    /// Incidente detectado e pendente de tratamento.
    /// </summary>
    Detected = 1,

    /// <summary>
    /// Incidente mitigado automaticamente (ex: campanha ou anúncio pausado preventivamente).
    /// </summary>
    Mitigated = 2,

    /// <summary>
    /// Incidente reconhecido manualmente pelo gestor de tráfego.
    /// </summary>
    Acknowledged = 3,

    /// <summary>
    /// Incidente resolvido e entidade restabelecida.
    /// </summary>
    Resolved = 4
}
