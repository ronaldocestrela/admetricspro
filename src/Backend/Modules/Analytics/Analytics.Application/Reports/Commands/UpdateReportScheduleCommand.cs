using Analytics.Application.Reports.DTOs;
using Analytics.Domain.Reports;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Reports;

namespace Analytics.Application.Reports.Commands.UpdateReportSchedule;

/// <summary>
/// Comando para atualizar uma regra de agendamento existente.
/// </summary>
public sealed record UpdateReportScheduleCommand(
    Guid ScheduleId,
    string Name,
    ReportFrequency Frequency,
    DayOfWeek? DayOfWeek,
    int? DayOfMonth,
    TimeSpan ScheduledTimeUtc,
    ReportDateRangeType DateRangeType,
    ReportOutputFormat OutputFormat,
    ReportDeliveryChannel DeliveryChannels,
    string? CustomTitle,
    string? CustomNotes,
    bool IncludeCopilotInsights,
    bool IncludeTopCreatives,
    bool IncludeChannelBreakdown,
    bool IncludePacingSummary,
    bool IsActive,
    IReadOnlyList<ReportRecipientDto> Recipients) : ICommand;

/// <summary>
/// Manipulador do comando <see cref="UpdateReportScheduleCommand"/>.
/// </summary>
public sealed class UpdateReportScheduleCommandHandler : ICommandHandler<UpdateReportScheduleCommand>
{
    private readonly IReportRepository _repository;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="UpdateReportScheduleCommandHandler"/>.
    /// </summary>
    public UpdateReportScheduleCommandHandler(IReportRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    /// <inheritdoc />
    public async Task<Result> Handle(UpdateReportScheduleCommand request, CancellationToken cancellationToken)
    {
        var schedule = await _repository.GetScheduleByIdAsync(request.ScheduleId, cancellationToken);
        if (schedule is null)
        {
            return Result.Failure(Error.NotFound("ReportSchedule.NotFound", "Agendamento de relatório não localizado."));
        }

        var recipientsList = new List<ReportRecipient>();
        foreach (var r in request.Recipients)
        {
            var recipientResult = ReportRecipient.Create(r.Name, r.Channel, r.Destination);
            if (recipientResult.IsFailure)
            {
                return Result.Failure(recipientResult.Error);
            }
            recipientsList.Add(recipientResult.Value);
        }

        var updateResult = schedule.Update(
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
            recipientsList,
            DateTime.UtcNow);

        if (updateResult.IsFailure)
        {
            return updateResult;
        }

        schedule.SetActive(request.IsActive);
        await _repository.UpdateScheduleAsync(schedule, cancellationToken);

        return Result.Success();
    }
}

/// <summary>
/// Comando para excluir uma regra de agendamento.
/// </summary>
public sealed record DeleteReportScheduleCommand(Guid ScheduleId) : ICommand;

/// <summary>
/// Manipulador do comando <see cref="DeleteReportScheduleCommand"/>.
/// </summary>
public sealed class DeleteReportScheduleCommandHandler : ICommandHandler<DeleteReportScheduleCommand>
{
    private readonly IReportRepository _repository;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="DeleteReportScheduleCommandHandler"/>.
    /// </summary>
    public DeleteReportScheduleCommandHandler(IReportRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    /// <inheritdoc />
    public async Task<Result> Handle(DeleteReportScheduleCommand request, CancellationToken cancellationToken)
    {
        var schedule = await _repository.GetScheduleByIdAsync(request.ScheduleId, cancellationToken);
        if (schedule is null)
        {
            return Result.Failure(Error.NotFound("ReportSchedule.NotFound", "Agendamento de relatório não localizado."));
        }

        await _repository.DeleteScheduleAsync(schedule, cancellationToken);
        return Result.Success();
    }
}
