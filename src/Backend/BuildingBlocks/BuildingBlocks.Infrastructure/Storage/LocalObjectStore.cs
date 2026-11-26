using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Application.Storage;
using BuildingBlocks.Domain.Primitives;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.Infrastructure.Storage;

/// <summary>
/// Implementação de <see cref="IObjectStore"/> em disco local (Fase 3).
/// </summary>
public sealed class LocalObjectStore : IObjectStore
{
    private readonly ObjectStorageOptions _options;
    private readonly ITenantContextAccessor _tenantContextAccessor;
    private readonly ILogger<LocalObjectStore> _logger;

    /// <summary>
    /// Inicializa o store local.
    /// </summary>
    /// <param name="options">Opções de object storage.</param>
    /// <param name="tenantContextAccessor">Contexto do tenant atual.</param>
    /// <param name="logger">Logger estruturado.</param>
    public LocalObjectStore(
        IOptions<ObjectStorageOptions> options,
        ITenantContextAccessor tenantContextAccessor,
        ILogger<LocalObjectStore> logger)
    {
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

        var path = MapPath(keyResult.Value.Value);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await using var file = File.Create(path);
            if (content.CanSeek)
            {
                content.Position = 0;
            }

            await content.CopyToAsync(file, cancellationToken);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Falha ao gravar objeto local. Bucket={Bucket}, Key={Key}, Root={Root}",
                bucket,
                key,
                _options.LocalRoot);
            return Result.Failure(StorageErrors.WriteFailed);
        }
    }

    /// <inheritdoc />
    public Task<Result<Stream>> OpenReadAsync(
        string bucket,
        string key,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var keyResult = ValidateKey(key);
        if (keyResult.IsFailure)
        {
            return Task.FromResult(Result<Stream>.Failure(keyResult.Error));
        }

        var path = MapPath(keyResult.Value!.Value);
        if (!File.Exists(path))
        {
            return Task.FromResult(Result<Stream>.Failure(StorageErrors.ObjectNotFound));
        }

        try
        {
            Stream stream = File.OpenRead(path);
            return Task.FromResult(Result<Stream>.Success(stream));
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Falha ao ler objeto local. Bucket={Bucket}, Key={Key}",
                bucket,
                keyResult.Value.Value);
            return Task.FromResult(Result<Stream>.Failure(StorageErrors.ReadFailed));
        }
    }

    /// <inheritdoc />
    public Task<Result> DeleteAsync(
        string bucket,
        string key,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var keyResult = ValidateKey(key);
        if (keyResult.IsFailure)
        {
            return Task.FromResult(Result.Failure(keyResult.Error));
        }

        var path = MapPath(keyResult.Value!.Value);
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Falha ao excluir objeto local. Bucket={Bucket}, Key={Key}",
                bucket,
                keyResult.Value.Value);
            return Task.FromResult(Result.Failure(StorageErrors.DeleteFailed));
        }
    }

    /// <inheritdoc />
    public Task<Result<bool>> ExistsAsync(
        string bucket,
        string key,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var keyResult = ValidateKey(key);
        if (keyResult.IsFailure)
        {
            return Task.FromResult(Result<bool>.Failure(keyResult.Error));
        }

        var path = MapPath(keyResult.Value!.Value);
        return Task.FromResult(Result<bool>.Success(File.Exists(path)));
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

    private string MapPath(string validatedKey) =>
        Path.Combine(_options.LocalRoot, validatedKey.Replace('/', Path.DirectorySeparatorChar));
}