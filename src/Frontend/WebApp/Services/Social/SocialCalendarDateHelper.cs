namespace WebApp.Services.Social;

/// <summary>
/// Cálculos de intervalo civil e conversão UTC para a grade do calendário orgânico.
/// </summary>
public static class SocialCalendarDateHelper
{
    /// <summary>
    /// Retorna os dias visíveis na grade conforme o modo e a data âncora.
    /// </summary>
    /// <param name="view">Modo mensal ou semanal.</param>
    /// <param name="anchorDate">Data de referência (qualquer dia do mês ou da semana).</param>
    /// <returns>Dias consecutivos exibidos na grade.</returns>
    public static IReadOnlyList<DateOnly> GetVisibleDays(SocialCalendarView view, DateOnly anchorDate) =>
        view switch
        {
            SocialCalendarView.Month => GetMonthGridDays(anchorDate),
            SocialCalendarView.Week => GetWeekDays(anchorDate),
            _ => throw new ArgumentOutOfRangeException(nameof(view), view, null),
        };

    /// <summary>
    /// Segunda-feira da semana que contém a data informada.
    /// </summary>
    /// <param name="date">Data de referência.</param>
    /// <returns>Primeiro dia (segunda) da semana civil.</returns>
    public static DateOnly StartOfWeekMonday(DateOnly date)
    {
        var dayOfWeek = date.DayOfWeek;
        var offset = dayOfWeek == DayOfWeek.Sunday ? 6 : (int)dayOfWeek - (int)DayOfWeek.Monday;
        return date.AddDays(-offset);
    }

    /// <summary>
    /// Converte o agendamento UTC para a data civil no fuso do workspace.
    /// </summary>
    /// <param name="scheduledAtUtc">Instante UTC do post.</param>
    /// <param name="timeZone">Fuso IANA resolvido.</param>
    /// <returns>Data local do slot.</returns>
    public static DateOnly ToLocalDate(DateTime scheduledAtUtc, TimeZoneInfo timeZone)
    {
        var utc = DateTime.SpecifyKind(scheduledAtUtc, DateTimeKind.Utc);
        var local = TimeZoneInfo.ConvertTimeFromUtc(utc, timeZone);
        return DateOnly.FromDateTime(local);
    }

    /// <summary>
    /// Calcula o novo UTC ao mover o post para outro dia civil preservando hora e minuto locais.
    /// </summary>
    /// <param name="scheduledAtUtc">Horário atual em UTC.</param>
    /// <param name="targetLocalDay">Dia civil de destino no fuso do workspace.</param>
    /// <param name="timeZone">Fuso IANA resolvido.</param>
    /// <returns>Novo instante UTC.</returns>
    public static DateTime ReschedulePreservingLocalTime(
        DateTime scheduledAtUtc,
        DateOnly targetLocalDay,
        TimeZoneInfo timeZone)
    {
        var utc = DateTime.SpecifyKind(scheduledAtUtc, DateTimeKind.Utc);
        var local = TimeZoneInfo.ConvertTimeFromUtc(utc, timeZone);
        var newLocal = new DateTime(
            targetLocalDay.Year,
            targetLocalDay.Month,
            targetLocalDay.Day,
            local.Hour,
            local.Minute,
            local.Second,
            DateTimeKind.Unspecified);
        return DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeToUtc(newLocal, timeZone), DateTimeKind.Utc);
    }

    /// <summary>
    /// Intervalo UTC inclusivo que cobre do início do primeiro dia visível ao fim do último, no fuso informado.
    /// </summary>
    /// <param name="visibleDays">Dias renderizados na grade.</param>
    /// <param name="timeZone">Fuso IANA do workspace.</param>
    /// <returns>Limites inferior e superior em UTC.</returns>
    public static (DateTime FromUtc, DateTime ToUtc) GetUtcRangeForVisibleDays(
        IReadOnlyList<DateOnly> visibleDays,
        TimeZoneInfo timeZone)
    {
        if (visibleDays.Count == 0)
        {
            throw new ArgumentException("A grade deve conter ao menos um dia.", nameof(visibleDays));
        }

        var first = visibleDays[0];
        var last = visibleDays[^1];
        var fromLocal = new DateTime(first.Year, first.Month, first.Day, 0, 0, 0, DateTimeKind.Unspecified);
        var toLocal = new DateTime(last.Year, last.Month, last.Day, 23, 59, 59, DateTimeKind.Unspecified);
        return (
            DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeToUtc(fromLocal, timeZone), DateTimeKind.Utc),
            DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeToUtc(toLocal, timeZone), DateTimeKind.Utc));
    }

    private static IReadOnlyList<DateOnly> GetMonthGridDays(DateOnly anchorDate)
    {
        var firstOfMonth = new DateOnly(anchorDate.Year, anchorDate.Month, 1);
        var lastOfMonth = firstOfMonth.AddMonths(1).AddDays(-1);
        var start = StartOfWeekMonday(firstOfMonth);
        var end = StartOfWeekMonday(lastOfMonth).AddDays(6);
        var days = new List<DateOnly>();
        for (var d = start; d <= end; d = d.AddDays(1))
        {
            days.Add(d);
        }

        return days;
    }

    private static IReadOnlyList<DateOnly> GetWeekDays(DateOnly anchorDate)
    {
        var start = StartOfWeekMonday(anchorDate);
        return Enumerable.Range(0, 7).Select(i => start.AddDays(i)).ToList();
    }
}
