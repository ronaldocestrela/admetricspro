using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Reports;

namespace Analytics.Domain.Reports;

/// <summary>
/// Contrato do orquestrador de envio de relatórios aos destinatários via múltiplos canais (E-mail e WhatsApp).
/// </summary>
public interface IReportDispatchService
{
    /// <summary>
    /// Despacha um relatório gerado para a lista de destinatários configurada, registrando os logs de auditoria.
    /// </summary>
    /// <param name="report">Entidade do relatório gerado.</param>
    /// <param name="schedule">Regra de agendamento com as configurações de entrega.</param>
    /// <param name="model">Modelo de renderização do relatório.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Lista dos logs de auditoria com o resultado de cada tentativa de entrega.</returns>
    Task<Result<IReadOnlyList<ReportDispatchLog>>> DispatchReportAsync(
        GeneratedReport report,
        ReportSchedule schedule,
        ReportRenderModel model,
        CancellationToken cancellationToken = default);
}
