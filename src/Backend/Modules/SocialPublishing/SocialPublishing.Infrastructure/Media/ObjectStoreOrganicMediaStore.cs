using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Application.Storage;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Infrastructure.Storage;
using Microsoft.Extensions.Options;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Application.Media;
using SocialPublishing.Application.Options;

namespace SocialPublishing.Infrastructure.Media;

/// <summary>
/// Porta <see cref="IOrganicMediaStore"/> sobre <see cref="IObjectStore"/> (bucket orgânico).
/// </summary>
public sealed class ObjectStoreOrganicMediaStore : IOrganicMediaStore
{
    private readonly IObjectStore _objectStore;
    private readonly ObjectStorageOptions _objectStorageOptions;
    private readonly OrganicMediaOptions _mediaOptions;
    private readonly ITenantContextAccessor _tenantContextAccessor;

    /// <summary>
    /// Inicializa o adaptador de mídia orgânica.
    /// </summary>
    /// <param name="objectStore">Store compartilhado de objetos.</param>
    /// <param name="objectStorageOptions">Buckets e provedor.</param>
    /// <param name="mediaOptions">Prefixo de chave orgânica.</param>
    /// <param name="tenantContextAccessor">Contexto multitenant da requisição.</param>
    public ObjectStoreOrganicMediaStore(
        IObjectStore objectStore,
        IOptions<ObjectStorageOptions> objectStorageOptions,
        IOptions<OrganicMediaOptions> mediaOptions,
        ITenantContextAccessor tenantContextAccessor)
    {
        _objectStore = objectStore ?? throw new ArgumentNullException(nameof(objectStore));
        _objectStorageOptions = objectStorageOptions?.Value ?? throw new ArgumentNullException(nameof(objectStorageOptions));
        _mediaOptions = mediaOptions?.Value ?? throw new ArgumentNullException(nameof(mediaOptions));
        _tenantContextAccessor = tenantContextAccessor ?? throw new ArgumentNullException(nameof(tenantContextAccessor));
    }

    /// <inheritdoc />
    public async Task<Result<string>> SaveAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid assetId,
        Stream content,
        string contentType,
        OrganicMediaStorageTier tier,
        CancellationToken cancellationToken = default)
    {
        var tenantCheck = EnsureTenantMatchesContext(tenantId);
        if (tenantCheck.IsFailure)
        {
            return Result<string>.Failure(tenantCheck.Error);
        }

        var key = OrganicMediaObjectKeyFormatter.Build(
            _mediaOptions.KeyPrefix,
            tenantId,
            workspaceId,
            assetId,
            tier);

        var put = await _objectStore.PutAsync(
            _objectStorageOptions.OrganicBucket,
            key,
            content,
            contentType,
            cancellationToken);

        return put.IsFailure
            ? Result<string>.Failure(put.Error)
            : Result<string>.Success(key);
    }

    /// <inheritdoc />
    public async Task<Result<Stream>> OpenReadAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        var keyCheck = ValidateObjectKeyForContext(objectKey);
        if (keyCheck.IsFailure)
        {
            return Result<Stream>.Failure(keyCheck.Error);
        }

        return await _objectStore.OpenReadAsync(
            _objectStorageOptions.OrganicBucket,
            objectKey,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result> DeleteAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        var keyCheck = ValidateObjectKeyForContext(objectKey);
        if (keyCheck.IsFailure)
        {
            return keyCheck;
        }

        return await _objectStore.DeleteAsync(
            _objectStorageOptions.OrganicBucket,
            objectKey,
            cancellationToken);
    }

    private Result EnsureTenantMatchesContext(Guid tenantId)
    {
        var contextTenantId = _tenantContextAccessor.TenantContext?.TenantId;
        if (contextTenantId is null || contextTenantId == Guid.Empty)
        {
            return Result.Failure(StorageErrors.KeyInvalid);
        }

        if (contextTenantId.Value != tenantId)
        {
            return Result.Failure(StorageErrors.KeyInvalid);
        }

        return Result.Success();
    }

    private Result ValidateObjectKeyForContext(string objectKey)
    {
        var contextTenantId = _tenantContextAccessor.TenantContext?.TenantId;
        if (contextTenantId is null || contextTenantId == Guid.Empty)
        {
            return Result.Failure(StorageErrors.KeyInvalid);
        }

        return ObjectKey.Create(objectKey, contextTenantId.Value).IsFailure
            ? Result.Failure(StorageErrors.KeyInvalid)
            : Result.Success();
    }
}