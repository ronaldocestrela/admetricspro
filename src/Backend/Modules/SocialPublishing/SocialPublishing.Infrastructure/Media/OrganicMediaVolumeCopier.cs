using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Application.Storage;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Infrastructure.MultiTenancy;
using BuildingBlocks.Infrastructure.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SocialPublishing.Application.Media;
using SocialPublishing.Application.Options;

namespace SocialPublishing.Infrastructure.Media;

/// <summary>
/// Copia arquivos do disco legado (<see cref="OrganicMediaOptions.LocalRoot"/>) para o bucket orgânico mantendo a mesma chave.
/// </summary>
public sealed class OrganicMediaVolumeCopier
{
    private readonly IObjectStore _objectStore;
    private readonly ObjectStorageOptions _objectStorageOptions;
    private readonly OrganicMediaOptions _mediaOptions;
    private readonly ITenantContextAccessor _tenantContextAccessor;
    private readonly ILogger<OrganicMediaVolumeCopier> _logger;

    /// <summary>
    /// Inicializa o copiador de volume orgânico.
    /// </summary>
    /// <param name="objectStore">Store de destino (MinIO).</param>
    /// <param name="objectStorageOptions">Bucket orgânico.</param>
    /// <param name="mediaOptions">Prefixo e layout de chave.</param>
    /// <param name="tenantContextAccessor">Contexto multitenant exigido pelo store.</param>
    /// <param name="logger">Logger estruturado (somente chaves, nunca bytes).</param>
    public OrganicMediaVolumeCopier(
        IObjectStore objectStore,
        IOptions<ObjectStorageOptions> objectStorageOptions,
        IOptions<OrganicMediaOptions> mediaOptions,
        ITenantContextAccessor tenantContextAccessor,
        ILogger<OrganicMediaVolumeCopier> logger)
    {
        _objectStore = objectStore ?? throw new ArgumentNullException(nameof(objectStore));
        _objectStorageOptions = objectStorageOptions?.Value ?? throw new ArgumentNullException(nameof(objectStorageOptions));
        _mediaOptions = mediaOptions?.Value ?? throw new ArgumentNullException(nameof(mediaOptions));
        _tenantContextAccessor = tenantContextAccessor ?? throw new ArgumentNullException(nameof(tenantContextAccessor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Enumera arquivos sob a raiz local e envia ao bucket quando necessário.
    /// </summary>
    /// <param name="localRoot">Raiz legada (<c>SocialPublishing:Media:LocalRoot</c>).</param>
    /// <param name="resolveContentType">Resolve o MIME por chave (ex.: catálogo SQL).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Contagens de cópia; não altera o SQL.</returns>
    public async Task<Result<OrganicMediaVolumeCopyReport>> CopyFromLocalVolumeAsync(
        string localRoot,
        Func<string, string> resolveContentType,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(localRoot))
        {
            return Result<OrganicMediaVolumeCopyReport>.Failure(
                Error.Validation("Social.MediaLocalRootRequired", "A raiz local de mídia orgânica é obrigatória para a cópia."));
        }

        var root = Path.GetFullPath(localRoot.Trim());
        if (!Directory.Exists(root))
        {
            return Result<OrganicMediaVolumeCopyReport>.Success(new OrganicMediaVolumeCopyReport(0, 0, 0));
        }

        var copied = 0;
        var skipped = 0;
        var failed = 0;
        var bucket = _objectStorageOptions.OrganicBucket;

        foreach (var filePath in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var relativeKey = Path.GetRelativePath(root, filePath).Replace('\\', '/');
            if (!IsAllowedRelativeKey(relativeKey))
            {
                failed++;
                _logger.LogWarning("Chave local ignorada por layout inválido: {ObjectKey}", relativeKey);
                continue;
            }

            if (!OrganicMediaObjectKeyFormatter.TryGetTenantId(
                    relativeKey,
                    _mediaOptions.KeyPrefix,
                    out var tenantId))
            {
                failed++;
                _logger.LogWarning("Chave local fora do layout orgânico: {ObjectKey}", relativeKey);
                continue;
            }

            _tenantContextAccessor.TenantContext = TenantContext.Create(
                tenantId,
                null,
                TenantResolutionSource.None);

            var fileLength = new FileInfo(filePath).Length;
            var lengthResult = await _objectStore.GetContentLengthAsync(bucket, relativeKey, cancellationToken);
            if (lengthResult.IsFailure)
            {
                failed++;
                _logger.LogWarning(
                    "Falha ao consultar tamanho no destino. Key={ObjectKey}, Code={ErrorCode}",
                    relativeKey,
                    lengthResult.Error.Code);
                continue;
            }

            if (lengthResult.Value == fileLength)
            {
                skipped++;
                _logger.LogInformation("Objeto já presente com mesmo tamanho; ignorado. Key={ObjectKey}", relativeKey);
                continue;
            }

            await using var fileStream = File.OpenRead(filePath);
            var contentType = resolveContentType(relativeKey);
            var put = await _objectStore.PutAsync(bucket, relativeKey, fileStream, contentType, cancellationToken);
            if (put.IsFailure)
            {
                failed++;
                _logger.LogWarning(
                    "Falha ao copiar objeto. Key={ObjectKey}, Code={ErrorCode}",
                    relativeKey,
                    put.Error.Code);
                continue;
            }

            copied++;
            _logger.LogInformation("Objeto copiado para o bucket orgânico. Key={ObjectKey}", relativeKey);
        }

        return Result<OrganicMediaVolumeCopyReport>.Success(new OrganicMediaVolumeCopyReport(copied, skipped, failed));
    }

    /// <summary>
    /// Conta chaves persistidas no SQL cujo arquivo não existe na raiz local.
    /// </summary>
    /// <param name="catalog">Entradas lidas do banco do tenant (somente leitura).</param>
    /// <param name="localRoot">Raiz legada.</param>
    /// <returns>Número de chaves ausentes no disco.</returns>
    public int CountMissingOnDisk(
        IReadOnlyList<OrganicMediaCatalogEntry> catalog,
        string localRoot)
    {
        if (string.IsNullOrWhiteSpace(localRoot) || catalog.Count == 0)
        {
            return 0;
        }

        var root = Path.GetFullPath(localRoot.Trim());
        var missing = 0;

        foreach (var entry in catalog)
        {
            if (string.IsNullOrWhiteSpace(entry.ObjectKey))
            {
                continue;
            }

            var path = Path.Combine(root, entry.ObjectKey.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path))
            {
                missing++;
                _logger.LogWarning("Chave no SQL sem arquivo no volume local: {ObjectKey}", entry.ObjectKey);
            }
        }

        return missing;
    }

    private static bool IsAllowedRelativeKey(string relativeKey) =>
        !string.IsNullOrWhiteSpace(relativeKey)
        && !relativeKey.Contains("..", StringComparison.Ordinal)
        && !relativeKey.StartsWith("/", StringComparison.Ordinal);
}