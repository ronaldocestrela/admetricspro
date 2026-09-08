namespace Master.Domain.Tenants;

/// <summary>
/// Identifica os tipos de notificações transacionais e lembretes de ciclo de vida de tenant.
/// </summary>
public enum TrialNoticeType
{
    /// <summary>
    /// E-mail transacional de boas-vindas disparado imediatamente após o provisionamento.
    /// </summary>
    WelcomeEmail = 1,

    /// <summary>
    /// Lembrete automático enviado quando faltam 7 dias para o término do trial.
    /// </summary>
    TrialReminder7Days = 2,

    /// <summary>
    /// Lembrete automático enviado quando faltam 3 dias para o término do trial.
    /// </summary>
    TrialReminder3Days = 3,

    /// <summary>
    /// Lembrete automático enviado quando falta 1 dia para o término do trial.
    /// </summary>
    TrialReminder1Day = 4,

    /// <summary>
    /// Alerta emitido quando o período de trial de 14 dias se esgota sem ativação de plano pago.
    /// </summary>
    TrialExpired = 5,

    /// <summary>
    /// E-mail de confirmação emitido após ativação definitiva de assinatura paga e liquidação financeira.
    /// </summary>
    SubscriptionActivated = 6
}
