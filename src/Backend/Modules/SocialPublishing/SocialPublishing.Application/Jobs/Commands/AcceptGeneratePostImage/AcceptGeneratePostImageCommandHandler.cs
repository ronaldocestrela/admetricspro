using System.Text.Json;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Application.Security;
using BuildingBlocks.Domain.Messaging;
using BuildingBlocks.Domain.Primitives;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Application.Jobs;
using SocialPublishing.Application.Posts;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Posts;

namespace SocialPublishing.Application.Jobs.Commands.AcceptGeneratePostImage;

/// <summary>
/// Valida o post localmente e grava job com outbox sem invocar o gerador de imagem.
/// </summary>
public sealed class AcceptGeneratePostImageCommandHandler
    : ICommandHandler<AcceptGeneratePostImageCommand, UserInitiatedJobDto>
{
    private const int MaxDirectionLength = 500;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IOrganicPostRepository _postRepository;
    private readonly ISocialUserInitiatedJobCommitter _jobCommitter;
    private readonly ITenantContextAccessor _tenantContextAccessor;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="AcceptGeneratePostImageCommandHandler"/>.
    /// </summary>
    public AcceptGeneratePostImageCommandHandler(
        IOrganicPostRepository postRepository,
        ISocialUserInitiatedJobCommitter jobCommitter,
        ITenantContextAccessor tenantContextAccessor,
        ICurrentUserContext currentUserContext,
        TimeProvider timeProvider)
    {
        _postRepository = postRepository ?? throw new ArgumentNullException(nameof(postRepository));
        _jobCommitter = jobCommitter ?? throw new ArgumentNullException(nameof(jobCommitter));
        _tenantContextAccessor = tenantContextAccessor ?? throw new ArgumentNullException(nameof(tenantContextAccessor));
        _currentUserContext = currentUserContext ?? throw new ArgumentNullException(nameof(currentUserContext));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <inheritdoc />
    public async Task<Result<UserInitiatedJobDto>> Handle(
        AcceptGeneratePostImageCommand request,
        CancellationToken cancellationToken)
    {
        if (request.Direction is { Length: > MaxDirectionLength })
        {
            return Result<UserInitiatedJobDto>.Failure(SocialErrors.ImageGenerationDirectionTooLong);
        }

        var loadResult = await OrganicPostCommandSupport.LoadPostAsync(
            request.WorkspaceId,
            request.PostId,
            _postRepository,
            _tenantContextAccessor,
            _currentUserContext,
            cancellationToken);

        if (loadResult.IsFailure)
        {
            return Result<UserInitiatedJobDto>.Failure(loadResult.Error);
        }

        var post = loadResult.Value;
        if (post.Status != OrganicPostStatus.Approved)
        {
            return Result<UserInitiatedJobDto>.Failure(SocialErrors.InvalidTransition);
        }

        if (post.Format != OrganicPostFormat.FeedImage)
        {
            return Result<UserInitiatedJobDto>.Failure(SocialErrors.ImageGenerationFormatUnsupported);
        }

        var tenantId = _tenantContextAccessor.TenantContext!.TenantId;
        if (tenantId is null || tenantId == Guid.Empty)
        {
            return Result<UserInitiatedJobDto>.Failure(SocialErrors.TenantRequired);
        }

        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        var jobId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var parameters = new PostImageJobParameters(request.WorkspaceId, request.PostId, request.Direction?.Trim());
        var parametersJson = JsonSerializer.Serialize(parameters, JsonOptions);

        var jobResult = UserInitiatedJob.CreateAccepted(
            jobId,
            tenantId.Value,
            request.WorkspaceId,
            UserInitiatedJobKind.PostImageGenerate,
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
            UserInitiatedJobIntegrationMessageTypes.PostImageGenerate,
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