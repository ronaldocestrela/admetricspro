using BuildingBlocks.Domain.Reports;

namespace Analytics.Domain.Reports;

/// <summary>
/// Contrato do repositório de persistência de agendamentos, relatórios gerados e auditoria de despachos.
/// </summary>
public interface IReportRepository
{
    /// <summary>
    /// Obtém uma regra de agendamento pelo seu identificador.
    /// </summary>
    Task<ReportSchedule?> GetScheduleByIdAsync(Guid scheduleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista todos os agendamentos configurados para um Workspace.
    /// </summary>
    Task<IReadOnlyList<ReportSchedule>> GetSchedulesByWorkspaceIdAsync(Guid workspaceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém todos os agendamentos ativos cuja execução está pendente (NextExecutionAtUtc &lt;= data limite).
    /// </summary>
    Task<IReadOnlyList<ReportSchedule>> GetPendingSchedulesAsync(DateTime dueLimitUtc, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adiciona um novo agendamento de relatório.
    /// </summary>
    Task AddScheduleAsync(ReportSchedule schedule, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atualiza um agendamento existente.
    /// </summary>
    Task UpdateScheduleAsync(ReportSchedule schedule, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove um agendamento.
    /// </summary>
    Task DeleteScheduleAsync(ReportSchedule schedule, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém um relatório gerado pelo seu token seguro de compartilhamento.
    /// </summary>
    Task<GeneratedReport?> GetReportByShareTokenAsync(string shareToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém um relatório gerado pelo seu identificador.
    /// </summary>
    Task<GeneratedReport?> GetReportByIdAsync(Guid reportId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista os relatórios gerados para um Workspace com ordenação descendente pela data de compilação.
    /// </summary>
    Task<IReadOnlyList<GeneratedReport>> GetReportsByWorkspaceIdAsync(Guid workspaceId, int limit = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adiciona um novo relatório gerado ao histórico.
    /// </summary>
    Task AddGeneratedReportAsync(GeneratedReport report, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atualiza um relatório gerado.
    /// </summary>
    Task UpdateGeneratedReportAsync(GeneratedReport report, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adiciona múltiplos registros de despacho ao histórico de auditoria.
    /// </summary>
    Task AddDispatchLogsAsync(IEnumerable<ReportDispatchLog> logs, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém os registros de auditoria de despacho de um relatório específico.
    /// </summary>
    Task<IReadOnlyList<ReportDispatchLog>> GetDispatchLogsByReportIdAsync(Guid reportId, CancellationToken cancellationToken = default);
}
