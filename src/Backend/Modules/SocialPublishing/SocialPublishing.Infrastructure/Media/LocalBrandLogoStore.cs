using BuildingBlocks.Domain.Primitives;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Application.Media;
using SocialPublishing.Application.Options;
using SocialPublishing.Domain;

namespace SocialPublishing.Infrastructure.Media;

/// <summary>
/// Armazenamento local da logo do cliente no Brand Voice (desenvolvimento).
/// </summary>
public sealed class LocalBrandLogoStore : IBrandLogoStore
{
    private readonly OrganicMediaOptions _options;
    private readonly ILogger<LocalBrandLogoStore> _logger;

    /// <summary>
    /// Inicializa o adaptador local de logo de marca.
    /// </summary>
    /// <param name="options">Opções de mídia orgânica (LocalRoot compartilhado).</param>
    /// <param name="logger">Logger estruturado.</param>
    public LocalBrandLogoStore(IOptions<OrganicMediaOptions> options, ILogger<LocalBrandLogoStore> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public string BuildObjectKey(Guid tenantId, Guid workspaceId) =>
        BrandLogoObjectKeyFormatter.Build(_options.KeyPrefix, tenantId, workspaceId);

    /// <inheritdoc />
    public async Task<Result<string>> SaveAsync(
        Guid tenantId,
        Guid workspaceId,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var key = BuildObjectKey(tenantId, workspaceId);
        var path = MapPath(key);

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await using var file = File.Create(path);
            if (content.CanSeek)
            {
                content.Position = 0;
            }

            await content.CopyToAsync(file, cancellationToken);
            return Result<string>.Success(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao gravar logo do cliente. Key={ObjectKey}", key);
            return Result<string>.Failure(
                Error.Failure("Social.BrandLogoStorageFailed", "Falha ao gravar a logo do cliente."));
        }
    }

    /// <inheritdoc />
    public Task<Result<Stream>> OpenReadAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = MapPath(objectKey);
        if (!File.Exists(path))
        {
            return Task.FromResult(Result<Stream>.Failure(SocialErrors.BrandLogoUnavailable));
        }

        Stream stream = File.OpenRead(path);
        return Task.FromResult(Result<Stream>.Success(stream));
    }

    /// <inheritdoc />
    public Task<Result> DeleteAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = MapPath(objectKey);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.FromResult(Result.Success());
    }

    private string MapPath(string objectKey) =>
        Path.Combine(_options.LocalRoot, objectKey.Replace('/', Path.DirectorySeparatorChar));
}