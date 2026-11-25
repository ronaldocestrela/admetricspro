using System.Text.Json;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Domain.Messaging;
using BuildingBlocks.Domain.Primitives;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Application.Jobs;
using SocialPublishing.Application.Planning;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Planning;

namespace SocialPublishing.Application.Jobs.Commands.AcceptEditorialPlanGeneration;

/// <summary>
/// Valida o pedido localmente, grava job <see cref="UserInitiatedJobStatus.Accepted"/> e outbox na mesma transação.
/// </summary>
public sealed class AcceptEditorialPlanGenerationCommandHandler
    : ICommandHandler<AcceptEditorialPlanGenerationCommand, UserInitiatedJobDto>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IBrandVoiceRepository _brandVoiceRepository;
    private readonly IEditorialPlanGenerationRepository _planGenerationRepository;
    private readonly ISocialUserInitiatedJobCommitter _jobCommitter;
    private readonly ITenantContextAccessor _tenantContextAccessor;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="AcceptEditorialPlanGenerationCommandHandler"/>.
    /// </summary>
    public AcceptEditorialPlanGenerationCommandHandler(
        IBrandVoiceRepository brandVoiceRepository,
        IEditorialPlanGenerationRepository planGenerationRepository,
        ISocialUserInitiatedJobCommitter jobCommitter,
        ITenantContextAccessor tenantContextAccessor,
        TimeProvider timeProvider)
    {
        _brandVoiceRepository = brandVoiceRepository ?? throw new ArgumentNullException(nameof(brandVoiceRepository));
        _planGenerationRepository = planGenerationRepository ?? throw new ArgumentNullException(nameof(planGenerationRepository));
        _jobCommitter = jobCommitter ?? throw new ArgumentNullException(nameof(jobCommitter));
        _tenantContextAccessor = tenantContextAccessor ?? throw new ArgumentNullException(nameof(tenantContextAccessor));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <inheritdoc />
    public async Task<Result<UserInitiatedJobDto>> Handle(
        AcceptEditorialPlanGenerationCommand request,
        CancellationToken cancellationToken)
    {
        if (request.WorkspaceId == Guid.Empty)
        {
            return Result<UserInitiatedJobDto>.Failure(SocialErrors.WorkspaceRequired);
        }

        var tenantId = _tenantContextAccessor.TenantContext?.TenantId;
        if (tenantId is null || tenantId == Guid.Empty)
        {
            return Result<UserInitiatedJobDto>.Failure(SocialErrors.TenantRequired);
        }

        var existing = await _planGenerationRepository.GetByKeyAsync(
            request.WorkspaceId,
            request.RangeStart,
            request.RangeEnd,
            request.Seed,
            cancellationToken);

        if (existing is not null)
        {
            if (existing.Status == EditorialPlanGenerationStatus.InProgress)
            {
                return Result<UserInitiatedJobDto>.Failure(SocialErrors.PlanGenerationInProgress);
            }

            if (existing.Status == EditorialPlanGenerationStatus.Completed && !existing.IsRetriableEmptyCompletion())
            {
                return Result<UserInitiatedJobDto>.Failure(
                    Error.Conflict("UserJob.PlanAlreadyCompleted", "Plano já materializado para este intervalo e semente."));
            }
        }

        var brandVoice = await _brandVoiceRepository.GetByWorkspaceAsync(request.WorkspaceId, cancellationToken);
        var planResult = EditorialCalendarPlanner.Plan(
            brandVoice,
            request.RangeStart,
            request.RangeEnd,
            request.TimeZoneId,
            request.PostsPerWeek,
            request.Seed);

        if (planResult.IsFailure)
        {
            return Result<UserInitiatedJobDto>.Failure(planResult.Error);
        }

        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        var jobId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var parameters = new EditorialPlanJobParameters(
            request.WorkspaceId,
            request.RangeStart,
            request.RangeEnd,
            request.TimeZoneId,
            request.PostsPerWeek,
            request.Seed);

        var parametersJson = JsonSerializer.Serialize(parameters, JsonOptions);
        var jobResult = UserInitiatedJob.CreateAccepted(
            jobId,
            tenantId.Value,
            request.WorkspaceId,
            UserInitiatedJobKind.EditorialPlanGenerate,
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
            UserInitiatedJobIntegrationMessageTypes.EditorialPlanGenerate,
            utcNow);

        if (outboxResult.IsFailure)
        {
            return Result<UserInitiatedJobDto>.Failure(outboxResult.Error);
        }

        var commit = await _jobCommitter.CommitAcceptedJobAsync(jobResult.Value, outboxResult.Value, cancellationToken);
        if (commit.IsFailure)
        {
            return Result<UserInitiatedJobDto>.Failure(commit.Error);
        }

        return Result<UserInitiatedJobDto>.Success(UserInitiatedJobDto.FromEntity(jobResult.Value));
    }
}