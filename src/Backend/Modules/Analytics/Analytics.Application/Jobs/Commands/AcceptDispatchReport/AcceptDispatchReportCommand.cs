using BuildingBlocks.Application.Messaging;

namespace Analytics.Application.Jobs.Commands.AcceptDispatchReport;

/// <summary>
/// Aceita disparo assíncrono de agendamento de relatório.
/// </summary>
public sealed record AcceptDispatchReportCommand(Guid WorkspaceId, Guid ScheduleId) : ICommand<UserInitiatedJobDto>;