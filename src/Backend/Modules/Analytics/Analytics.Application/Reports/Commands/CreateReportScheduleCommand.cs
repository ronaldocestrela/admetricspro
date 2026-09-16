using Analytics.Application.Reports.DTOs;
using Analytics.Domain.Reports;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Reports;

namespace Analytics.Application.Reports.Commands.CreateReportSchedule;

/// <summary>
/// Comando para criar uma nova regra de agendamento de relatórios white-label.
/// </summary>
public sealed record CreateReportScheduleCommand(
    Guid WorkspaceId,
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
    IReadOnlyList<ReportRecipientDto>? Recipients) : ICommand<Guid>;

/// <summary>
/// Manipulador do comando <see cref="CreateReportScheduleCommand"/>.
/// </summary>
public sealed class CreateReportScheduleCommandHandler : ICommandHandler<CreateReportScheduleCommand, Guid>
{
    private readonly IReportRepository _repository;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="CreateReportScheduleCommandHandler"/>.
    /// </summary>
    public CreateReportScheduleCommandHandler(IReportRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(CreateReportScheduleCommand request, CancellationToken cancellationToken)
    {
        var recipientsList = new List<ReportRecipient>();
        if (request.Recipients is not null)
        {
            foreach (var r in request.Recipients)
            {
                var recipientResult = ReportRecipient.Create(r.Name, r.Channel, r.Destination);
                if (recipientResult.IsFailure)
                {
                    return Result<Guid>.Failure(recipientResult.Error);
                }
                recipientsList.Add(recipientResult.Value);
            }
        }

        var scheduleId = Guid.NewGuid();
        var scheduleResult = ReportSchedule.Create(
            scheduleId,
            request.WorkspaceId,
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

        if (scheduleResult.IsFailure)
        {
            return Result<Guid>.Failure(scheduleResult.Error);
        }

        await _repository.AddScheduleAsync(scheduleResult.Value, cancellationToken);
        return Result<Guid>.Success(scheduleId);
    }
}
