using Analytics.Application.Reports;
using Analytics.Domain.Reports;
using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Application.Storage;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Infrastructure.Storage;
using Microsoft.Extensions.Options;

namespace Analytics.Infrastructure.Reports;

/// <summary>
/// Implementação de <see cref="IReportPdfStore"/> sobre <see cref="IObjectStore"/>.
/// </summary>
public sealed class ObjectStoreReportPdfStore : IReportPdfStore
{
    private const string PdfContentType = "application/pdf";

    private readonly IObjectStore _objectStore;
    private readonly ObjectStorageOptions _objectStorageOptions;
    private readonly ITenantContextAccessor _tenantContextAccessor;

    /// <summary>
    /// Inicializa o adaptador de PDFs de relatório.
    /// </summary>
    /// <param name="objectStore">Store compartilhado de objetos.</param>
    /// <param name="objectStorageOptions">Bucket de relatórios.</param>
    /// <param name="tenantContextAccessor">Contexto multitenant da requisição.</param>
    public ObjectStoreReportPdfStore(
        IObjectStore objectStore,
        IOptions<ObjectStorageOptions> objectStorageOptions,
        ITenantContextAccessor tenantContextAccessor)
    {
        _objectStore = objectStore ?? throw new ArgumentNullException(nameof(objectStore));
        _objectStorageOptions = objectStorageOptions?.Value ?? throw new ArgumentNullException(nameof(objectStorageOptions));
        _tenantContextAccessor = tenantContextAccessor ?? throw new ArgumentNullException(nameof(tenantContextAccessor));
    }

    /// <inheritdoc />
    public string BuildObjectKey(Guid workspaceId, Guid reportId)
    {
        var tenantId = RequireTenantId();
        return ReportPdfObjectKeyFormatter.Build(tenantId, workspaceId, reportId);
    }

    /// <inheritdoc />
    public async Task<Result<string>> SaveAsync(
        Guid workspaceId,
        Guid reportId,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        var key = BuildObjectKey(workspaceId, reportId);
        var keyCheck = ValidateObjectKeyForContext(key);
        if (keyCheck.IsFailure)
        {
            return Result<string>.Failure(keyCheck.Error);
        }

        var put = await _objectStore.PutAsync(
            _objectStorageOptions.ReportsBucket,
            key,
            content,
            PdfContentType,
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
            _objectStorageOptions.ReportsBucket,
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
            _objectStorageOptions.ReportsBucket,
            objectKey,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<long?>> GetContentLengthAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        var keyCheck = ValidateObjectKeyForContext(objectKey);
        if (keyCheck.IsFailure)
        {
            return Result<long?>.Failure(keyCheck.Error);
        }

        return await _objectStore.GetContentLengthAsync(
            _objectStorageOptions.ReportsBucket,
            objectKey,
            cancellationToken);
    }

    private Guid RequireTenantId()
    {
        var tenantId = _tenantContextAccessor.TenantContext?.TenantId;
        if (tenantId is null || tenantId == Guid.Empty)
        {
            throw new InvalidOperationException("Contexto de tenant ausente para armazenamento de PDF.");
        }

        return tenantId.Value;
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