using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Domain.Primitives;
using Tenants.Application.Branding.DTOs;
using Tenants.Application.Branding.Repositories;
using Tenants.Application.Persistence;

namespace Tenants.Application.Branding.Commands.DeleteTenantBrandAsset;

/// <summary>
/// Remove ativo de marca do bucket e zera o slot no SQL.
/// </summary>
public sealed class DeleteTenantBrandAssetCommandHandler
    : ICommandHandler<DeleteTenantBrandAssetCommand, TenantBrandingDetailsDto>
{
    private readonly ITenantBrandingRepository _brandingRepository;
    private readonly ITenantBrandAssetStore _brandAssetStore;
    private readonly ITenantBrandPublicUrlResolver _urlResolver;
    private readonly ITenantUnitOfWork _unitOfWork;
    private readonly ITenantContextAccessor _tenantContextAccessor;

    /// <summary>
    /// Inicializa o manipulador de exclusão de ativo de branding.
    /// </summary>
    /// <param name="brandingRepository">Repositório de branding.</param>
    /// <param name="brandAssetStore">Store de ativos de marca.</param>
    /// <param name="urlResolver">Resolvedor de URL pública.</param>
    /// <param name="unitOfWork">Unidade de trabalho do tenant.</param>
    /// <param name="tenantContextAccessor">Contexto multitenant.</param>
    public DeleteTenantBrandAssetCommandHandler(
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
        DeleteTenantBrandAssetCommand request,
        CancellationToken cancellationToken)
    {
        var tenantContext = _tenantContextAccessor.TenantContext;
        if (!tenantContext.IsResolved || !tenantContext.TenantId.HasValue || tenantContext.TenantId.Value == Guid.Empty)
        {
            return Result<TenantBrandingDetailsDto>.Failure(
                Error.Unauthorized("Tenant.Unresolved", "Acesso não autorizado: contexto do inquilino não identificado."));
        }

        var tenantId = tenantContext.TenantId.Value;
        var branding = await _brandingRepository.GetAsync(cancellationToken);
        if (branding is null)
        {
            return Result<TenantBrandingDetailsDto>.Failure(
                Error.NotFound("TenantBranding.NotFound", "Configuração de marca não encontrada."));
        }

        var kind = TenantBrandAssetKindMapping.ToDomainKind(request.Slot);
        var objectKey = branding.GetStoredObjectKey(kind);
        if (!string.IsNullOrWhiteSpace(objectKey))
        {
            _ = await _brandAssetStore.DeleteAsync(objectKey, cancellationToken);
        }

        branding.ClearStoredAsset(kind);
        await _unitOfWork.CommitAsync(cancellationToken);

        var dto = TenantBrandingDetailsMapper.ToDetailsDto(branding, tenantId, _urlResolver, request.RequestOrigin);
        return Result<TenantBrandingDetailsDto>.Success(dto);
    }
}