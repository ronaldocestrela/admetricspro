using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Application.Storage;
using BuildingBlocks.Domain.Primitives;

namespace Tenants.Application.Branding.Queries.GetPublicTenantBrandAsset;

/// <summary>
/// Abre stream público de logo ou favicon após fixar o tenant no contexto.
/// </summary>
public sealed class GetPublicTenantBrandAssetQueryHandler
    : IQueryHandler<GetPublicTenantBrandAssetQuery, TenantBrandAssetReadResult>
{
    private readonly ITenantBrandAssetStore _brandAssetStore;
    private readonly ITenantContextAccessor _tenantContextAccessor;

    /// <summary>
    /// Inicializa o handler de leitura pública.
    /// </summary>
    /// <param name="brandAssetStore">Store de ativos.</param>
    /// <param name="tenantContextAccessor">Contexto multitenant.</param>
    public GetPublicTenantBrandAssetQueryHandler(
        ITenantBrandAssetStore brandAssetStore,
        ITenantContextAccessor tenantContextAccessor)
    {
        _brandAssetStore = brandAssetStore ?? throw new ArgumentNullException(nameof(brandAssetStore));
        _tenantContextAccessor = tenantContextAccessor ?? throw new ArgumentNullException(nameof(tenantContextAccessor));
    }

    /// <inheritdoc />
    public async Task<Result<TenantBrandAssetReadResult>> Handle(
        GetPublicTenantBrandAssetQuery request,
        CancellationToken cancellationToken)
    {
        if (request.TenantId == Guid.Empty)
        {
            return Result<TenantBrandAssetReadResult>.Failure(
                Error.Validation("Tenant.InvalidId", "Identificador do inquilino inválido."));
        }

        var existingTenantId = _tenantContextAccessor.TenantContext?.TenantId;
        if (existingTenantId.HasValue
            && existingTenantId.Value != Guid.Empty
            && existingTenantId.Value != request.TenantId)
        {
            return Result<TenantBrandAssetReadResult>.Failure(StorageErrors.ObjectNotFound);
        }

        if (!existingTenantId.HasValue || existingTenantId == Guid.Empty)
        {
            _tenantContextAccessor.TenantContext = new ScopedTenantContext(
                request.TenantId,
                TenantResolutionSource.PublishToken,
                rawIdentifier: "public-branding");
        }

        var objectKey = TenantBrandObjectKeyFormatter.Build(request.TenantId, request.Slot);
        var read = await _brandAssetStore.OpenReadAsync(objectKey, cancellationToken);
        if (read.IsFailure)
        {
            if (read.Error.Code == StorageErrors.KeyInvalid.Code)
            {
                return Result<TenantBrandAssetReadResult>.Failure(read.Error);
            }

            return Result<TenantBrandAssetReadResult>.Failure(StorageErrors.ObjectNotFound);
        }

        return read;
    }
}