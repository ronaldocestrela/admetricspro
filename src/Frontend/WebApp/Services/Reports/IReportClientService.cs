using Analytics.Application.Reports.DTOs;
using BuildingBlocks.Domain.Primitives;

namespace WebApp.Services.Reports;

/// <summary>
/// Contrato do cliente HTTP para consumo das APIs de relatórios white-label autenticadas.
/// </summary>
public interface IReportClientService
{
    /// <summary>
    /// Lista os agendamentos de relatórios configurados para o Workspace.
    /// </summary>
    Task<Result<IReadOnlyList<ReportScheduleDto>>> GetSchedulesAsync(Guid workspaceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cria uma nova regra de agendamento automático de relatório.
    /// </summary>
    Task<Result<Guid>> CreateScheduleAsync(Guid workspaceId, CreateReportScheduleRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atualiza uma regra de agendamento existente.
    /// </summary>
    Task<Result> UpdateScheduleAsync(Guid workspaceId, Guid scheduleId, CreateReportScheduleRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove uma regra de agendamento.
    /// </summary>
    Task<Result> DeleteScheduleAsync(Guid workspaceId, Guid scheduleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Compila e gera um relatório executivo sob demanda.
    /// </summary>
    Task<Result<Guid>> GenerateReportAsync(Guid workspaceId, GenerateReportRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Dispara manualmente o envio de um agendamento para testes de entrega.
    /// </summary>
    Task<Result<Guid>> DispatchScheduleAsync(Guid workspaceId, Guid scheduleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém o histórico de relatórios gerados do Workspace.
    /// </summary>
    Task<Result<IReadOnlyList<GeneratedReportSummaryDto>>> GetHistoryAsync(Guid workspaceId, int limit = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// Baixa o PDF do relatório gerado.
    /// </summary>
    Task<Result<byte[]>> DownloadPdfAsync(Guid workspaceId, Guid reportId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Contrato do cliente HTTP para acesso público e anônimo a relatórios compartilhados por token.
/// </summary>
public interface IPublicReportClientService
{
    /// <summary>
    /// Consulta os dados completos do relatório compartilhado e seu branding por token.
    /// </summary>
    Task<Result<PublicSharedReportDto>> GetSharedReportAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Baixa o binário do PDF do relatório público por token.
    /// </summary>
    Task<Result<byte[]>> DownloadSharedReportPdfAsync(string token, CancellationToken cancellationToken = default);
}
