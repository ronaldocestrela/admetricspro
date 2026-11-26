using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Domain.Primitives;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Application.Brand.DTOs;
using SocialPublishing.Domain;

namespace SocialPublishing.Application.Brand.Commands.UploadBrandVoiceLogo;

/// <summary>
/// Valida e persiste a logo do cliente associada ao Brand Voice.
/// </summary>
public sealed class UploadBrandVoiceLogoCommandHandler : ICommandHandler<UploadBrandVoiceLogoCommand, BrandVoiceDto>
{
    private readonly IBrandVoiceRepository _brandVoiceRepository;
    private readonly IBrandLogoStore _brandLogoStore;
    private readonly ISocialPublishingUnitOfWork _unitOfWork;
    private readonly ITenantContextAccessor _tenantContextAccessor;

    /// <summary>
    /// Inicializa o manipulador de upload de logo do cliente.
    /// </summary>
    /// <param name="brandVoiceRepository">Repositório de brand voice.</param>
    /// <param name="brandLogoStore">Armazenamento da logo.</param>
    /// <param name="unitOfWork">Unit of work transacional.</param>
    /// <param name="tenantContextAccessor">Contexto multitenant.</param>
    public UploadBrandVoiceLogoCommandHandler(
        IBrandVoiceRepository brandVoiceRepository,
        IBrandLogoStore brandLogoStore,
        ISocialPublishingUnitOfWork unitOfWork,
        ITenantContextAccessor tenantContextAccessor)
    {
        _brandVoiceRepository = brandVoiceRepository ?? throw new ArgumentNullException(nameof(brandVoiceRepository));
        _brandLogoStore = brandLogoStore ?? throw new ArgumentNullException(nameof(brandLogoStore));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _tenantContextAccessor = tenantContextAccessor ?? throw new ArgumentNullException(nameof(tenantContextAccessor));
    }

    /// <inheritdoc />
    public async Task<Result<BrandVoiceDto>> Handle(UploadBrandVoiceLogoCommand request, CancellationToken cancellationToken)
    {
        if (request.WorkspaceId == Guid.Empty)
        {
            return Result<BrandVoiceDto>.Failure(SocialErrors.WorkspaceRequired);
        }

        var tenantId = _tenantContextAccessor.TenantContext?.TenantId;
        if (tenantId is null || tenantId == Guid.Empty)
        {
            return Result<BrandVoiceDto>.Failure(SocialErrors.TenantRequired);
        }

        var profile = await _brandVoiceRepository.GetByWorkspaceAsync(request.WorkspaceId, cancellationToken);
        if (profile is null)
        {
            return Result<BrandVoiceDto>.Failure(SocialErrors.BrandVoiceNotFound);
        }

        var validation = BrandLogoHeaderInspector.Validate(request.Content, request.ContentType, request.FileName);
        if (validation.IsFailure)
        {
            return Result<BrandVoiceDto>.Failure(validation.Error);
        }

        var normalizedMime = request.ContentType.Trim().ToLowerInvariant();
        if (normalizedMime == "image/jpg")
        {
            normalizedMime = "image/jpeg";
        }

        var previousKey = profile.LogoObjectKey;
        if (request.Content.CanSeek)
        {
            request.Content.Position = 0;
        }

        var save = await _brandLogoStore.SaveAsync(
            tenantId.Value,
            request.WorkspaceId,
            request.Content,
            normalizedMime,
            cancellationToken);

        if (save.IsFailure)
        {
            return Result<BrandVoiceDto>.Failure(save.Error);
        }

        var attach = profile.AttachLogo(save.Value, normalizedMime);
        if (attach.IsFailure)
        {
            await _brandLogoStore.DeleteAsync(save.Value, cancellationToken);
            return Result<BrandVoiceDto>.Failure(attach.Error);
        }

        await _brandVoiceRepository.UpdateAsync(profile, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(previousKey)
            && !string.Equals(previousKey, save.Value, StringComparison.Ordinal))
        {
            await _brandLogoStore.DeleteAsync(previousKey, cancellationToken);
        }

        return Result<BrandVoiceDto>.Success(BrandVoiceMapping.ToDto(profile));
    }
}