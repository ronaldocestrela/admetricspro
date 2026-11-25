using System.Text.Json;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Messaging.Persistence;
using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Domain.Messaging;
using BuildingBlocks.Domain.Primitives;
using Analytics.Application.Jobs;
using Analytics.Application.Reports.Commands.GenerateReport;

namespace Analytics.Application.Jobs.Commands.AcceptGenerateReport;

/// <summary>
/// Grava job e outbox para geração de relatório sem compilar PDF no request HTTP.
/// </summary>
public sealed class AcceptGenerateReportCommandHandler(
    ITenantUserInitiatedJobCommitter jobCommitter,
    ITenantContextAccessor tenantContextAccessor,
    TimeProvider timeProvider) : ICommandHandler<AcceptGenerateReportCommand, UserInitiatedJobDto>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <inheritdoc />
    public async Task<Result<UserInitiatedJobDto>> Handle(
        AcceptGenerateReportCommand request,
        CancellationToken cancellationToken)
    {
        if (request.WorkspaceId == Guid.Empty)
        {
            return Result<UserInitiatedJobDto>.Failure(
                Error.Validation("Report.InvalidWorkspace", "O workspace é obrigatório."));
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
        var parameters = new ReportGenerateJobParameters(
            request.WorkspaceId,
            request.Title,
            request.StartDateUtc,
            request.EndDateUtc,
            request.CustomNotes,
            request.IncludeCopilotInsights,
            request.IncludeTopCreatives,
            request.IncludeChannelBreakdown,
            request.IncludePacingSummary,
            request.ExpirationDays);

        var parametersJson = JsonSerializer.Serialize(parameters, JsonOptions);
        var jobResult = UserInitiatedJob.CreateAccepted(
            jobId,
            tenantId.Value,
            request.WorkspaceId,
            UserInitiatedJobKind.ReportGenerate,
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
            UserInitiatedJobIntegrationMessageTypes.ReportGenerate,
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