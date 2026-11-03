using BuildingBlocks.Domain.Primitives;
using Microsoft.Extensions.Options;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Application.Options;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Connections;

namespace SocialPublishing.Infrastructure.OAuth;

/// <summary>
/// Adaptador OAuth orgânico para TikTok Content Posting (Login Kit v2).
/// </summary>
public sealed class TikTokContentPostingAdapter : IOrganicPlatformAdapter
{
    private readonly TikTokOrganicOAuthOptions _options;

    /// <summary>
    /// Inicializa o adaptador (HttpClient reservado para troca de código na Subfase 2.2).
    /// </summary>
    /// <param name="httpClient">Cliente HTTP nomeado do módulo.</param>
    /// <param name="options">Opções OAuth orgânicas do TikTok.</param>
    public TikTokContentPostingAdapter(HttpClient httpClient, IOptions<OrganicOAuthOptions> options)
    {
        _ = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value.TikTok ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public OrganicPlatform Platform => OrganicPlatform.TikTok;

    /// <inheritdoc />
    public Result<string> BuildAuthorizationUrl(string state, string redirectUri)
    {
        if (string.IsNullOrWhiteSpace(_options.ClientKey))
        {
            return Result<string>.Failure(SocialErrors.OAuthConfigurationMissing);
        }

        var url =
            "https://www.tiktok.com/v2/auth/authorize/" +
            $"?client_key={Uri.EscapeDataString(_options.ClientKey)}" +
            $"&response_type=code" +
            $"&scope={Uri.EscapeDataString(_options.Scopes)}" +
            $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
            $"&state={Uri.EscapeDataString(state)}";

        return Result<string>.Success(url);
    }
}
