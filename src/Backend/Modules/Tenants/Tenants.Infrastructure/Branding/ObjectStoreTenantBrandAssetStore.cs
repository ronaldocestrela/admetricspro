using Amazon.S3;
using Amazon.S3.Model;
using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Application.Storage;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Infrastructure.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tenants.Application.Branding;

namespace Tenants.Infrastructure.Branding;

/// <summary>
/// Implementação de <see cref="ITenantBrandAssetStore"/> sobre <see cref="IObjectStore"/>.
/// </summary>
public sealed class ObjectStoreTenantBrandAssetStore : ITenantBrandAssetStore
{
    private readonly IObjectStore _objectStore;
    private readonly ObjectStorageOptions _options;
    private readonly ITenantContextAccessor _tenantContextAccessor;
    private readonly IAmazonS3? _s3;
    private readonly ILogger<ObjectStoreTenantBrandAssetStore> _logger;

    /// <summary>
    /// Inicializa o adaptador de ativos de branding.
    /// </summary>
    /// <param name="objectStore">Store compartilhado de objetos.</param>
    /// <param name="objectStorageOptions">Bucket de branding.</param>
    /// <param name="tenantContextAccessor">Contexto multitenant da requisição.</param>
    /// <param name="logger">Logger estruturado.</param>
    /// <param name="s3">Cliente S3 opcional (MinIO) para leitura com MIME.</param>
    public ObjectStoreTenantBrandAssetStore(
        IObjectStore objectStore,
        IOptions<ObjectStorageOptions> objectStorageOptions,
        ITenantContextAccessor tenantContextAccessor,
        ILogger<ObjectStoreTenantBrandAssetStore> logger,
        IAmazonS3? s3 = null)
    {
        _objectStore = objectStore ?? throw new ArgumentNullException(nameof(objectStore));
        _options = objectStorageOptions?.Value ?? throw new ArgumentNullException(nameof(objectStorageOptions));
        _tenantContextAccessor = tenantContextAccessor ?? throw new ArgumentNullException(nameof(tenantContextAccessor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _s3 = s3;
    }

    /// <inheritdoc />
    public string BuildObjectKey(TenantBrandAssetSlot slot)
    {
        var tenantId = RequireTenantId();
        return TenantBrandObjectKeyFormatter.Build(tenantId, slot);
    }

    /// <inheritdoc />
    public async Task<Result<string>> SaveAsync(
        TenantBrandAssetSlot slot,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var key = BuildObjectKey(slot);
        var put = await _objectStore.PutAsync(
            _options.BrandingBucket,
            key,
            content,
            contentType,
            cancellationToken);

        if (put.IsFailure)
        {
            return Result<string>.Failure(put.Error);
        }

        if (!ObjectStorageOptions.ShouldRegisterMinioHealthCheck(_options))
        {
            await WriteLocalContentTypeSidecarAsync(key, contentType, cancellationToken);
        }

        return Result<string>.Success(key);
    }

    /// <inheritdoc />
    public async Task<Result<TenantBrandAssetReadResult>> OpenReadAsync(
        string objectKey,
        CancellationToken cancellationToken = default)
    {
        var keyCheck = ValidateObjectKeyForContext(objectKey);
        if (keyCheck.IsFailure)
        {
            return Result<TenantBrandAssetReadResult>.Failure(keyCheck.Error);
        }

        if (_s3 is not null && ObjectStorageOptions.ShouldRegisterMinioHealthCheck(_options))
        {
            try
            {
                var response = await _s3.GetObjectAsync(
                    _options.BrandingBucket,
                    objectKey,
                    cancellationToken);

                var mime = string.IsNullOrWhiteSpace(response.Headers.ContentType)
                    ? "application/octet-stream"
                    : response.Headers.ContentType;

                return Result<TenantBrandAssetReadResult>.Success(
                    new TenantBrandAssetReadResult(response.ResponseStream, mime));
            }
            catch (AmazonS3Exception ex) when (IsObjectNotFound(ex))
            {
                return Result<TenantBrandAssetReadResult>.Failure(StorageErrors.ObjectNotFound);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao ler ativo de branding no MinIO. Key={Key}", objectKey);
                return Result<TenantBrandAssetReadResult>.Failure(StorageErrors.ReadFailed);
            }
        }

        var read = await _objectStore.OpenReadAsync(_options.BrandingBucket, objectKey, cancellationToken);
        if (read.IsFailure)
        {
            return Result<TenantBrandAssetReadResult>.Failure(read.Error);
        }

        var contentType = await ReadLocalContentTypeSidecarAsync(objectKey, cancellationToken)
                          ?? "application/octet-stream";
        return Result<TenantBrandAssetReadResult>.Success(
            new TenantBrandAssetReadResult(read.Value, contentType));
    }

    /// <inheritdoc />
    public async Task<Result> DeleteAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        var keyCheck = ValidateObjectKeyForContext(objectKey);
        if (keyCheck.IsFailure)
        {
            return keyCheck;
        }

        var delete = await _objectStore.DeleteAsync(_options.BrandingBucket, objectKey, cancellationToken);
        if (delete.IsFailure)
        {
            return delete;
        }

        if (!ObjectStorageOptions.ShouldRegisterMinioHealthCheck(_options))
        {
            TryDeleteLocalContentTypeSidecar(objectKey);
        }

        return Result.Success();
    }

    private Guid RequireTenantId()
    {
        var tenantId = _tenantContextAccessor.TenantContext?.TenantId;
        if (tenantId is null || tenantId == Guid.Empty)
        {
            throw new InvalidOperationException("Contexto do inquilino não resolvido para branding.");
        }

        return tenantId.Value;
    }

    private Result ValidateObjectKeyForContext(string objectKey)
    {
        var tenantId = _tenantContextAccessor.TenantContext?.TenantId;
        if (tenantId is null || tenantId == Guid.Empty)
        {
            return Result.Failure(StorageErrors.KeyInvalid);
        }

        return ObjectKey.Create(objectKey, tenantId.Value).IsFailure
            ? Result.Failure(StorageErrors.KeyInvalid)
            : Result.Success();
    }

    private string LocalSidecarPath(string objectKey) =>
        Path.Combine(
            _options.LocalRoot,
            objectKey.Replace('/', Path.DirectorySeparatorChar) + ".mime");

    private async Task WriteLocalContentTypeSidecarAsync(
        string objectKey,
        string contentType,
        CancellationToken cancellationToken)
    {
        var path = LocalSidecarPath(objectKey);
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(path, contentType, cancellationToken);
    }

    private async Task<string?> ReadLocalContentTypeSidecarAsync(string objectKey, CancellationToken cancellationToken)
    {
        var path = LocalSidecarPath(objectKey);
        if (!File.Exists(path))
        {
            return null;
        }

        var text = await File.ReadAllTextAsync(path, cancellationToken);
        return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
    }

    private void TryDeleteLocalContentTypeSidecar(string objectKey)
    {
        var path = LocalSidecarPath(objectKey);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private static bool IsObjectNotFound(AmazonS3Exception ex) =>
        ex.StatusCode == System.Net.HttpStatusCode.NotFound
        || string.Equals(ex.ErrorCode, "NoSuchKey", StringComparison.Ordinal)
        || string.Equals(ex.ErrorCode, "NotFound", StringComparison.Ordinal);
}