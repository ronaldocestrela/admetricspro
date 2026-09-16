namespace WebApp.Models;

/// <summary>
/// Estado dos filtros globais do painel executivo analítico.
/// </summary>
public sealed class DashboardFiltersState
{
    /// <summary>
    /// Identificador do workspace selecionado ou nulo para consolidado.
    /// </summary>
    public Guid? WorkspaceId { get; set; }

    /// <summary>
    /// Canal / Rede de anúncios selecionada (All, Meta, Google, TikTok, Bing).
    /// </summary>
    public string Platform { get; set; } = "All";

    /// <summary>
    /// Período predefinido de análise (Last7Days, Last14Days, Last30Days, CurrentMonth, Custom).
    /// </summary>
    public string Period { get; set; } = "Last7Days";

    /// <summary>
    /// Data de início personalizada (quando Period == Custom).
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Data de fim personalizada (quando Period == Custom).
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Dispositivo de acesso selecionado (All, Mobile, Desktop, Tablet).
    /// </summary>
    public string Device { get; set; } = "All";

    /// <summary>
    /// Moeda de visualização (padrão: BRL).
    /// </summary>
    public string Currency { get; set; } = "BRL";

    /// <summary>
    /// Calcula a data inicial efetiva com base no período selecionado.
    /// </summary>
    public DateTime GetEffectiveStartDate()
    {
        var now = DateTime.UtcNow.Date;
        return Period switch
        {
            "Last7Days" => now.AddDays(-6),
            "Last14Days" => now.AddDays(-13),
            "Last30Days" => now.AddDays(-29),
            "CurrentMonth" => new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc),
            "Custom" => StartDate?.Date ?? now.AddDays(-6),
            _ => now.AddDays(-6)
        };
    }

    /// <summary>
    /// Calcula a data final efetiva com base no período selecionado.
    /// </summary>
    public DateTime GetEffectiveEndDate()
    {
        var now = DateTime.UtcNow.Date;
        return Period switch
        {
            "Custom" => EndDate?.Date ?? now,
            _ => now
        };
    }
}
