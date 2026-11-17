using BuildingBlocks.Domain.Primitives;
using Microsoft.Extensions.Options;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Application.Options;

namespace SocialPublishing.Infrastructure.Media;

/// <summary>
/// Armazenamento de mídia orgânica em disco local (desenvolvimento).
/// </summary>
public sealed class LocalOrganicMediaStore : IOrganicMediaStore
{
    private readonly OrganicMediaOptions _options;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="LocalOrganicMediaStore"/>.
    /// </summary>
    /// <param name="options">Opções de mídia orgânica.</param>
    public LocalOrganicMediaStore(IOptions<OrganicMediaOptions> options)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
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
        var key = BuildKey(tenantId, workspaceId, assetId, tier);
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
            return Result<string>.Failure(
                Error.Failure("Social.MediaStorageFailed", $"Falha ao gravar mídia local: {ex.Message}"));
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

    private string BuildKey(Guid tenantId, Guid workspaceId, Guid assetId, OrganicMediaStorageTier tier)
    {
        var segment = tier == OrganicMediaStorageTier.Staging ? "staging" : "durable";
        return $"{_options.KeyPrefix}/{segment}/{tenantId:N}/{workspaceId:N}/{assetId:N}";
    }

    private string MapPath(string objectKey) =>
        Path.Combine(_options.LocalRoot, objectKey.Replace('/', Path.DirectorySeparatorChar));
}
