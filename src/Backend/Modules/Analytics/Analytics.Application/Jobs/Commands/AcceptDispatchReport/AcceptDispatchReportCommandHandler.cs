using System.Text.Json;
using Analytics.Domain.Reports;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Messaging.Persistence;
using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Domain.Messaging;
using BuildingBlocks.Domain.Primitives;

namespace Analytics.Application.Jobs.Commands.AcceptDispatchReport;

/// <summary>
/// Grava job para disparo manual de agendamento de relatório.
/// </summary>
public sealed class AcceptDispatchReportCommandHandler(
    IReportRepository reportRepository,
    ITenantUserInitiatedJobCommitter jobCommitter,
    ITenantContextAccessor tenantContextAccessor,
    TimeProvider timeProvider) : ICommandHandler<AcceptDispatchReportCommand, UserInitiatedJobDto>
{
    /// <inheritdoc />
    public async Task<Result<UserInitiatedJobDto>> Handle(
        AcceptDispatchReportCommand request,
        CancellationToken cancellationToken)
    {
        if (request.WorkspaceId == Guid.Empty || request.ScheduleId == Guid.Empty)
        {
            return Result<UserInitiatedJobDto>.Failure(
                Error.Validation("Report.InvalidRequest", "Workspace e agendamento são obrigatórios."));
        }

        var schedule = await reportRepository.GetScheduleByIdAsync(request.ScheduleId, cancellationToken);
        if (schedule is null || schedule.WorkspaceId != request.WorkspaceId)
        {
            return Result<UserInitiatedJobDto>.Failure(
                Error.NotFound("Report.ScheduleNotFound", "Agendamento não localizado."));
        }

        var tenantId = tenantContextAccessor.TenantContext?.TenantId;
        if (tenantId is null || tenantId == Guid.Empty)
        {
            return Result<UserInitiatedJobDto>.Failure(
                Error.Validation("Report.TenantRequired", "Tenant não resolvido."));
        }

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var jobId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var parametersJson = JsonSerializer.Serialize(new { scheduleId = request.ScheduleId });

        var jobResult = UserInitiatedJob.CreateAccepted(
            jobId,
            tenantId.Value,
            request.WorkspaceId,
            UserInitiatedJobKind.ReportDispatch,
            eventId,
            parametersJson,
            utcNow);

        if (jobResult.IsFailure)
        {
            return Result<UserInitiatedJobDto>.Failure(jobResult.Error);
        }

        var outboxResult = UserInitiatedJobOutboxFactory.Create(
            tenantId.Value,
            jobId,
            eventId,
            UserInitiatedJobIntegrationMessageTypes.ReportDispatch,
            utcNow);

        if (outboxResult.IsFailure)
        {
            return Result<UserInitiatedJobDto>.Failure(outboxResult.Error);
        }

        var commit = await jobCommitter.CommitAcceptedJobAsync(jobResult.Value, outboxResult.Value, cancellationToken);
        if (commit.IsFailure)
        {
            return Result<UserInitiatedJobDto>.Failure(commit.Error);
        }

        return Result<UserInitiatedJobDto>.Success(UserInitiatedJobDto.FromEntity(jobResult.Value));
    }
}