using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Application.Security;
using BuildingBlocks.Domain.Primitives;
using Microsoft.Extensions.Options;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Application.Options;
using SocialPublishing.Application.Planning.Images;
using SocialPublishing.Application.Posts.DTOs;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Posts;

namespace SocialPublishing.Application.Posts.Commands.GeneratePostImage;

/// <summary>
/// Orquestra geração de imagem IA, persistência no armazenamento orgânico e anexação ao post aprovado.
/// </summary>
public sealed class GeneratePostImageCommandHandler : ICommandHandler<GeneratePostImageCommand, OrganicPostDto>
{
    private const int MaxDirectionLength = 500;

    private readonly IOrganicPostRepository _postRepository;
    private readonly IBrandVoiceRepository _brandVoiceRepository;
    private readonly ISocialImageGenerator _imageGenerator;
    private readonly IGeneratedFeedImageNormalizer _feedImageNormalizer;
    private readonly IOrganicMediaStore _mediaStore;
    private readonly IOrganicMediaPublicUrlResolver _mediaPublicUrlResolver;
    private readonly ISocialPublishingUnitOfWork _unitOfWork;
    private readonly ITenantContextAccessor _tenantContextAccessor;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly SocialImageAiOptions _imageAiOptions;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="GeneratePostImageCommandHandler"/>.
    /// </summary>
    public GeneratePostImageCommandHandler(
        IOrganicPostRepository postRepository,
        IBrandVoiceRepository brandVoiceRepository,
        ISocialImageGenerator imageGenerator,
        IGeneratedFeedImageNormalizer feedImageNormalizer,
        IOrganicMediaStore mediaStore,
        IOrganicMediaPublicUrlResolver mediaPublicUrlResolver,
        ISocialPublishingUnitOfWork unitOfWork,
        ITenantContextAccessor tenantContextAccessor,
        ICurrentUserContext currentUserContext,
        IOptions<SocialImageAiOptions> imageAiOptions)
    {
        _postRepository = postRepository ?? throw new ArgumentNullException(nameof(postRepository));
        _brandVoiceRepository = brandVoiceRepository ?? throw new ArgumentNullException(nameof(brandVoiceRepository));
        _imageGenerator = imageGenerator ?? throw new ArgumentNullException(nameof(imageGenerator));
        _feedImageNormalizer = feedImageNormalizer ?? throw new ArgumentNullException(nameof(feedImageNormalizer));
        _mediaStore = mediaStore ?? throw new ArgumentNullException(nameof(mediaStore));
        _mediaPublicUrlResolver = mediaPublicUrlResolver ?? throw new ArgumentNullException(nameof(mediaPublicUrlResolver));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _tenantContextAccessor = tenantContextAccessor ?? throw new ArgumentNullException(nameof(tenantContextAccessor));
        _currentUserContext = currentUserContext ?? throw new ArgumentNullException(nameof(currentUserContext));
        _imageAiOptions = imageAiOptions?.Value ?? throw new ArgumentNullException(nameof(imageAiOptions));
    }

    /// <inheritdoc />
    public async Task<Result<OrganicPostDto>> Handle(GeneratePostImageCommand request, CancellationToken cancellationToken)
    {
        if (request.Direction is { Length: > MaxDirectionLength })
        {
            return Result<OrganicPostDto>.Failure(SocialErrors.ImageGenerationDirectionTooLong);
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
            return Result<OrganicPostDto>.Failure(loadResult.Error);
        }

        var post = loadResult.Value;
        if (post.Status != OrganicPostStatus.Approved)
        {
            return Result<OrganicPostDto>.Failure(SocialErrors.InvalidTransition);
        }

        if (post.Format != OrganicPostFormat.FeedImage)
        {
            return Result<OrganicPostDto>.Failure(SocialErrors.ImageGenerationFormatUnsupported);
        }

        var brandVoice = await _brandVoiceRepository.GetByWorkspaceAsync(request.WorkspaceId, cancellationToken);
        if (brandVoice is null)
        {
            return Result<OrganicPostDto>.Failure(SocialErrors.BrandVoiceNotFound);
        }

        var brief = ImageBriefBuilder.Build(
            brandVoice,
            post,
            _imageAiOptions.ImageSize,
            request.Direction?.Trim());

        var generated = await _imageGenerator.GenerateAsync(brief, cancellationToken);
        if (generated.IsFailure)
        {
            return Result<OrganicPostDto>.Failure(generated.Error);
        }

        var normalized = await _feedImageNormalizer.NormalizeAsync(
            generated.Value.Content,
            generated.Value.MimeType,
            brief.AspectRatio,
            cancellationToken);
        if (normalized.IsFailure)
        {
            return Result<OrganicPostDto>.Failure(normalized.Error);
        }

        var tenantId = _tenantContextAccessor.TenantContext!.TenantId;
        if (tenantId is null || tenantId == Guid.Empty)
        {
            return Result<OrganicPostDto>.Failure(SocialErrors.TenantRequired);
        }

        var previousObjectKeys = post.Media
            .Select(m => m.ObjectKey)
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Select(k => k!)
            .ToList();

        var assetId = Guid.NewGuid();
        string? durableKey = null;

        try
        {
            await using var content = new MemoryStream(normalized.Value.Content, writable: false);
            var save = await _mediaStore.SaveAsync(
                tenantId.Value,
                request.WorkspaceId,
                assetId,
                content,
                normalized.Value.MimeType,
                OrganicMediaStorageTier.Durable,
                cancellationToken);

            if (save.IsFailure)
            {
                return Result<OrganicPostDto>.Failure(save.Error);
            }

            durableKey = save.Value;

            var attach = post.ReplaceGeneratedImage(
                durableKey,
                normalized.Value.MimeType,
                normalized.Value.Width,
                normalized.Value.Height,
                assetId);

            if (attach.IsFailure)
            {
                await TryDeleteAsync(durableKey, cancellationToken);
                return Result<OrganicPostDto>.Failure(attach.Error);
            }

            await _postRepository.UpdateAsync(post, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            foreach (var oldKey in previousObjectKeys)
            {
                await TryDeleteAsync(oldKey, cancellationToken);
            }

            return Result<OrganicPostDto>.Success(
                OrganicPostMapping.ToDto(post, _mediaPublicUrlResolver));
        }
        catch
        {
            if (!string.IsNullOrWhiteSpace(durableKey))
            {
                await TryDeleteAsync(durableKey, cancellationToken);
            }

            throw;
        }
    }

    private async Task TryDeleteAsync(string objectKey, CancellationToken cancellationToken)
    {
        var delete = await _mediaStore.DeleteAsync(objectKey, cancellationToken);
        _ = delete;
    }
}