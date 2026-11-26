using BuildingBlocks.Domain.Primitives;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Application.Media;
using SocialPublishing.Application.Options;

namespace SocialPublishing.Infrastructure.Media;

/// <summary>
/// Armazenamento de mídia orgânica em disco local (desenvolvimento).
/// </summary>
public sealed class LocalOrganicMediaStore : IOrganicMediaStore
{
    private readonly OrganicMediaOptions _options;
    private readonly ILogger<LocalOrganicMediaStore> _logger;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="LocalOrganicMediaStore"/>.
    /// </summary>
    /// <param name="options">Opções de mídia orgânica.</param>
    /// <param name="logger">Logger estruturado.</param>
    public LocalOrganicMediaStore(IOptions<OrganicMediaOptions> options, ILogger<LocalOrganicMediaStore> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
        cancellationToken.ThrowIfCancellationRequested();
        var key = OrganicMediaObjectKeyFormatter.Build(
            _options.KeyPrefix,
            tenantId,
            workspaceId,
            assetId,
            tier);
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
            _logger.LogError(ex, "Falha ao gravar mídia orgânica local. Key={ObjectKey}", key);
            return Result<string>.Failure(
                Error.Failure("Social.MediaStorageFailed", "Falha ao gravar mídia orgânica no armazenamento local."));
        }
    }

    /// <inheritdoc />
    public Task<Result<Stream>> OpenReadAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = MapPath(objectKey);
        if (!File.Exists(path))
        {
            return Task.FromResult(Result<Stream>.Failure(
                Error.NotFound("Social.MediaObjectNotFound", "Objeto de mídia não encontrado.")));
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
