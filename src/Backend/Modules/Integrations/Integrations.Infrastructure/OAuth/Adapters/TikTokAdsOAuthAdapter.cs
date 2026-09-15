using System.Net.Http.Json;
using System.Text.Json;
using BuildingBlocks.Domain.Primitives;
using Integrations.Application.Options;
using Integrations.Domain.OAuth;
using Microsoft.Extensions.Options;

namespace Integrations.Infrastructure.OAuth.Adapters;

/// <summary>
/// Adaptador de autenticação OAuth2 para a TikTok Marketing API.
/// Utiliza os endpoints de acesso da versão v1.3 do TikTok for Business Developers.
/// </summary>
public sealed class TikTokAdsOAuthAdapter : IOAuthAdapter
{
    private const string TokenEndpoint = "https://business-api.tiktok.com/open_api/v1.3/oauth2/access_token/";
    private readonly HttpClient _httpClient;
    private readonly TikTokAdsOAuthOptions _options;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="TikTokAdsOAuthAdapter"/>.
    /// </summary>
    /// <param name="httpClient">Instância de HttpClient para requisições externas.</param>
    /// <param name="options">Opções de configuração do TikTok for Business.</param>
    public TikTokAdsOAuthAdapter(HttpClient httpClient, IOptions<TikTokAdsOAuthOptions> options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public string Platform => OAuthPlatform.TikTokAds;

    /// <inheritdoc />
    public string GetAuthorizationUrl(string state, string redirectUri)
    {
        var encodedRedirectUri = Uri.EscapeDataString(redirectUri);
        var encodedState = Uri.EscapeDataString(state);

        return "https://business-api.tiktok.com/portal/auth" +
               $"?app_id={_options.AppId}" +
               $"&state={encodedState}" +
               $"&redirect_uri={encodedRedirectUri}";
    }

    /// <inheritdoc />
    public async Task<Result<OAuthTokenResult>> ExchangeCodeAsync(
        string code,
        string redirectUri,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new
            {
                app_id = _options.AppId,
                secret = _options.Secret,
                auth_code = code
            };

            var response = await _httpClient.PostAsJsonAsync(TokenEndpoint, payload, cancellationToken);
            var json = await response.Content.ReadAsStringAsync(cancellationToken);

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var responseCode = root.TryGetProperty("code", out var cProp) ? cProp.GetInt32() : -1;
            if (responseCode != 0)
            {
                var msg = root.TryGetProperty("message", out var mProp) ? mProp.GetString() : "Erro desconhecido";
                return Result<OAuthTokenResult>.Failure(
                    Error.Failure("TikTokAds.OAuthFailed", $"Falha na autenticação TikTok Ads: {msg}"));
            }

            if (!root.TryGetProperty("data", out var dataProp))
            {
                return Result<OAuthTokenResult>.Failure(
                    Error.Failure("TikTokAds.InvalidResponse", "Resposta do TikTok não continha objeto 'data'."));
            }

            var accessToken = dataProp.GetProperty("access_token").GetString() ?? string.Empty;
            var refreshToken = dataProp.TryGetProperty("refresh_token", out var rProp) ? rProp.GetString() : null;

            DateTime? expiresAt = null;
            if (dataProp.TryGetProperty("expires_in", out var expProp))
            {
                expiresAt = DateTime.UtcNow.AddSeconds(expProp.GetInt64());
            }

            string? externalAccountId = null;
            if (dataProp.TryGetProperty("advertiser_ids", out var advProp) &&
                advProp.ValueKind == JsonValueKind.Array &&
                advProp.GetArrayLength() > 0)
            {
                externalAccountId = advProp[0].GetString();
            }

            return Result<OAuthTokenResult>.Success(new OAuthTokenResult(
                AccessToken: accessToken,
                RefreshToken: refreshToken,
                AccessTokenExpiresAtUtc: expiresAt,
                RefreshTokenExpiresAtUtc: null,
                Scopes: _options.DefaultScopes,
                ExternalAccountId: externalAccountId,
                ExternalAccountName: "Conta TikTok Ads"));
        }
        catch (Exception ex)
        {
            return Result<OAuthTokenResult>.Failure(
                Error.Failure("TikTokAds.Exception", $"Exceção ao conectar à TikTok Marketing API: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<OAuthTokenResult>> RefreshTokenAsync(
        string refreshTokenOrCurrentToken,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new
            {
                app_id = _options.AppId,
                secret = _options.Secret,
                refresh_token = refreshTokenOrCurrentToken
            };

            var response = await _httpClient.PostAsJsonAsync(TokenEndpoint, payload, cancellationToken);
            var json = await response.Content.ReadAsStringAsync(cancellationToken);

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var responseCode = root.TryGetProperty("code", out var cProp) ? cProp.GetInt32() : -1;
            if (responseCode != 0)
            {
                var msg = root.TryGetProperty("message", out var mProp) ? mProp.GetString() : "Erro na renovação";
                return Result<OAuthTokenResult>.Failure(
                    Error.Failure("TikTokAds.RefreshFailed", $"Falha ao renovar token TikTok Ads: {msg}"));
            }

            var dataProp = root.GetProperty("data");
            var accessToken = dataProp.GetProperty("access_token").GetString() ?? string.Empty;
            var refreshToken = dataProp.TryGetProperty("refresh_token", out var rProp) ? rProp.GetString() : refreshTokenOrCurrentToken;

            DateTime? expiresAt = null;
            if (dataProp.TryGetProperty("expires_in", out var expProp))
            {
                expiresAt = DateTime.UtcNow.AddSeconds(expProp.GetInt64());
            }

            return Result<OAuthTokenResult>.Success(new OAuthTokenResult(
                AccessToken: accessToken,
                RefreshToken: refreshToken,
                AccessTokenExpiresAtUtc: expiresAt,
                RefreshTokenExpiresAtUtc: null,
                Scopes: _options.DefaultScopes));
        }
        catch (Exception ex)
        {
            return Result<OAuthTokenResult>.Failure(
                Error.Failure("TikTokAds.Exception", $"Erro ao renovar token com TikTok: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public Task<Result> RevokeTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Success());
    }
}
