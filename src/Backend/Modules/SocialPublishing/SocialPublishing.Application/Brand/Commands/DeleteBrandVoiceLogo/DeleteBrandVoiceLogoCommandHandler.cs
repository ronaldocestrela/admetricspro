using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Domain.Primitives;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Application.Brand.DTOs;
using SocialPublishing.Domain;

namespace SocialPublishing.Application.Brand.Commands.DeleteBrandVoiceLogo;

/// <summary>
/// Remove metadados e objeto da logo do cliente.
/// </summary>
public sealed class DeleteBrandVoiceLogoCommandHandler : ICommandHandler<DeleteBrandVoiceLogoCommand, BrandVoiceDto>
{
    private readonly IBrandVoiceRepository _brandVoiceRepository;
    private readonly IBrandLogoStore _brandLogoStore;
    private readonly ISocialPublishingUnitOfWork _unitOfWork;
    private readonly ITenantContextAccessor _tenantContextAccessor;

    /// <summary>
    /// Inicializa o manipulador de remoção de logo do cliente.
    /// </summary>
    /// <param name="brandVoiceRepository">Repositório de brand voice.</param>
    /// <param name="brandLogoStore">Armazenamento da logo.</param>
    /// <param name="unitOfWork">Unit of work transacional.</param>
    /// <param name="tenantContextAccessor">Contexto multitenant.</param>
    public DeleteBrandVoiceLogoCommandHandler(
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
    public async Task<Result<BrandVoiceDto>> Handle(DeleteBrandVoiceLogoCommand request, CancellationToken cancellationToken)
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

        if (!profile.HasLogo)
        {
            return Result<BrandVoiceDto>.Failure(SocialErrors.BrandLogoNotFound);
        }

        var objectKey = profile.LogoObjectKey!;
        var clear = profile.ClearLogo();
        if (clear.IsFailure)
        {
            return Result<BrandVoiceDto>.Failure(clear.Error);
        }

        await _brandVoiceRepository.UpdateAsync(profile, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        await _brandLogoStore.DeleteAsync(objectKey, cancellationToken);

        return Result<BrandVoiceDto>.Success(BrandVoiceMapping.ToDto(profile));
    }
}