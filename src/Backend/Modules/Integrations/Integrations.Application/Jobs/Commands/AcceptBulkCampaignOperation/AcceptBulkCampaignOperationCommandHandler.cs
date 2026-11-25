using System.Text.Json;
using Integrations.Application.Jobs;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Messaging.Persistence;
using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Domain.Messaging;
using BuildingBlocks.Domain.Primitives;

namespace Integrations.Application.Jobs.Commands.AcceptBulkCampaignOperation;

/// <summary>
/// Valida lote e grava job com outbox para mutações externas fora do request HTTP.
/// </summary>
public sealed class AcceptBulkCampaignOperationCommandHandler(
    ITenantUserInitiatedJobCommitter jobCommitter,
    ITenantContextAccessor tenantContextAccessor,
    TimeProvider timeProvider) : ICommandHandler<AcceptBulkCampaignOperationCommand, UserInitiatedJobDto>
{
    private const int AsyncThreshold = 5;

    /// <inheritdoc />
    public async Task<Result<UserInitiatedJobDto>> Handle(
        AcceptBulkCampaignOperationCommand request,
        CancellationToken cancellationToken)
    {
        if (request.WorkspaceId == Guid.Empty)
        {
            return Result<UserInitiatedJobDto>.Failure(
                Error.Validation("BulkCampaign.InvalidWorkspaceId", "O identificador do workspace é obrigatório."));
        }

        if (request.Operations is null || request.Operations.Count == 0)
        {
            return Result<UserInitiatedJobDto>.Failure(
                Error.Validation("BulkCampaign.EmptyList", "A lista de operações em lote não pode ser vazia."));
        }

        if (request.Operations.Count <= AsyncThreshold)
        {
            return Result<UserInitiatedJobDto>.Failure(
                Error.Validation("BulkCampaign.BelowAsyncThreshold", $"Lotes com até {AsyncThreshold} itens permanecem síncronos."));
        }

        var tenantId = tenantContextAccessor.TenantContext?.TenantId;
        if (tenantId is null || tenantId == Guid.Empty)
        {
            return Result<UserInitiatedJobDto>.Failure(
                Error.Validation("BulkCampaign.TenantRequired", "Tenant não resolvido."));
        }

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var jobId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var parametersJson = JsonSerializer.Serialize(
            new BulkCampaignJobParameters(request.WorkspaceId, request.Operations),
            JsonSerializerOptions.Web);

        var jobResult = UserInitiatedJob.CreateAccepted(
            jobId,
            tenantId.Value,
            request.WorkspaceId,
            UserInitiatedJobKind.BulkCampaignOperation,
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
            UserInitiatedJobIntegrationMessageTypes.BulkCampaignMutation,
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