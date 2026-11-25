using Master.Domain.Tenants;

namespace Master.Application.Messaging;

/// <summary>
/// Mapeia estágios de inadimplência para tipos de notificação transacional.
/// </summary>
public static class DunningEmailNoticeMapper
{
    /// <summary>
    /// Converte um estágio de dunning em tipo de log de notificação, quando aplicável.
    /// </summary>
    /// <param name="stage">Estágio avaliado.</param>
    /// <returns>Tipo de e-mail ou <c>null</c> na carência.</returns>
    public static TrialNoticeType? ToNoticeType(DunningStage stage) =>
        stage switch
        {
            DunningStage.AutomationsDisabled => TrialNoticeType.DunningAutomationsDisabled,
            DunningStage.ReportsBlocked => TrialNoticeType.DunningReportsBlocked,
            DunningStage.LoginBlocked => TrialNoticeType.DunningLoginBlocked,
            _ => null
        };
}