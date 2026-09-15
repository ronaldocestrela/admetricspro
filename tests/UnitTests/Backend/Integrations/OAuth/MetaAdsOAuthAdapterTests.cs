using System.Net;
using FluentAssertions;
using Integrations.Application.Options;
using Integrations.Domain.OAuth;
using Integrations.Infrastructure.OAuth.Adapters;
using Microsoft.Extensions.Options;
using UnitTests.Backend.Common;

namespace UnitTests.Backend.Integrations.OAuth;

/// <summary>
/// Testes unitários para o adaptador de autenticação do Meta Ads <see cref="MetaAdsOAuthAdapter"/>.
/// </summary>
public sealed class MetaAdsOAuthAdapterTests
{
    private readonly MetaAdsOAuthOptions _options = new()
    {
        AppId = "meta_app_12345",
        AppSecret = "meta_secret_67890",
        DefaultScopes = "ads_read,ads_management,read_insights",
        GraphApiVersion = "v21.0"
    };

    /// <summary>
    /// Valida que a URL de autorização é construída corretamente com parâmetros esperados da Meta.
    /// </summary>
    [Fact]
    public void GetAuthorizationUrl_DeveConterParametrosObrigatorios()
    {
        // Arrange
        var adapter = new MetaAdsOAuthAdapter(new HttpClient(), Options.Create(_options));
        var state = "state123";
        var redirectUri = "https://app.admetricspro.com/callback";

        // Act
        var url = adapter.GetAuthorizationUrl(state, redirectUri);

        // Assert
        url.Should().StartWith("https://www.facebook.com/v21.0/dialog/oauth");
        url.Should().Contain("client_id=meta_app_12345");
        url.Should().Contain("state=state123");
        url.Should().Contain("scope=ads_read%2Cads_management%2Cread_insights");
    }

    /// <summary>
    /// Valida o fluxo de troca de código: short-lived token seguido de troca por long-lived token de 60 dias.
    /// </summary>
    [Fact]
    public async Task ExchangeCodeAsync_ComCodigoValido_DeveRetornarLongLivedTokenComDadosDaConta()
    {
        // Arrange
        var handler = new TestHttpMessageHandler((request, _) =>
        {
            var uri = request.RequestUri!.ToString();

            if (uri.Contains("grant_type=fb_exchange_token"))
            {
                // Resposta de troca por long-lived token (60 dias = 5184000s)
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        """
                        {
                          "access_token": "EAABb123LongLivedToken60Days",
                          "token_type": "bearer",
                          "expires_in": 5184000
                        }
                        """)
                };
            }

            if (uri.Contains("me?fields="))
            {
                // Resposta do endpoint me para dados do perfil
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        """
                        {
                          "id": "100020003000",
                          "name": "Agência de Performance Alfa"
                        }
                        """)
                };
            }

            // Resposta inicial de troca de code por short-lived token
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {
                      "access_token": "EAABb123ShortLivedToken",
                      "token_type": "bearer",
                      "expires_in": 7200
                    }
                    """)
            };
        });

        var client = new HttpClient(handler);
        var adapter = new MetaAdsOAuthAdapter(client, Options.Create(_options));

        // Act
        var result = await adapter.ExchangeCodeAsync("valid_auth_code", "https://app.admetricspro.com/callback");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("EAABb123LongLivedToken60Days");
        result.Value.AccessTokenExpiresAtUtc.Should().NotBeNull();
        result.Value.AccessTokenExpiresAtUtc!.Value.Should().BeAfter(DateTime.UtcNow.AddDays(55));
        result.Value.ExternalAccountId.Should().Be("100020003000");
        result.Value.ExternalAccountName.Should().Be("Agência de Performance Alfa");
    }

    /// <summary>
    /// Valida que erro 400 retornado pelo Graph API resulta em Result.Failure com código semântico.
    /// </summary>
    [Fact]
    public async Task ExchangeCodeAsync_QuandoMetaRetornaErro_DeveRetornarFalhaNegocio()
    {
        // Arrange
        var handler = new TestHttpMessageHandler((_, _) => new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(
                """
                {
                  "error": {
                    "message": "This authorization code has been used.",
                    "type": "OAuthException",
                    "code": 100
                  }
                }
                """)
        });

        var client = new HttpClient(handler);
        var adapter = new MetaAdsOAuthAdapter(client, Options.Create(_options));

        // Act
        var result = await adapter.ExchangeCodeAsync("used_code", "https://app.admetricspro.com/callback");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("MetaAds.OAuthFailed");
        result.Error.Description.Should().Contain("This authorization code has been used.");
    }

    /// <summary>
    /// Valida renovação preventiva de token estendendo para novos 60 dias.
    /// </summary>
    [Fact]
    public async Task RefreshTokenAsync_DeveEstenderValidadeDoToken()
    {
        // Arrange
        var handler = new TestHttpMessageHandler((_, _) => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """
                {
                  "access_token": "EAABb123RefreshedLongLivedToken",
                  "token_type": "bearer",
                  "expires_in": 5184000
                }
                """)
        });

        var client = new HttpClient(handler);
        var adapter = new MetaAdsOAuthAdapter(client, Options.Create(_options));

        // Act
        var result = await adapter.RefreshTokenAsync("current_token");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("EAABb123RefreshedLongLivedToken");
        result.Value.AccessTokenExpiresAtUtc!.Value.Should().BeAfter(DateTime.UtcNow.AddDays(55));
    }
}
