using Analytics.Application.Reports.DTOs;
using BuildingBlocks.Domain.Reports;

namespace WebApp.Models;

/// <summary>
/// Modelo de formulário mutável para vinculação com o componente Blazor de agendamento de relatórios.
/// </summary>
public sealed class ReportScheduleFormModel
{
    /// <summary>Nome do agendamento.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Periodicidade de execução.</summary>
    public ReportFrequency Frequency { get; set; } = ReportFrequency.Weekly;

    /// <summary>Dia da semana quando frequência semanal.</summary>
    public DayOfWeek? DayOfWeek { get; set; } = System.DayOfWeek.Monday;

    /// <summary>Dia do mês quando frequência mensal.</summary>
    public int? DayOfMonth { get; set; } = 1;

    /// <summary>Horário no formato HH:mm.</summary>
    public string ScheduledTime { get; set; } = "08:00";

    /// <summary>Janela de dados do relatório.</summary>
    public ReportDateRangeType DateRangeType { get; set; } = ReportDateRangeType.Last7Days;

    /// <summary>Formato de saída.</summary>
    public ReportOutputFormat OutputFormat { get; set; } = ReportOutputFormat.Both;

    /// <summary>Canais de entrega.</summary>
    public ReportDeliveryChannel DeliveryChannels { get; set; } = ReportDeliveryChannel.Both;

    /// <summary>Título customizado no cabeçalho.</summary>
    public string? CustomTitle { get; set; }

    /// <summary>Observações estratégicas da agência.</summary>
    public string? CustomNotes { get; set; }

    /// <summary>Incluir diagnósticos do Copiloto IA.</summary>
    public bool IncludeCopilotInsights { get; set; } = true;

    /// <summary>Incluir galeria de top criativos.</summary>
    public bool IncludeTopCreatives { get; set; } = true;

    /// <summary>Incluir divisão por plataformas.</summary>
    public bool IncludeChannelBreakdown { get; set; } = true;

    /// <summary>Incluir métricas de Budget Pacing.</summary>
    public bool IncludePacingSummary { get; set; } = true;

    /// <summary>Lista de destinatários do agendamento.</summary>
    public List<RecipientFormModel> Recipients { get; set; } = new();

    /// <summary>
    /// Converte o modelo do formulário em payload imutável <see cref="CreateReportScheduleRequest"/>.
    /// </summary>
    public CreateReportScheduleRequest ToRequest()
    {
        var time = TimeSpan.TryParse(ScheduledTime, out var t) ? t : new TimeSpan(8, 0, 0);
        return new CreateReportScheduleRequest(
            Name,
            Frequency,
            DayOfWeek,
            DayOfMonth,
            time,
            DateRangeType,
            OutputFormat,
            DeliveryChannels,
            CustomTitle,
            CustomNotes,
            IncludeCopilotInsights,
            IncludeTopCreatives,
            IncludeChannelBreakdown,
            IncludePacingSummary,
            Recipients.Select(r => new ReportRecipientDto(r.Name, r.Channel, r.Destination)).ToList());
    }
}

/// <summary>
/// Modelo de formulário mutável para destinatário.
/// </summary>
public sealed class RecipientFormModel
{
    /// <summary>Nome do destinatário.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Canal de entrega (E-mail ou WhatsApp).</summary>
    public ReportDeliveryChannel Channel { get; set; } = ReportDeliveryChannel.Email;

    /// <summary>Endereço de e-mail ou telefone.</summary>
    public string Destination { get; set; } = string.Empty;
}
