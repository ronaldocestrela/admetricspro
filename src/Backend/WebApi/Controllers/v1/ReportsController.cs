using Analytics.Application.Reports.Commands.CreateReportSchedule;
using Analytics.Application.Reports.Commands.DispatchReportSchedule;
using Analytics.Application.Reports.Commands.GenerateReport;
using Analytics.Application.Reports.Commands.UpdateReportSchedule;
using Analytics.Application.Reports.DTOs;
using Analytics.Application.Reports.Queries;
using BuildingBlocks.Domain.Primitives;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.v1;

/// <summary>
/// Controlador para gerenciamento de agendamentos e geração de relatórios white-label (Subfase 5.4).
/// </summary>
[ApiController]
[Route("api/v1/workspaces/{workspaceId:guid}/reports")]
public sealed class ReportsController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="ReportsController"/>.
    /// </summary>
    public ReportsController(ISender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    /// <summary>
    /// Lista todos os agendamentos de relatórios configurados para o Workspace.
    /// </summary>
    [HttpGet("schedules")]
    [EndpointSummary("Lista as regras de agendamento automático de relatórios do Workspace")]
    [ProducesResponseType(typeof(Result<IReadOnlyList<ReportScheduleDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<IReadOnlyList<ReportScheduleDto>>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Result<IReadOnlyList<ReportScheduleDto>>>> GetSchedules(
        [FromRoute] Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetReportSchedulesQuery(workspaceId), cancellationToken);
        if (result.IsFailure) return BadRequest(result);
        return Ok(result);
    }

    /// <summary>
    /// Cria uma nova regra de agendamento automático para envio de relatórios.
    /// </summary>
    [HttpPost("schedules")]
    [EndpointSummary("Cria um novo agendamento automático de relatório")]
    [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Result<Guid>>> CreateSchedule(
        [FromRoute] Guid workspaceId,
        [FromBody] CreateReportScheduleRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new CreateReportScheduleCommand(
            workspaceId,
            request.Name,
            request.Frequency,
            request.DayOfWeek,
            request.DayOfMonth,
            request.ScheduledTimeUtc,
            request.DateRangeType,
            request.OutputFormat,
            request.DeliveryChannels,
            request.CustomTitle,
            request.CustomNotes,
            request.IncludeCopilotInsights,
            request.IncludeTopCreatives,
            request.IncludeChannelBreakdown,
            request.IncludePacingSummary,
            request.Recipients);

        var result = await _sender.Send(command, cancellationToken);
        if (result.IsFailure) return BadRequest(result);
        return Ok(result);
    }

    /// <summary>
    /// Atualiza uma regra de agendamento de relatório existente.
    /// </summary>
    [HttpPut("schedules/{scheduleId:guid}")]
    [EndpointSummary("Atualiza os parâmetros de um agendamento existente")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Result>> UpdateSchedule(
        [FromRoute] Guid workspaceId,
        [FromRoute] Guid scheduleId,
        [FromBody] CreateReportScheduleRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new UpdateReportScheduleCommand(
            scheduleId,
            request.Name,
            request.Frequency,
            request.DayOfWeek,
            request.DayOfMonth,
            request.ScheduledTimeUtc,
            request.DateRangeType,
            request.OutputFormat,
            request.DeliveryChannels,
            request.CustomTitle,
            request.CustomNotes,
            request.IncludeCopilotInsights,
            request.IncludeTopCreatives,
            request.IncludeChannelBreakdown,
            request.IncludePacingSummary,
            IsActive: true,
            request.Recipients ?? new List<ReportRecipientDto>());

        var result = await _sender.Send(command, cancellationToken);
        if (result.IsFailure) return BadRequest(result);
        return Ok(result);
    }

    /// <summary>
    /// Exclui um agendamento de relatório.
    /// </summary>
    [HttpDelete("schedules/{scheduleId:guid}")]
    [EndpointSummary("Exclui uma regra de agendamento de relatório")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Result>> DeleteSchedule(
        [FromRoute] Guid workspaceId,
        [FromRoute] Guid scheduleId,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new DeleteReportScheduleCommand(scheduleId), cancellationToken);
        if (result.IsFailure) return BadRequest(result);
        return Ok(result);
    }

    /// <summary>
    /// Compila e gera um relatório executivo sob demanda com token web e PDF.
    /// </summary>
    [HttpPost("generate")]
    [EndpointSummary("Gera um relatório executivo sob demanda com link web e PDF")]
    [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Result<Guid>>> GenerateReport(
        [FromRoute] Guid workspaceId,
        [FromBody] GenerateReportRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new GenerateReportCommand(
            workspaceId,
            request.Title,
            request.StartDateUtc,
            request.EndDateUtc,
            request.CustomNotes,
            request.IncludeCopilotInsights,
            request.IncludeTopCreatives,
            request.IncludeChannelBreakdown,
            request.IncludePacingSummary,
            request.ExpirationDays);

        var result = await _sender.Send(command, cancellationToken);
        if (result.IsFailure) return BadRequest(result);
        return Ok(result);
    }

    /// <summary>
    /// Dispara imediatamente um agendamento para testes de entrega via E-mail e WhatsApp.
    /// </summary>
    [HttpPost("schedules/{scheduleId:guid}/dispatch")]
    [EndpointSummary("Dispara manualmente um agendamento para seus destinatários")]
    [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Result<Guid>>> DispatchSchedule(
        [FromRoute] Guid workspaceId,
        [FromRoute] Guid scheduleId,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new DispatchReportScheduleCommand(scheduleId), cancellationToken);
        if (result.IsFailure) return BadRequest(result);
        return Ok(result);
    }

    /// <summary>
    /// Lista o histórico de relatórios compilados e gerados no Workspace.
    /// </summary>
    [HttpGet("history")]
    [EndpointSummary("Obtém o histórico de relatórios gerados e seus links públicos")]
    [ProducesResponseType(typeof(Result<IReadOnlyList<GeneratedReportSummaryDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Result<IReadOnlyList<GeneratedReportSummaryDto>>>> GetHistory(
        [FromRoute] Guid workspaceId,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetGeneratedReportsQuery(workspaceId, limit), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Faz o download do arquivo PDF binário de um relatório gerado.
    /// </summary>
    [HttpGet("{reportId:guid}/pdf")]
    [EndpointSummary("Baixa o arquivo binário PDF do relatório gerado")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadPdf(
        [FromRoute] Guid workspaceId,
        [FromRoute] Guid reportId,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetReportPdfQuery(reportId, null), cancellationToken);
        if (result.IsFailure) return NotFound(result);

        return File(result.Value, "application/pdf", $"relatorio-{reportId}.pdf");
    }
}
