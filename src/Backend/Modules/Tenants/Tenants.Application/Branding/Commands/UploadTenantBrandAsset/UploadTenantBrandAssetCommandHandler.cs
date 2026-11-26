using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using Tenants.Application.Branding.DTOs;
using Tenants.Application.Branding.Repositories;
using Tenants.Application.Persistence;

namespace Tenants.Application.Branding.Commands.UploadTenantBrandAsset;

/// <summary>
/// Valida e persiste upload de ativo de marca white-label.
/// </summary>
public sealed class UploadTenantBrandAssetCommandHandler
    : ICommandHandler<UploadTenantBrandAssetCommand, TenantBrandingDetailsDto>
{
    private readonly ITenantBrandingRepository _brandingRepository;
    private readonly ITenantBrandAssetStore _brandAssetStore;
    private readonly ITenantBrandPublicUrlResolver _urlResolver;
    private readonly ITenantUnitOfWork _unitOfWork;
    private readonly ITenantContextAccessor _tenantContextAccessor;

    /// <summary>
    /// Inicializa o manipulador de upload de branding.
    /// </summary>
    /// <param name="brandingRepository">Repositório de branding.</param>
    /// <param name="brandAssetStore">Store de ativos de marca.</param>
    /// <param name="urlResolver">Resolvedor de URL pública da API.</param>
    /// <param name="unitOfWork">Unidade de trabalho do tenant.</param>
    /// <param name="tenantContextAccessor">Contexto multitenant.</param>
    public UploadTenantBrandAssetCommandHandler(
        ITenantBrandingRepository brandingRepository,
        ITenantBrandAssetStore brandAssetStore,
        ITenantBrandPublicUrlResolver urlResolver,
        ITenantUnitOfWork unitOfWork,
        ITenantContextAccessor tenantContextAccessor)
    {
        _brandingRepository = brandingRepository ?? throw new ArgumentNullException(nameof(brandingRepository));
        _brandAssetStore = brandAssetStore ?? throw new ArgumentNullException(nameof(brandAssetStore));
        _urlResolver = urlResolver ?? throw new ArgumentNullException(nameof(urlResolver));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _tenantContextAccessor = tenantContextAccessor ?? throw new ArgumentNullException(nameof(tenantContextAccessor));
    }

    /// <inheritdoc />
    public async Task<Result<TenantBrandingDetailsDto>> Handle(
        UploadTenantBrandAssetCommand request,
        CancellationToken cancellationToken)
    {
        var tenantContext = _tenantContextAccessor.TenantContext;
        if (!tenantContext.IsResolved || !tenantContext.TenantId.HasValue || tenantContext.TenantId.Value == Guid.Empty)
        {
            return Result<TenantBrandingDetailsDto>.Failure(
                Error.Unauthorized("Tenant.Unresolved", "Acesso não autorizado: contexto do inquilino não identificado."));
        }

        var tenantId = tenantContext.TenantId.Value;
        var validation = TenantBrandAssetHeaderInspector.Validate(
            request.Content,
            request.ContentType,
            request.FileName,
            request.Slot);

        if (validation.IsFailure)
        {
            return Result<TenantBrandingDetailsDto>.Failure(validation.Error);
        }

        var publicUrl = _urlResolver.Resolve(tenantId, request.Slot, request.RequestOrigin);
        if (string.IsNullOrWhiteSpace(publicUrl))
        {
            return Result<TenantBrandingDetailsDto>.Failure(Error.Validation(
                "TenantBranding.PublicUrlUnavailable",
                "Não foi possível montar a URL pública do ativo. Configure ObjectStorage:PublicBaseUrl ou use HTTPS na API."));
        }

        var branding = await _brandingRepository.GetAsync(cancellationToken);
        if (branding is null)
        {
            var createResult = TenantBranding.Create(
                Guid.NewGuid(),
                "#2563EB",
                "#0F172A");
            if (createResult.IsFailure)
            {
                return Result<TenantBrandingDetailsDto>.Failure(createResult.Error);
            }

            branding = createResult.Value;
            await _brandingRepository.AddAsync(branding, cancellationToken);
        }

        var kind = TenantBrandAssetKindMapping.ToDomainKind(request.Slot);
        var previousKey = branding.GetStoredObjectKey(kind);
        var canonicalKey = _brandAssetStore.BuildObjectKey(request.Slot);

        if (request.Content.CanSeek)
        {
            request.Content.Position = 0;
        }

        var save = await _brandAssetStore.SaveAsync(
            request.Slot,
            request.Content,
            request.ContentType.Trim(),
            cancellationToken);

        if (save.IsFailure)
        {
            return Result<TenantBrandingDetailsDto>.Failure(save.Error);
        }

        var assign = branding.AssignUploadedAsset(
            kind,
            save.Value,
            request.ContentType.Trim(),
            publicUrl);

        if (assign.IsFailure)
        {
            await _brandAssetStore.DeleteAsync(save.Value, cancellationToken);
            return Result<TenantBrandingDetailsDto>.Failure(assign.Error);
        }

        await _unitOfWork.CommitAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(previousKey)
            && !string.Equals(previousKey, canonicalKey, StringComparison.Ordinal))
        {
            await _brandAssetStore.DeleteAsync(previousKey, cancellationToken);
        }

        var dto = TenantBrandingDetailsMapper.ToDetailsDto(branding, tenantId, _urlResolver, request.RequestOrigin);
        return Result<TenantBrandingDetailsDto>.Success(dto);
    }
}