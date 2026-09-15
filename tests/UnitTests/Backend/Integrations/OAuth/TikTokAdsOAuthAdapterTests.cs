using System.Net;
using FluentAssertions;
using Integrations.Application.Options;
using Integrations.Domain.OAuth;
using Integrations.Infrastructure.OAuth.Adapters;
using Microsoft.Extensions.Options;
using UnitTests.Backend.Common;

namespace UnitTests.Backend.Integrations.OAuth;

/// <summary>
/// Testes unitários para o adaptador do TikTok Marketing API <see cref="TikTokAdsOAuthAdapter"/>.
/// </summary>
public sealed class TikTokAdsOAuthAdapterTests
{
    private readonly TikTokAdsOAuthOptions _options = new()
    {
        AppId = "tiktok_app_id_123",
        Secret = "tiktok_secret_456",
        DefaultScopes = "user.info.basic,video.list,advertiser.insights"
    };

    /// <summary>
    /// Valida que a URL de autorização do TikTok for Business é montada corretamente.
    /// </summary>
    [Fact]
    public void GetAuthorizationUrl_DeveConterParametrosEsperados()
    {
        // Arrange
        var adapter = new TikTokAdsOAuthAdapter(new HttpClient(), Options.Create(_options));
        var state = "stateTikTok123";
        var redirectUri = "https://app.admetricspro.com/callback";

        // Act
        var url = adapter.GetAuthorizationUrl(state, redirectUri);

        // Assert
        url.Should().StartWith("https://business-api.tiktok.com/portal/auth");
        url.Should().Contain("app_id=tiktok_app_id_123");
        url.Should().Contain("state=stateTikTok123");
        url.Should().Contain("redirect_uri=https%3A%2F%2Fapp.admetricspro.com%2Fcallback");
    }

    /// <summary>
    /// Valida troca de auth_code retornando tokens e advertiser_id do TikTok Marketing API.
    /// </summary>
    [Fact]
    public async Task ExchangeCodeAsync_ComCodigoValido_DeveRetornarTokensEAdvertiserId()
    {
        // Arrange
        var handler = new TestHttpMessageHandler((_, _) => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """
                {
                  "code": 0,
                  "message": "OK",
                  "data": {
                    "access_token": "act.TikTokAccessToken123",
                    "expires_in": 86400,
                    "refresh_token": "rft.TikTokRefreshToken456",
                    "refresh_token_expires_in": 31536000,
                    "advertiser_ids": ["7000100020003000"],
                    "scope": [1, 2, 3]
                  }
                }
                """)
        });

        var client = new HttpClient(handler);
        var adapter = new TikTokAdsOAuthAdapter(client, Options.Create(_options));

        // Act
        var result = await adapter.ExchangeCodeAsync("valid_auth_code", "https://app.admetricspro.com/callback");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("act.TikTokAccessToken123");
        result.Value.RefreshToken.Should().Be("rft.TikTokRefreshToken456");
        result.Value.ExternalAccountId.Should().Be("7000100020003000");
    }

    /// <summary>
    /// Valida que retorno de código diferente de 0 da API do TikTok resulta em Result.Failure.
    /// </summary>
    [Fact]
    public async Task ExchangeCodeAsync_QuandoTikTokRetornaErroDeNegocio_DeveRetornarFalha()
    {
        // Arrange
        var handler = new TestHttpMessageHandler((_, _) => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """
                {
                  "code": 40001,
                  "message": "auth_code expired or invalid"
                }
                """)
        });

        var client = new HttpClient(handler);
        var adapter = new TikTokAdsOAuthAdapter(client, Options.Create(_options));

        // Act
        var result = await adapter.ExchangeCodeAsync("expired_auth_code", "https://app.admetricspro.com/callback");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("TikTokAds.OAuthFailed");
        result.Error.Description.Should().Contain("auth_code expired or invalid");
    }
}
