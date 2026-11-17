using BuildingBlocks.Domain.Primitives;
using Microsoft.Extensions.Options;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Application.Options;
using SocialPublishing.Domain;

namespace SocialPublishing.Infrastructure.Media;

/// <summary>
/// Monta URLs públicas HTTPS a partir de <see cref="OrganicMediaOptions.PublicBaseUrl"/> e da chave do objeto.
/// </summary>
public sealed class ConfigurableOrganicMediaPublicUrlResolver : IOrganicMediaPublicUrlResolver
{
    private readonly OrganicMediaOptions _options;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="ConfigurableOrganicMediaPublicUrlResolver"/>.
    /// </summary>
    /// <param name="options">Opções de mídia orgânica.</param>
    public ConfigurableOrganicMediaPublicUrlResolver(IOptions<OrganicMediaOptions> options)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public Result<string> ResolvePublicUrl(string objectKey)
    {
        if (string.IsNullOrWhiteSpace(objectKey))
        {
            return Result<string>.Failure(
                Error.Validation("Social.MediaObjectKeyRequired", "A chave do objeto de mídia é obrigatória."));
        }

        if (string.IsNullOrWhiteSpace(_options.PublicBaseUrl))
        {
            return Result<string>.Failure(SocialErrors.MediaPublicBaseUrlMissing);
        }

        var baseUrl = _options.PublicBaseUrl.TrimEnd('/');
        var key = objectKey.TrimStart('/');
        return Result<string>.Success($"{baseUrl}/{key}");
    }
}
