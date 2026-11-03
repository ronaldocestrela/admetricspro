using Amazon.S3;
using Amazon.S3.Model;
using BuildingBlocks.Domain.Primitives;
using Microsoft.Extensions.Options;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Application.Options;

namespace SocialPublishing.Infrastructure.Media;

/// <summary>
/// Armazenamento S3-compatível de mídia orgânica.
/// </summary>
public sealed class S3OrganicMediaStore : IOrganicMediaStore
{
    private readonly IAmazonS3 _s3;
    private readonly OrganicMediaOptions _options;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="S3OrganicMediaStore"/>.
    /// </summary>
    /// <param name="s3">Cliente S3.</param>
    /// <param name="options">Opções de mídia.</param>
    public S3OrganicMediaStore(IAmazonS3 s3, IOptions<OrganicMediaOptions> options)
    {
        _s3 = s3 ?? throw new ArgumentNullException(nameof(s3));
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
        var key = BuildKey(tenantId, workspaceId, assetId, tier);
        try
        {
            if (content.CanSeek)
            {
                content.Position = 0;
            }

            var request = new PutObjectRequest
            {
                BucketName = _options.Bucket,
                Key = key,
                InputStream = content,
                ContentType = contentType,
            };

            await _s3.PutObjectAsync(request, cancellationToken);
            return Result<string>.Success(key);
        }
        catch (Exception ex)
        {
            return Result<string>.Failure(
                Error.Failure("Social.MediaStorageFailed", $"Falha ao gravar mídia S3: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<Stream>> OpenReadAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _s3.GetObjectAsync(_options.Bucket, objectKey, cancellationToken);
            return Result<Stream>.Success(response.ResponseStream);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return Result<Stream>.Failure(
                Error.NotFound("Social.MediaObjectNotFound", "Objeto de mídia não encontrado."));
        }
        catch (Exception ex)
        {
            return Result<Stream>.Failure(
                Error.Failure("Social.MediaStorageFailed", $"Falha ao ler mídia S3: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result> DeleteAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        try
        {
            await _s3.DeleteObjectAsync(_options.Bucket, objectKey, cancellationToken);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(
                Error.Failure("Social.MediaStorageFailed", $"Falha ao excluir mídia S3: {ex.Message}"));
        }
    }

    private string BuildKey(Guid tenantId, Guid workspaceId, Guid assetId, OrganicMediaStorageTier tier)
    {
        var segment = tier == OrganicMediaStorageTier.Staging ? "staging" : "durable";
        return $"{_options.KeyPrefix}/{segment}/{tenantId:N}/{workspaceId:N}/{assetId:N}";
    }
}
