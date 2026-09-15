using System.Net;
using FluentAssertions;
using Integrations.Application.Options;
using Integrations.Domain.OAuth;
using Integrations.Infrastructure.OAuth.Adapters;
using Microsoft.Extensions.Options;
using UnitTests.Backend.Common;

namespace UnitTests.Backend.Integrations.OAuth;

/// <summary>
/// Testes unitários para o adaptador de autenticação do Google Ads <see cref="GoogleAdsOAuthAdapter"/>.
/// </summary>
public sealed class GoogleAdsOAuthAdapterTests
{
    private readonly GoogleAdsOAuthOptions _options = new()
    {
        ClientId = "google_client_123.apps.googleusercontent.com",
        ClientSecret = "google_secret_456",
        DeveloperToken = "dev_token_789",
        DefaultScopes = "https://www.googleapis.com/auth/adwords"
    };

    /// <summary>
    /// Valida que a URL de autorização possui os parâmetros essenciais para offline access e consent prompt.
    /// </summary>
    [Fact]
    public void GetAuthorizationUrl_DeveConterParametrosObrigatorios()
    {
        // Arrange
        var adapter = new GoogleAdsOAuthAdapter(new HttpClient(), Options.Create(_options));
        var state = "stateGoogle123";
        var redirectUri = "https://app.admetricspro.com/callback";

        // Act
        var url = adapter.GetAuthorizationUrl(state, redirectUri);

        // Assert
        url.Should().StartWith("https://accounts.google.com/o/oauth2/v2/auth");
        url.Should().Contain("client_id=google_client_123.apps.googleusercontent.com");
        url.Should().Contain("access_type=offline");
        url.Should().Contain("prompt=consent");
        url.Should().Contain("state=stateGoogle123");
        url.Should().Contain("scope=https%3A%2F%2Fwww.googleapis.com%2Fauth%2Fadwords");
    }

    /// <summary>
    /// Valida troca de código retornando access token e refresh token válidos.
    /// </summary>
    [Fact]
    public async Task ExchangeCodeAsync_ComCodigoValido_DeveRetornarTokensERefreshToken()
    {
        // Arrange
        var handler = new TestHttpMessageHandler((_, _) => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """
                {
                  "access_token": "ya29.GoogleAccessTokenSample",
                  "expires_in": 3600,
                  "refresh_token": "1//GoogleRefreshTokenSample",
                  "scope": "https://www.googleapis.com/auth/adwords",
                  "token_type": "Bearer"
                }
                """)
        });

        var client = new HttpClient(handler);
        var adapter = new GoogleAdsOAuthAdapter(client, Options.Create(_options));

        // Act
        var result = await adapter.ExchangeCodeAsync("valid_auth_code", "https://app.admetricspro.com/callback");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("ya29.GoogleAccessTokenSample");
        result.Value.RefreshToken.Should().Be("1//GoogleRefreshTokenSample");
        result.Value.AccessTokenExpiresAtUtc.Should().NotBeNull();
        result.Value.Scopes.Should().Be("https://www.googleapis.com/auth/adwords");
    }

    /// <summary>
    /// Valida renovação de token através do refresh token.
    /// </summary>
    [Fact]
    public async Task RefreshTokenAsync_ComRefreshTokenValido_DeveRenovarAccessToken()
    {
        // Arrange
        var handler = new TestHttpMessageHandler((_, _) => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """
                {
                  "access_token": "ya29.GoogleNewAccessTokenSample",
                  "expires_in": 3600,
                  "scope": "https://www.googleapis.com/auth/adwords",
                  "token_type": "Bearer"
                }
                """)
        });

        var client = new HttpClient(handler);
        var adapter = new GoogleAdsOAuthAdapter(client, Options.Create(_options));

        // Act
        var result = await adapter.RefreshTokenAsync("1//GoogleRefreshTokenSample");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("ya29.GoogleNewAccessTokenSample");
        result.Value.AccessTokenExpiresAtUtc.Should().NotBeNull();
    }

    /// <summary>
    /// Valida que erro 400 retornado pelo Google OAuth retorna falha mapeada.
    /// </summary>
    [Fact]
    public async Task ExchangeCodeAsync_ComErroDoGoogle_DeveRetornarFalhaNegocio()
    {
        // Arrange
        var handler = new TestHttpMessageHandler((_, _) => new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(
                """
                {
                  "error": "invalid_grant",
                  "error_description": "Bad Request: Token has been expired or revoked."
                }
                """)
        });

        var client = new HttpClient(handler);
        var adapter = new GoogleAdsOAuthAdapter(client, Options.Create(_options));

        // Act
        var result = await adapter.ExchangeCodeAsync("invalid_code", "https://app.admetricspro.com/callback");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("GoogleAds.OAuthFailed");
        result.Error.Description.Should().Contain("Token has been expired or revoked.");
    }
}
