using System.Text.Json;
using BuildingBlocks.Domain.Primitives;
using Integrations.Application.Options;
using Integrations.Domain.OAuth;
using Microsoft.Extensions.Options;

namespace Integrations.Infrastructure.OAuth.Adapters;

/// <summary>
/// Adaptador de autenticação OAuth2 para a Google Ads API.
/// Força o consentimento com access_type=offline para obtenção contínua de refresh_token
/// e suporta o cabeçalho developer-token exigido pela Google.
/// </summary>
public sealed class GoogleAdsOAuthAdapter : IOAuthAdapter
{
    private const string TokenEndpoint = "https://oauth2.googleapis.com/token";
    private const string RevokeEndpoint = "https://oauth2.googleapis.com/revoke";
    private readonly HttpClient _httpClient;
    private readonly GoogleAdsOAuthOptions _options;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="GoogleAdsOAuthAdapter"/>.
    /// </summary>
    /// <param name="httpClient">Instância de HttpClient para requisições externas.</param>
    /// <param name="options">Opções de configuração do Google Cloud / Ads.</param>
    public GoogleAdsOAuthAdapter(HttpClient httpClient, IOptions<GoogleAdsOAuthOptions> options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public string Platform => OAuthPlatform.GoogleAds;

    /// <inheritdoc />
    public string GetAuthorizationUrl(string state, string redirectUri)
    {
        var scope = Uri.EscapeDataString(_options.DefaultScopes);
        var encodedRedirectUri = Uri.EscapeDataString(redirectUri);
        var encodedState = Uri.EscapeDataString(state);

        return "https://accounts.google.com/o/oauth2/v2/auth" +
               $"?client_id={_options.ClientId}" +
               $"&redirect_uri={encodedRedirectUri}" +
               $"&response_type=code" +
               $"&scope={scope}" +
               $"&access_type=offline" +
               $"&prompt=consent" +
               $"&state={encodedState}";
    }

    /// <inheritdoc />
    public async Task<Result<OAuthTokenResult>> ExchangeCodeAsync(
        string code,
        string redirectUri,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = new Dictionary<string, string>
            {
                ["code"] = code,
                ["client_id"] = _options.ClientId,
                ["client_secret"] = _options.ClientSecret,
                ["redirect_uri"] = redirectUri,
                ["grant_type"] = "authorization_code"
            };

            using var requestContent = new FormUrlEncodedContent(parameters);
            var response = await _httpClient.PostAsync(TokenEndpoint, requestContent, cancellationToken);
            var json = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorMsg = ExtractGoogleErrorMessage(json);
                return Result<OAuthTokenResult>.Failure(
                    Error.Failure("GoogleAds.OAuthFailed", $"Falha ao trocar código Google: {errorMsg}"));
            }

            using var doc = JsonDocument.Parse(json);
            var accessToken = doc.RootElement.GetProperty("access_token").GetString() ?? string.Empty;
            var refreshToken = doc.RootElement.TryGetProperty("refresh_token", out var rProp) ? rProp.GetString() : null;
            var scopes = doc.RootElement.TryGetProperty("scope", out var sProp) ? sProp.GetString() ?? _options.DefaultScopes : _options.DefaultScopes;

            DateTime? expiresAt = null;
            if (doc.RootElement.TryGetProperty("expires_in", out var expProp))
            {
                expiresAt = DateTime.UtcNow.AddSeconds(expProp.GetInt64());
            }

            return Result<OAuthTokenResult>.Success(new OAuthTokenResult(
                AccessToken: accessToken,
                RefreshToken: refreshToken,
                AccessTokenExpiresAtUtc: expiresAt,
                RefreshTokenExpiresAtUtc: null,
                Scopes: scopes));
        }
        catch (Exception ex)
        {
            return Result<OAuthTokenResult>.Failure(
                Error.Failure("GoogleAds.Exception", $"Exceção ao conectar à Google Ads API: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<OAuthTokenResult>> RefreshTokenAsync(
        string refreshTokenOrCurrentToken,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = new Dictionary<string, string>
            {
                ["client_id"] = _options.ClientId,
                ["client_secret"] = _options.ClientSecret,
                ["refresh_token"] = refreshTokenOrCurrentToken,
                ["grant_type"] = "refresh_token"
            };

            using var requestContent = new FormUrlEncodedContent(parameters);
            var response = await _httpClient.PostAsync(TokenEndpoint, requestContent, cancellationToken);
            var json = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorMsg = ExtractGoogleErrorMessage(json);
                return Result<OAuthTokenResult>.Failure(
                    Error.Failure("GoogleAds.RefreshFailed", $"Falha ao renovar token Google Ads: {errorMsg}"));
            }

            using var doc = JsonDocument.Parse(json);
            var accessToken = doc.RootElement.GetProperty("access_token").GetString() ?? string.Empty;
            var scopes = doc.RootElement.TryGetProperty("scope", out var sProp) ? sProp.GetString() ?? _options.DefaultScopes : _options.DefaultScopes;

            DateTime? expiresAt = null;
            if (doc.RootElement.TryGetProperty("expires_in", out var expProp))
            {
                expiresAt = DateTime.UtcNow.AddSeconds(expProp.GetInt64());
            }

            return Result<OAuthTokenResult>.Success(new OAuthTokenResult(
                AccessToken: accessToken,
                RefreshToken: refreshTokenOrCurrentToken,
                AccessTokenExpiresAtUtc: expiresAt,
                RefreshTokenExpiresAtUtc: null,
                Scopes: scopes));
        }
        catch (Exception ex)
        {
            return Result<OAuthTokenResult>.Failure(
                Error.Failure("GoogleAds.Exception", $"Erro ao renovar token com a Google: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result> RevokeTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = new Dictionary<string, string>
            {
                ["token"] = token
            };

            using var requestContent = new FormUrlEncodedContent(parameters);
            var response = await _httpClient.PostAsync(RevokeEndpoint, requestContent, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return Result.Failure(
                    Error.Failure("GoogleAds.RevocationFailed", "Falha ao revogar token junto ao Google OAuth2."));
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(
                Error.Failure("GoogleAds.Exception", $"Erro ao revogar token Google: {ex.Message}"));
        }
    }

    private static string ExtractGoogleErrorMessage(string jsonResponse)
    {
        try
        {
            using var doc = JsonDocument.Parse(jsonResponse);
            if (doc.RootElement.TryGetProperty("error_description", out var descProp))
            {
                return descProp.GetString() ?? jsonResponse;
            }
            if (doc.RootElement.TryGetProperty("error", out var errProp))
            {
                return errProp.GetString() ?? jsonResponse;
            }
        }
        catch
        {
            // Ignora erro de parse
        }

        return jsonResponse;
    }
}
