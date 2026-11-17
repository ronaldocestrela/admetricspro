using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Domain.Primitives;
using MediatR;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Application.Planning.Commands.GenerateSlotCopy;
using SocialPublishing.Application.Planning.Copy;
using SocialPublishing.Application.Planning.DTOs;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Connections;
using SocialPublishing.Domain.Planning;
using SocialPublishing.Domain.Posts;

namespace SocialPublishing.Application.Planning.Commands.GenerateEditorialPlan;

/// <summary>
/// Orquestra planner determinístico, geração de cópia por slot e persistência transacional por slot.
/// </summary>
public sealed class GenerateEditorialPlanCommandHandler : ICommandHandler<GenerateEditorialPlanCommand, GeneratedPlanDto>
{
    private const int PlannerLocalHour = 10;
    private const int PlannerLocalMinute = 0;

    private readonly IBrandVoiceRepository _brandVoiceRepository;
    private readonly IOrganicConnectionRepository _connectionRepository;
    private readonly IOrganicPostRepository _postRepository;
    private readonly IEditorialPlanGenerationRepository _planGenerationRepository;
    private readonly ISocialPublishingUnitOfWork _unitOfWork;
    private readonly ISender _sender;
    private readonly ITenantContextAccessor _tenantContextAccessor;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="GenerateEditorialPlanCommandHandler"/>.
    /// </summary>
    public GenerateEditorialPlanCommandHandler(
        IBrandVoiceRepository brandVoiceRepository,
        IOrganicConnectionRepository connectionRepository,
        IOrganicPostRepository postRepository,
        IEditorialPlanGenerationRepository planGenerationRepository,
        ISocialPublishingUnitOfWork unitOfWork,
        ISender sender,
        ITenantContextAccessor tenantContextAccessor)
    {
        _brandVoiceRepository = brandVoiceRepository ?? throw new ArgumentNullException(nameof(brandVoiceRepository));
        _connectionRepository = connectionRepository ?? throw new ArgumentNullException(nameof(connectionRepository));
        _postRepository = postRepository ?? throw new ArgumentNullException(nameof(postRepository));
        _planGenerationRepository = planGenerationRepository ?? throw new ArgumentNullException(nameof(planGenerationRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
        _tenantContextAccessor = tenantContextAccessor ?? throw new ArgumentNullException(nameof(tenantContextAccessor));
    }

    /// <inheritdoc />
    public async Task<Result<GeneratedPlanDto>> Handle(GenerateEditorialPlanCommand request, CancellationToken cancellationToken)
    {
        if (request.WorkspaceId == Guid.Empty)
        {
            return Result<GeneratedPlanDto>.Failure(SocialErrors.WorkspaceRequired);
        }

        if (_tenantContextAccessor.TenantContext is null)
        {
            return Result<GeneratedPlanDto>.Failure(SocialErrors.TenantRequired);
        }

        var existing = await _planGenerationRepository.GetByKeyAsync(
            request.WorkspaceId,
            request.RangeStart,
            request.RangeEnd,
            request.Seed,
            cancellationToken);

        if (existing is not null)
        {
            return existing.Status switch
            {
                EditorialPlanGenerationStatus.Completed =>
                    Result<GeneratedPlanDto>.Success(EditorialPlanMapping.ToDto(existing)),
                EditorialPlanGenerationStatus.InProgress =>
                    Result<GeneratedPlanDto>.Failure(SocialErrors.PlanGenerationInProgress),
                _ => Result<GeneratedPlanDto>.Failure(SocialErrors.PlanGenerationInProgress),
            };
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
            return Result<GeneratedPlanDto>.Failure(planResult.Error);
        }

        var startResult = EditorialPlanGeneration.Start(
            request.WorkspaceId,
            request.RangeStart,
            request.RangeEnd,
            request.Seed);

        if (startResult.IsFailure)
        {
            return Result<GeneratedPlanDto>.Failure(startResult.Error);
        }

        var generation = startResult.Value;
        await _planGenerationRepository.AddAsync(generation, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        var connections = await _connectionRepository.ListByWorkspaceAsync(request.WorkspaceId, cancellationToken);
        var slots = planResult.Value;

        for (var index = 0; index < slots.Count; index++)
        {
            var slot = slots[index];
            await ProcessSlotAsync(
                request,
                generation,
                slot,
                connections,
                cancellationToken);

            if (index == slots.Count - 1)
            {
                generation.Complete();
            }

            await _planGenerationRepository.UpdateAsync(generation, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);
        }

        if (slots.Count == 0)
        {
            generation.Complete();
            await _planGenerationRepository.UpdateAsync(generation, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);
        }

        return Result<GeneratedPlanDto>.Success(EditorialPlanMapping.ToDto(generation));
    }

    private async Task ProcessSlotAsync(
        GenerateEditorialPlanCommand request,
        EditorialPlanGeneration generation,
        EditorialSlot slot,
        IReadOnlyList<OrganicConnection> connections,
        CancellationToken cancellationToken)
    {
        var connection = ResolveConnection(slot.Platform, connections);
        if (connection is null)
        {
            generation.RecordFailedSlot(CreateFailedSlot(slot, SocialErrors.ConnectionRequired));
            return;
        }

        var copyCommand = new GenerateSlotCopyCommand(
            request.WorkspaceId,
            slot.LocalDate,
            slot.UtcInstant,
            slot.Pillar,
            slot.Platform,
            slot.Format);

        var copyResult = await _sender.Send(copyCommand, cancellationToken);
        if (copyResult.IsFailure)
        {
            generation.RecordFailedSlot(CreateFailedSlot(slot, copyResult.Error));
            return;
        }

        var composed = EditorialCaptionComposer.Compose(
            slot.Platform,
            copyResult.Value.Caption,
            copyResult.Value.HashtagGroups);

        if (composed.IsFailure)
        {
            generation.RecordFailedSlot(CreateFailedSlot(slot, composed.Error));
            return;
        }

        var localScheduled = new DateTime(
            slot.LocalDate.Year,
            slot.LocalDate.Month,
            slot.LocalDate.Day,
            PlannerLocalHour,
            PlannerLocalMinute,
            0,
            DateTimeKind.Unspecified);

        var postResult = OrganicPost.CreateEditorialDraft(
            request.WorkspaceId,
            connection.Id,
            slot.Platform,
            slot.Format,
            slot.Pillar,
            composed.Value,
            localScheduled,
            request.TimeZoneId);

        if (postResult.IsFailure)
        {
            generation.RecordFailedSlot(CreateFailedSlot(slot, postResult.Error));
            return;
        }

        await _postRepository.AddAsync(postResult.Value, cancellationToken);
        generation.RecordCreatedPost(postResult.Value.Id);
    }

    private static OrganicConnection? ResolveConnection(
        OrganicPlatform platform,
        IReadOnlyList<OrganicConnection> connections) =>
        connections
            .Where(c => c.Status == OrganicConnectionStatus.Active && c.Platform == platform)
            .OrderByDescending(c => c.CreatedAtUtc)
            .FirstOrDefault();

    private static EditorialPlanFailedSlot CreateFailedSlot(EditorialSlot slot, Error error) =>
        new(
            slot.LocalDate,
            slot.UtcInstant,
            slot.Pillar,
            slot.Platform,
            slot.Format,
            error.Code,
            error.Description);
}
