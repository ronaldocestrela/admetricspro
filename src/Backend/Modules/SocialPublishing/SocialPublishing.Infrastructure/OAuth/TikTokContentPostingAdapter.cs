using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BuildingBlocks.Domain.Primitives;
using Microsoft.Extensions.Options;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Application.Connections;
using SocialPublishing.Application.Options;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Connections;

namespace SocialPublishing.Infrastructure.OAuth;

/// <summary>
/// Adaptador OAuth orgânico para TikTok Content Posting (Login Kit v2).
/// </summary>
public sealed class TikTokContentPostingAdapter : IOrganicPlatformAdapter
{
    private const string TokenEndpoint = "https://open.tiktokapis.com/v2/oauth/token/";
    private readonly HttpClient _httpClient;
    private readonly TikTokOrganicOAuthOptions _options;

    /// <summary>
    /// Inicializa o adaptador com cliente HTTP dedicado (sem loggers de URL).
    /// </summary>
    /// <param name="httpClient">Cliente HTTP nomeado do módulo.</param>
    /// <param name="options">Opções OAuth orgânicas do TikTok.</param>
    public TikTokContentPostingAdapter(HttpClient httpClient, IOptions<OrganicOAuthOptions> options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
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

    /// <inheritdoc />
    public async Task<Result<OrganicAuthorizationGrant>> ExchangeCodeAsync(
        string code,
        string redirectUri,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ClientKey) || string.IsNullOrWhiteSpace(_options.ClientSecret))
        {
            return Result<OrganicAuthorizationGrant>.Failure(SocialErrors.OAuthConfigurationMissing);
        }

        try
        {
            using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, TokenEndpoint)
            {
                Content = JsonContent.Create(new
                {
                    client_key = _options.ClientKey,
                    client_secret = _options.ClientSecret,
                    code,
                    grant_type = "authorization_code",
                    redirect_uri = redirectUri,
                }),
            };

            using var tokenResponse = await _httpClient.SendAsync(tokenRequest, cancellationToken);
            if (!tokenResponse.IsSuccessStatusCode)
            {
                return Result<OrganicAuthorizationGrant>.Failure(SocialErrors.OAuthExchangeFailed);
            }

            using var tokenJson = JsonDocument.Parse(await tokenResponse.Content.ReadAsStringAsync(cancellationToken));
            var root = tokenJson.RootElement;
            if (!root.TryGetProperty("access_token", out var accessTokenProp) ||
                string.IsNullOrWhiteSpace(accessTokenProp.GetString()))
            {
                return Result<OrganicAuthorizationGrant>.Failure(SocialErrors.OAuthExchangeFailed);
            }

            var accessToken = accessTokenProp.GetString()!;
            var refreshToken = root.TryGetProperty("refresh_token", out var refreshProp)
                ? refreshProp.GetString()
                : null;

            DateTime? expiresAt = null;
            if (root.TryGetProperty("expires_in", out var expiresProp) &&
                expiresProp.TryGetInt64(out var seconds) &&
                seconds > 0)
            {
                expiresAt = DateTime.UtcNow.AddSeconds(seconds);
            }

            var openId = root.TryGetProperty("open_id", out var openIdProp) ? openIdProp.GetString() : null;
            if (string.IsNullOrWhiteSpace(openId))
            {
                return Result<OrganicAuthorizationGrant>.Failure(SocialErrors.OAuthExchangeFailed);
            }

            using var profileRequest = new HttpRequestMessage(
                HttpMethod.Get,
                "https://open.tiktokapis.com/v2/user/info/?fields=open_id,display_name");
            profileRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var profileResponse = await _httpClient.SendAsync(profileRequest, cancellationToken);
            if (!profileResponse.IsSuccessStatusCode)
            {
                return Result<OrganicAuthorizationGrant>.Failure(SocialErrors.OAuthExchangeFailed);
            }

            using var profileJson = JsonDocument.Parse(await profileResponse.Content.ReadAsStringAsync(cancellationToken));
            var displayName = openId;
            if (profileJson.RootElement.TryGetProperty("data", out var data) &&
                data.TryGetProperty("user", out var user) &&
                user.TryGetProperty("display_name", out var displayNameProp) &&
                !string.IsNullOrWhiteSpace(displayNameProp.GetString()))
            {
                displayName = displayNameProp.GetString()!;
            }

            return Result<OrganicAuthorizationGrant>.Success(new OrganicAuthorizationGrant(
                accessToken,
                refreshToken,
                expiresAt,
                openId,
                displayName,
                _options.Scopes));
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Result<OrganicAuthorizationGrant>.Failure(SocialErrors.OAuthExchangeFailed);
        }
        catch (Exception)
        {
            return Result<OrganicAuthorizationGrant>.Failure(SocialErrors.OAuthExchangeFailed);
        }
    }
}
