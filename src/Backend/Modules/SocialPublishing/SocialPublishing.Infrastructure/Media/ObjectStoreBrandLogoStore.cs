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
/// Porta <see cref="IBrandLogoStore"/> sobre <see cref="IObjectStore"/> (bucket orgânico).
/// </summary>
public sealed class ObjectStoreBrandLogoStore : IBrandLogoStore
{
    private readonly IObjectStore _objectStore;
    private readonly ObjectStorageOptions _objectStorageOptions;
    private readonly OrganicMediaOptions _mediaOptions;
    private readonly ITenantContextAccessor _tenantContextAccessor;

    /// <summary>
    /// Inicializa o adaptador de logo de marca.
    /// </summary>
    /// <param name="objectStore">Store compartilhado de objetos.</param>
    /// <param name="objectStorageOptions">Buckets e provedor.</param>
    /// <param name="mediaOptions">Prefixo de chave orgânica.</param>
    /// <param name="tenantContextAccessor">Contexto multitenant da requisição.</param>
    public ObjectStoreBrandLogoStore(
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
    public string BuildObjectKey(Guid tenantId, Guid workspaceId) =>
        BrandLogoObjectKeyFormatter.Build(_mediaOptions.KeyPrefix, tenantId, workspaceId);

    /// <inheritdoc />
    public async Task<Result<string>> SaveAsync(
        Guid tenantId,
        Guid workspaceId,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var tenantCheck = EnsureTenantMatchesContext(tenantId);
        if (tenantCheck.IsFailure)
        {
            return Result<string>.Failure(tenantCheck.Error);
        }

        var key = BuildObjectKey(tenantId, workspaceId);
        if (content.CanSeek)
        {
            content.Position = 0;
        }

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
        if (contextTenantId is null || contextTenantId.Value == Guid.Empty)
        {
            return Result.Failure(SocialPublishing.Domain.SocialErrors.TenantRequired);
        }

        return contextTenantId.Value == tenantId
            ? Result.Success()
            : Result.Failure(BuildingBlocks.Domain.Primitives.Error.Unauthorized(
                "Social.TenantMismatch",
                "O tenant da requisição não corresponde ao objeto solicitado."));
    }

    private Result ValidateObjectKeyForContext(string objectKey)
    {
        var contextTenantId = _tenantContextAccessor.TenantContext?.TenantId;
        if (contextTenantId is null || contextTenantId.Value == Guid.Empty)
        {
            return Result.Failure(SocialPublishing.Domain.SocialErrors.TenantRequired);
        }

        if (!BrandLogoObjectKeyFormatter.TryGetTenantId(objectKey, _mediaOptions.KeyPrefix, out var embeddedTenantId))
        {
            return Result.Failure(BuildingBlocks.Domain.Primitives.Error.Validation(
                "Social.BrandLogoKeyInvalid",
                "Chave de logo inválida."));
        }

        return embeddedTenantId == contextTenantId.Value
            ? Result.Success()
            : Result.Failure(BuildingBlocks.Domain.Primitives.Error.Unauthorized(
                "Social.TenantMismatch",
                "O tenant da requisição não corresponde ao objeto solicitado."));
    }
}