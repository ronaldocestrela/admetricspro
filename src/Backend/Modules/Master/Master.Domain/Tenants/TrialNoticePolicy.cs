namespace Master.Domain.Tenants;

/// <summary>
/// Política pura de domínio que calcula a elegibilidade e os limiares temporais para envio de lembretes da régua de trial.
/// </summary>
public static class TrialNoticePolicy
{
    /// <summary>
    /// Avalia a data de expiração e o histórico de lembretes enviados para determinar se existe alguma notificação pendente.
    /// </summary>
    /// <param name="expirationUtc">Data e hora de expiração do trial em UTC.</param>
    /// <param name="referenceUtc">Data e hora de referência para a avaliação (normalmente DateTime.UtcNow).</param>
    /// <param name="alreadySentNotices">Conjunto de tipos de avisos já despachados com sucesso para este tenant.</param>
    /// <returns>O tipo de aviso pendente a ser enviado, ou null caso nenhum aviso deva ser disparado.</returns>
    public static TrialNoticeType? EvaluatePendingNotice(
        DateTime expirationUtc,
        DateTime referenceUtc,
        IReadOnlySet<TrialNoticeType> alreadySentNotices)
    {
        ArgumentNullException.ThrowIfNull(alreadySentNotices);

        var remainingTime = expirationUtc - referenceUtc;
        var remainingDays = remainingTime.TotalDays;

        // Se já expirou
        if (remainingDays <= 0)
        {
            return alreadySentNotices.Contains(TrialNoticeType.TrialExpired)
                ? null
                : TrialNoticeType.TrialExpired;
        }

        // 1 dia restante (ou menos de 24h restantes)
        if (remainingDays <= 1.0)
        {
            return alreadySentNotices.Contains(TrialNoticeType.TrialReminder1Day)
                ? null
                : TrialNoticeType.TrialReminder1Day;
        }

        // 3 dias restantes (janela entre 1.0 e 3.0 dias)
        if (remainingDays <= 3.0)
        {
            return alreadySentNotices.Contains(TrialNoticeType.TrialReminder3Days)
                ? null
                : TrialNoticeType.TrialReminder3Days;
        }

        // 7 dias restantes (janela entre 3.0 e 7.0 dias)
        if (remainingDays <= 7.0)
        {
            return alreadySentNotices.Contains(TrialNoticeType.TrialReminder7Days)
                ? null
                : TrialNoticeType.TrialReminder7Days;
        }

        // Mais de 7 dias restantes: nenhum lembrete necessário ainda
        return null;
    }
}
