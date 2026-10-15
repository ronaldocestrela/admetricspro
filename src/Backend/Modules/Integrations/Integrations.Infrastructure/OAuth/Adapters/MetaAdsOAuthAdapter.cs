using System.Net.Http.Headers;
using System.Text.Json;
using BuildingBlocks.Domain.Primitives;
using Integrations.Application.Options;
using Integrations.Domain.OAuth;
using Integrations.Infrastructure.Meta;
using Microsoft.Extensions.Options;

namespace Integrations.Infrastructure.OAuth.Adapters;

/// <summary>OAuth Meta com callback fixo, validação do aplicativo e escopos efetivamente concedidos.</summary>
/// <param name="httpClient">Cliente HTTP configurado.</param>
/// <param name="options">Configurações validadas da operação.</param>
public sealed class MetaAdsOAuthAdapter(HttpClient httpClient, IOptions<MetaAdsOAuthOptions> options) : IOAuthAdapter
{
    private readonly MetaAdsOAuthOptions _options = options.Value;

    /// <inheritdoc />
    public string Platform => OAuthPlatform.MetaAds;

    /// <inheritdoc />
    public Result ValidateAuthorization(string redirectUri)
    {
        if (string.IsNullOrWhiteSpace(_options.AppId) || string.IsNullOrWhiteSpace(_options.AppSecret) ||
            !Uri.TryCreate(_options.RedirectUri, UriKind.Absolute, out var uri) ||
            (uri.Scheme != "https" && !(uri.IsLoopback && uri.Scheme == "http")))
            return Result.Failure(Error.Validation("Meta.ConfigurationMissing", "Configure App ID, App Secret e callback HTTPS da integração Meta."));
        if (!string.Equals(redirectUri, _options.RedirectUri, StringComparison.Ordinal) && !_options.AdditionalRedirectUris.Contains(redirectUri, StringComparer.Ordinal))
            return Result.Failure(Error.Validation("Meta.InvalidRedirectUri", "O callback deve corresponder ao endereço configurado no backend."));
        return Result.Success();
    }

    /// <inheritdoc />
    public string GetAuthorizationUrl(string state, string redirectUri) =>
        $"https://www.facebook.com/{_options.GraphApiVersion}/dialog/oauth?client_id={Uri.EscapeDataString(_options.AppId)}" +
        $"&redirect_uri={Uri.EscapeDataString(redirectUri)}&state={Uri.EscapeDataString(state)}" +
        $"&scope={Uri.EscapeDataString(_options.DefaultScopes)}&response_type=code";

    /// <inheritdoc />
    public async Task<Result<OAuthTokenResult>> ExchangeCodeAsync(string code, string redirectUri, CancellationToken cancellationToken = default)
    {
        var configuration = ValidateAuthorization(redirectUri);
        if (configuration.IsFailure) return Result<OAuthTokenResult>.Failure(configuration.Error);
        try
        {
            var shortResult = await ReadTokenAsync(
                $"client_id={Uri.EscapeDataString(_options.AppId)}&client_secret={Uri.EscapeDataString(_options.AppSecret)}" +
                $"&redirect_uri={Uri.EscapeDataString(redirectUri)}&code={Uri.EscapeDataString(code)}", cancellationToken);
            if (shortResult.IsFailure) return Result<OAuthTokenResult>.Failure(shortResult.Error);
            var longResult = await ReadTokenAsync(
                $"grant_type=fb_exchange_token&client_id={Uri.EscapeDataString(_options.AppId)}" +
                $"&client_secret={Uri.EscapeDataString(_options.AppSecret)}&fb_exchange_token={Uri.EscapeDataString(shortResult.Value.Token)}", cancellationToken);
            if (longResult.IsFailure) return Result<OAuthTokenResult>.Failure(longResult.Error);
            var token = longResult.Value.Token;
            using var debugRequest = new HttpRequestMessage(HttpMethod.Get,
                $"https://graph.facebook.com/{_options.GraphApiVersion}/debug_token?input_token={Uri.EscapeDataString(token)}");
            debugRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", $"{_options.AppId}|{_options.AppSecret}");
            using var debugResponse = await httpClient.SendAsync(debugRequest, cancellationToken);
            if (!debugResponse.IsSuccessStatusCode) return InvalidToken();
            using var debugJson = JsonDocument.Parse(await debugResponse.Content.ReadAsStringAsync(cancellationToken));
            if (!debugJson.RootElement.TryGetProperty("data", out var data) ||
                !data.TryGetProperty("is_valid", out var valid) || valid.ValueKind != JsonValueKind.True ||
                !data.TryGetProperty("app_id", out var appId) || appId.GetString() != _options.AppId ||
                !data.TryGetProperty("user_id", out var userId) || string.IsNullOrWhiteSpace(userId.GetString()) ||
                !data.TryGetProperty("scopes", out var scopes) || scopes.ValueKind != JsonValueKind.Array)
                return InvalidToken();
            var granted = scopes.EnumerateArray().Select(s => s.GetString()!).ToArray();
            if (!granted.Contains("ads_read", StringComparer.Ordinal))
                return Result<OAuthTokenResult>.Failure(Error.Forbidden("Meta.MissingAdsRead", "A permissão ads_read não foi concedida. Reconecte a Meta."));
            var profile = await new MetaGraphClient(httpClient, _options).GetAsync("me?fields=id,name", token, cancellationToken);
            if (profile.IsFailure) return Result<OAuthTokenResult>.Failure(profile.Error);
            if (!profile.Value.TryGetProperty("id", out var profileId) || profileId.GetString() != userId.GetString()) return InvalidToken();
            var name = profile.Value.TryGetProperty("name", out var profileName) ? profileName.GetString() : null;
            DateTime? expiry = longResult.Value.Expiry;
            if (data.TryGetProperty("expires_at", out var expires) && expires.TryGetInt64(out var seconds) && seconds > 0)
                expiry = DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime;
            if (data.TryGetProperty("data_access_expires_at", out var dataExpiry) && dataExpiry.TryGetInt64(out var ds) && ds > 0)
            {
                var accessExpiry = DateTimeOffset.FromUnixTimeSeconds(ds).UtcDateTime;
                if (expiry is null || accessExpiry < expiry) expiry = accessExpiry;
            }
            if (expiry <= DateTime.UtcNow) return InvalidToken();
            return Result<OAuthTokenResult>.Success(new(token, null, expiry, null,
                string.Join(',', granted), userId.GetString(), name));
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        { return Result<OAuthTokenResult>.Failure(Error.Failure("Meta.Timeout", "A Meta excedeu o tempo de resposta.")); }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or InvalidOperationException or FormatException or ArgumentOutOfRangeException)
        { return Result<OAuthTokenResult>.Failure(Error.Failure("MetaAds.OAuthFailed", "Não foi possível validar a autorização Meta. Inicie a conexão novamente.")); }
    }

    /// <inheritdoc />
    public Task<Result<OAuthTokenResult>> RefreshTokenAsync(string refreshTokenOrCurrentToken, CancellationToken cancellationToken = default) =>
        Task.FromResult(Result<OAuthTokenResult>.Failure(Error.Unauthorized("Meta.ReauthorizationRequired", "Tokens de usuário Meta exigem reconexão; não há refresh token OAuth para renovação indefinida.")));

    /// <inheritdoc />
    public async Task<Result> RevokeTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Delete, $"https://graph.facebook.com/{_options.GraphApiVersion}/me/permissions");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var response = await httpClient.SendAsync(request, cancellationToken);
            return response.IsSuccessStatusCode ? Result.Success() : Result.Failure(Error.Failure("Meta.RevocationFailed", "A Meta não confirmou a revogação."));
        }
        catch (HttpRequestException)
        { return Result.Failure(Error.Failure("Meta.Unavailable", "Não foi possível comunicar com a Meta.")); }
    }

    private async Task<Result<(string Token, DateTime? Expiry)>> ReadTokenAsync(string query, CancellationToken cancellationToken)
    {
        // O cliente HTTP deste adaptador tem logging de URLs desabilitado: parâmetros OAuth são sensíveis.
        using var response = await httpClient.GetAsync($"https://graph.facebook.com/{_options.GraphApiVersion}/oauth/access_token?{query}", cancellationToken);
        if (!response.IsSuccessStatusCode)
            return Result<(string, DateTime?)>.Failure(Error.Validation("MetaAds.OAuthFailed", "A Meta rejeitou o código ou a troca de token. Inicie a conexão novamente."));
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        if (!json.RootElement.TryGetProperty("access_token", out var token) || string.IsNullOrWhiteSpace(token.GetString()))
            return Result<(string, DateTime?)>.Failure(Error.Failure("Meta.InvalidResponse", "A Meta não retornou um token válido."));
        DateTime? expiry = json.RootElement.TryGetProperty("expires_in", out var expires) && expires.TryGetInt64(out var seconds) && seconds > 0
            ? DateTime.UtcNow.AddSeconds(seconds) : null;
        return Result<(string, DateTime?)>.Success((token.GetString()!, expiry));
    }

    private static Result<OAuthTokenResult> InvalidToken() => Result<OAuthTokenResult>.Failure(
        Error.Unauthorized("Meta.TokenInvalid", "A autorização não pertence ao aplicativo ou usuário esperado, expirou ou foi revogada."));
}
