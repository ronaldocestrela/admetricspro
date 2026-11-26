using Amazon.S3;
using Amazon.S3.Model;
using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Application.Storage;
using BuildingBlocks.Domain.Primitives;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.Infrastructure.Storage;

/// <summary>
/// Implementação de <see cref="IObjectStore"/> via MinIO (SDK S3 com path-style).
/// </summary>
public sealed class MinioObjectStore : IObjectStore
{
    private readonly IAmazonS3 _s3;
    private readonly ObjectStorageOptions _options;
    private readonly ITenantContextAccessor _tenantContextAccessor;
    private readonly ILogger<MinioObjectStore> _logger;

    /// <summary>
    /// Inicializa o store MinIO.
    /// </summary>
    /// <param name="s3">Cliente S3 configurado.</param>
    /// <param name="options">Opções de object storage.</param>
    /// <param name="tenantContextAccessor">Contexto do tenant atual.</param>
    /// <param name="logger">Logger estruturado.</param>
    public MinioObjectStore(
        IAmazonS3 s3,
        IOptions<ObjectStorageOptions> options,
        ITenantContextAccessor tenantContextAccessor,
        ILogger<MinioObjectStore> logger)
    {
        _s3 = s3 ?? throw new ArgumentNullException(nameof(s3));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _tenantContextAccessor = tenantContextAccessor ?? throw new ArgumentNullException(nameof(tenantContextAccessor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<Result> PutAsync(
        string bucket,
        string key,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var keyResult = ValidateKey(key);
        if (keyResult.IsFailure)
        {
            return Result.Failure(keyResult.Error);
        }

        var objectKey = keyResult.Value.Value;
        try
        {
            if (content.CanSeek)
            {
                content.Position = 0;
            }

            var request = new PutObjectRequest
            {
                BucketName = bucket,
                Key = objectKey,
                InputStream = content,
                ContentType = contentType,
                AutoCloseStream = false,
            };

            await _s3.PutObjectAsync(request, cancellationToken);
            return Result.Success();
        }
        catch (AmazonS3Exception ex)
        {
            LogS3Error(ex, "PutObject", bucket, objectKey);
            return Result.Failure(StorageErrors.WriteFailed);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Falha de transporte ao gravar objeto. Endpoint={Endpoint}, Bucket={Bucket}, Key={Key}",
                _options.ServiceUrl,
                bucket,
                objectKey);
            return Result.Failure(StorageErrors.WriteFailed);
        }
    }

    /// <inheritdoc />
    public async Task<Result<Stream>> OpenReadAsync(
        string bucket,
        string key,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var keyResult = ValidateKey(key);
        if (keyResult.IsFailure)
        {
            return Result<Stream>.Failure(keyResult.Error);
        }

        var objectKey = keyResult.Value.Value;
        try
        {
            var response = await _s3.GetObjectAsync(bucket, objectKey, cancellationToken);
            return Result<Stream>.Success(response.ResponseStream);
        }
        catch (AmazonS3Exception ex) when (IsObjectNotFound(ex))
        {
            return Result<Stream>.Failure(StorageErrors.ObjectNotFound);
        }
        catch (AmazonS3Exception ex)
        {
            LogS3Error(ex, "GetObject", bucket, objectKey);
            return Result<Stream>.Failure(StorageErrors.ReadFailed);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Falha de transporte ao ler objeto. Endpoint={Endpoint}, Bucket={Bucket}, Key={Key}",
                _options.ServiceUrl,
                bucket,
                objectKey);
            return Result<Stream>.Failure(StorageErrors.ReadFailed);
        }
    }

    /// <inheritdoc />
    public async Task<Result> DeleteAsync(
        string bucket,
        string key,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var keyResult = ValidateKey(key);
        if (keyResult.IsFailure)
        {
            return Result.Failure(keyResult.Error);
        }

        var objectKey = keyResult.Value.Value;
        try
        {
            await _s3.DeleteObjectAsync(bucket, objectKey, cancellationToken);
            return Result.Success();
        }
        catch (AmazonS3Exception ex)
        {
            LogS3Error(ex, "DeleteObject", bucket, objectKey);
            return Result.Failure(StorageErrors.DeleteFailed);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Falha de transporte ao excluir objeto. Endpoint={Endpoint}, Bucket={Bucket}, Key={Key}",
                _options.ServiceUrl,
                bucket,
                objectKey);
            return Result.Failure(StorageErrors.DeleteFailed);
        }
    }

    /// <inheritdoc />
    public async Task<Result<bool>> ExistsAsync(
        string bucket,
        string key,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var keyResult = ValidateKey(key);
        if (keyResult.IsFailure)
        {
            return Result<bool>.Failure(keyResult.Error);
        }

        var objectKey = keyResult.Value.Value;
        try
        {
            await _s3.GetObjectMetadataAsync(bucket, objectKey, cancellationToken);
            return Result<bool>.Success(true);
        }
        catch (AmazonS3Exception ex) when (IsObjectNotFound(ex))
        {
            return Result<bool>.Success(false);
        }
        catch (AmazonS3Exception ex)
        {
            LogS3Error(ex, "HeadObject", bucket, objectKey);
            return Result<bool>.Failure(StorageErrors.ReadFailed);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Falha de transporte ao verificar objeto. Endpoint={Endpoint}, Bucket={Bucket}, Key={Key}",
                _options.ServiceUrl,
                bucket,
                objectKey);
            return Result<bool>.Failure(StorageErrors.ReadFailed);
        }
    }

    /// <inheritdoc />
    public async Task<Result<long?>> GetContentLengthAsync(
        string bucket,
        string key,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var keyResult = ValidateKey(key);
        if (keyResult.IsFailure)
        {
            return Result<long?>.Failure(keyResult.Error);
        }

        var objectKey = keyResult.Value.Value;
        try
        {
            var metadata = await _s3.GetObjectMetadataAsync(bucket, objectKey, cancellationToken);
            return Result<long?>.Success(metadata.ContentLength);
        }
        catch (AmazonS3Exception ex) when (IsObjectNotFound(ex))
        {
            return Result<long?>.Success(null);
        }
        catch (AmazonS3Exception ex)
        {
            LogS3Error(ex, "HeadObject", bucket, objectKey);
            return Result<long?>.Failure(StorageErrors.ReadFailed);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Falha de transporte ao obter tamanho do objeto. Endpoint={Endpoint}, Bucket={Bucket}, Key={Key}",
                _options.ServiceUrl,
                bucket,
                objectKey);
            return Result<long?>.Failure(StorageErrors.ReadFailed);
        }
    }

    private Result<ObjectKey> ValidateKey(string key)
    {
        var tenantId = _tenantContextAccessor.TenantContext.TenantId;
        if (!tenantId.HasValue)
        {
            return Result<ObjectKey>.Failure(StorageErrors.KeyInvalid);
        }

        return ObjectKey.Create(key, tenantId.Value);
    }

    private void LogS3Error(AmazonS3Exception ex, string operation, string bucket, string objectKey)
    {
        _logger.LogError(
            ex,
            "Falha S3 em {Operation}. Endpoint={Endpoint}, Bucket={Bucket}, Key={Key}, Status={StatusCode}, ErrorCode={ErrorCode}",
            operation,
            _options.ServiceUrl,
            bucket,
            objectKey,
            ex.StatusCode,
            ex.ErrorCode);
    }

    private static bool IsObjectNotFound(AmazonS3Exception ex) =>
        ex.StatusCode == System.Net.HttpStatusCode.NotFound
        || string.Equals(ex.ErrorCode, "NoSuchKey", StringComparison.Ordinal)
        || string.Equals(ex.ErrorCode, "NotFound", StringComparison.Ordinal);

}