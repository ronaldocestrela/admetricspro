using FluentAssertions;
using Integrations.Domain.OAuth;
using Integrations.Infrastructure.OAuth;
using NSubstitute;

namespace UnitTests.Backend.Integrations.OAuth;

/// <summary>
/// Testes unitários para o orquestrador unificado <see cref="AdNetworkAuthService"/>.
/// </summary>
public sealed class AdNetworkAuthServiceTests
{
    private readonly IOAuthAdapter _metaAdapter = Substitute.For<IOAuthAdapter>();
    private readonly IOAuthAdapter _googleAdapter = Substitute.For<IOAuthAdapter>();
    private readonly IOAuthAdapter _bingAdapter = Substitute.For<IOAuthAdapter>();
    private readonly IOAuthAdapter _tiktokAdapter = Substitute.For<IOAuthAdapter>();
    private readonly AdNetworkAuthService _service;

    /// <summary>
    /// Construtor configurando os mocks de adaptadores.
    /// </summary>
    public AdNetworkAuthServiceTests()
    {
        _metaAdapter.Platform.Returns(OAuthPlatform.MetaAds);
        _googleAdapter.Platform.Returns(OAuthPlatform.GoogleAds);
        _bingAdapter.Platform.Returns(OAuthPlatform.BingAds);
        _tiktokAdapter.Platform.Returns(OAuthPlatform.TikTokAds);

        _service = new AdNetworkAuthService([_metaAdapter, _googleAdapter, _bingAdapter, _tiktokAdapter]);
    }

    /// <summary>
    /// Valida que GetAuthorizationUrl delega corretamente para o adaptador correspondente.
    /// </summary>
    [Fact]
    public void GetAuthorizationUrl_ComPlataformaValida_DeveChamarAdaptadorCorrespondente()
    {
        // Arrange
        _metaAdapter.GetAuthorizationUrl("state123", "https://app.admetricspro.com/cb")
            .Returns("https://meta.com/oauth");

        // Act
        var result = _service.GetAuthorizationUrl(OAuthPlatform.MetaAds, "state123", "https://app.admetricspro.com/cb");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("https://meta.com/oauth");
        _metaAdapter.Received(1).GetAuthorizationUrl("state123", "https://app.admetricspro.com/cb");
    }

    /// <summary>
    /// Valida que plataforma não suportada retorna erro de negócio.
    /// </summary>
    [Fact]
    public void GetAuthorizationUrl_ComPlataformaInvalida_DeveRetornarFalha()
    {
        // Act
        var result = _service.GetAuthorizationUrl("PlataformaInexistente", "state123", "https://app.admetricspro.com/cb");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AdNetworkAuth.UnsupportedPlatform");
    }

    /// <summary>
    /// Valida que ExchangeCodeAsync despacha para o adaptador correto.
    /// </summary>
    [Fact]
    public async Task ExchangeCodeAsync_ComGoogleAds_DeveChamarAdaptadorDoGoogle()
    {
        // Arrange
        var expected = new OAuthTokenResult("google_token", "google_refresh", DateTime.UtcNow.AddHours(1), null, "scopes");
        _googleAdapter.ExchangeCodeAsync("auth_code", "https://cb", Arg.Any<CancellationToken>())
            .Returns(BuildingBlocks.Domain.Primitives.Result<OAuthTokenResult>.Success(expected));

        // Act
        var result = await _service.ExchangeCodeAsync(OAuthPlatform.GoogleAds, "auth_code", "https://cb");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("google_token");
        await _googleAdapter.Received(1).ExchangeCodeAsync("auth_code", "https://cb", Arg.Any<CancellationToken>());
    }
}
