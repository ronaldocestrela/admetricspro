using BuildingBlocks.Domain.Primitives;
using Microsoft.Extensions.Options;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Application.Options;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Connections;

namespace SocialPublishing.Infrastructure.OAuth;

/// <summary>
/// Adaptador OAuth orgânico para publicação em Facebook Pages via Facebook Login.
/// </summary>
public sealed class MetaFacebookPageOrganicAdapter : IOrganicPlatformAdapter
{
    private readonly MetaOrganicOAuthOptions _options;

    /// <summary>
    /// Inicializa o adaptador (HttpClient reservado para troca de código na Subfase 2.2).
    /// </summary>
    /// <param name="httpClient">Cliente HTTP nomeado do módulo.</param>
    /// <param name="options">Opções OAuth orgânicas da Meta.</param>
    public MetaFacebookPageOrganicAdapter(HttpClient httpClient, IOptions<OrganicOAuthOptions> options)
    {
        _ = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value.Meta ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public OrganicPlatform Platform => OrganicPlatform.FacebookPage;

    /// <inheritdoc />
    public Result<string> BuildAuthorizationUrl(string state, string redirectUri)
    {
        if (string.IsNullOrWhiteSpace(_options.AppId))
        {
            return Result<string>.Failure(SocialErrors.OAuthConfigurationMissing);
        }

        var url =
            $"https://www.facebook.com/{_options.GraphApiVersion}/dialog/oauth?client_id={Uri.EscapeDataString(_options.AppId)}" +
            $"&redirect_uri={Uri.EscapeDataString(redirectUri)}&state={Uri.EscapeDataString(state)}" +
            $"&scope={Uri.EscapeDataString(_options.FacebookPageScopes)}&response_type=code";

        return Result<string>.Success(url);
    }
}
