using System.Net;
using FluentAssertions;
using Integrations.Application.Options;
using Integrations.Domain.OAuth;
using Integrations.Infrastructure.OAuth.Adapters;
using Microsoft.Extensions.Options;
using UnitTests.Backend.Common;

namespace UnitTests.Backend.Integrations.OAuth;

/// <summary>
/// Testes unitários para o adaptador do Microsoft Advertising / Bing Ads <see cref="BingAdsOAuthAdapter"/>.
/// </summary>
public sealed class BingAdsOAuthAdapterTests
{
    private readonly BingAdsOAuthOptions _options = new()
    {
        ClientId = "bing_client_id_azure",
        ClientSecret = "bing_client_secret_azure",
        DefaultScopes = "https://ads.microsoft.com/msads.manage offline_access"
    };

    /// <summary>
    /// Valida que a URL de autorização possui os escopos corretos da Microsoft Advertising Platform.
    /// </summary>
    [Fact]
    public void GetAuthorizationUrl_DeveConterParametrosMicrosoftIdentity()
    {
        // Arrange
        var adapter = new BingAdsOAuthAdapter(new HttpClient(), Options.Create(_options));
        var state = "stateBing123";
        var redirectUri = "https://app.admetricspro.com/callback";

        // Act
        var url = adapter.GetAuthorizationUrl(state, redirectUri);

        // Assert
        url.Should().StartWith("https://login.microsoftonline.com/common/oauth2/v2.0/authorize");
        url.Should().Contain("client_id=bing_client_id_azure");
        url.Should().Contain("response_type=code");
        url.Should().Contain("state=stateBing123");
        url.Should().Contain("scope=https%3A%2F%2Fads.microsoft.com%2Fmsads.manage%20offline_access");
    }

    /// <summary>
    /// Valida troca de código retornando tokens para Microsoft Ads.
    /// </summary>
    [Fact]
    public async Task ExchangeCodeAsync_ComCodigoValido_DeveRetornarTokens()
    {
        // Arrange
        var handler = new TestHttpMessageHandler((_, _) => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """
                {
                  "token_type": "Bearer",
                  "scope": "https://ads.microsoft.com/msads.manage",
                  "expires_in": 3600,
                  "access_token": "EwB4A...MicrosoftAccessToken",
                  "refresh_token": "MCw...MicrosoftRefreshToken"
                }
                """)
        });

        var client = new HttpClient(handler);
        var adapter = new BingAdsOAuthAdapter(client, Options.Create(_options));

        // Act
        var result = await adapter.ExchangeCodeAsync("valid_code", "https://app.admetricspro.com/callback");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("EwB4A...MicrosoftAccessToken");
        result.Value.RefreshToken.Should().Be("MCw...MicrosoftRefreshToken");
        result.Value.AccessTokenExpiresAtUtc.Should().NotBeNull();
    }

    /// <summary>
    /// Valida renovação de token no Microsoft Identity Platform.
    /// </summary>
    [Fact]
    public async Task RefreshTokenAsync_DeveRenovarAccessToken()
    {
        // Arrange
        var handler = new TestHttpMessageHandler((_, _) => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """
                {
                  "token_type": "Bearer",
                  "expires_in": 3600,
                  "access_token": "EwB4A...NewMicrosoftAccessToken",
                  "refresh_token": "MCw...NewMicrosoftRefreshToken"
                }
                """)
        });

        var client = new HttpClient(handler);
        var adapter = new BingAdsOAuthAdapter(client, Options.Create(_options));

        // Act
        var result = await adapter.RefreshTokenAsync("MCw...MicrosoftRefreshToken");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("EwB4A...NewMicrosoftAccessToken");
        result.Value.RefreshToken.Should().Be("MCw...NewMicrosoftRefreshToken");
    }
}
