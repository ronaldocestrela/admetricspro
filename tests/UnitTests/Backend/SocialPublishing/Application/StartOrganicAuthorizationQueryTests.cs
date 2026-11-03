using System.Net;
using System.Net.Http;
using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Infrastructure.MultiTenancy;
using FluentAssertions;
using Microsoft.Extensions.Options;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Application.Connections.Queries.StartOrganicAuthorization;
using SocialPublishing.Application.Options;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Connections;
using SocialPublishing.Infrastructure.OAuth;
using Xunit;

namespace UnitTests.Backend.SocialPublishing.Application;

/// <summary>
/// Testes unitários da consulta <see cref="StartOrganicAuthorizationQueryHandler"/>.
/// </summary>
public sealed class StartOrganicAuthorizationQueryTests
{
    private const string TestSigningKey = "v3ryS3cur3AndLong3n0ughK3yForHmacSha256Signature!12345";
    private const string RedirectUri = "https://app.example.com/social/callback";
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _workspaceId = Guid.NewGuid();

    private readonly OrganicOAuthOptions _options = new()
    {
        StateSigningKey = TestSigningKey,
        Meta = new MetaOrganicOAuthOptions
        {
            AppId = "meta-app-id",
            AppSecret = "meta-secret",
            GraphApiVersion = "v26.0",
            InstagramScopes = "pages_show_list,pages_read_engagement,instagram_basic,instagram_content_publish",
            FacebookPageScopes = "pages_show_list,pages_read_engagement,pages_manage_posts",
        },
        TikTok = new TikTokOrganicOAuthOptions
        {
            ClientKey = "tiktok-client-key",
            ClientSecret = "tiktok-secret",
            Scopes = "user.info.basic,video.publish,video.upload",
        },
    };

    /// <summary>
    /// Instagram deve retornar URL HTTPS da Meta com state verificável.
    /// </summary>
    [Fact]
    public async Task Handle_Instagram_DeveRetornarUrlMetaComStateValido()
    {
        var handler = CreateHandler(_tenantId);
        var result = await handler.Handle(
            new StartOrganicAuthorizationQuery(_workspaceId, OrganicPlatform.Instagram, RedirectUri),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var uri = new Uri(result.Value.AuthorizationUrl);
        uri.Scheme.Should().Be(Uri.UriSchemeHttps);
        uri.Host.Should().Be("www.facebook.com");
        uri.AbsolutePath.Should().Be("/v26.0/dialog/oauth");
        uri.Query.Should().Contain("client_id=meta-app-id");
        uri.Query.Should().Contain("response_type=code");
        uri.Query.Should().Contain(Uri.EscapeDataString(_options.Meta.InstagramScopes));

        var stateService = new OrganicOAuthStateService(TestSigningKey);
        var unpack = stateService.Validate(result.Value.State, _tenantId, _workspaceId, DateTime.UtcNow);
        unpack.IsSuccess.Should().BeTrue();
        result.Value.ExpiresAtUtc.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(15), TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// Facebook Page deve retornar URL HTTPS da Meta com escopos de Page.
    /// </summary>
    [Fact]
    public async Task Handle_FacebookPage_DeveRetornarUrlMetaComEscoposDePage()
    {
        var handler = CreateHandler(_tenantId);
        var result = await handler.Handle(
            new StartOrganicAuthorizationQuery(_workspaceId, OrganicPlatform.FacebookPage, RedirectUri),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var uri = new Uri(result.Value.AuthorizationUrl);
        uri.Host.Should().Be("www.facebook.com");
        uri.Query.Should().Contain(Uri.EscapeDataString(_options.Meta.FacebookPageScopes));
    }

    /// <summary>
    /// TikTok deve usar Login Kit v2, não o portal da Marketing API.
    /// </summary>
    [Fact]
    public async Task Handle_TikTok_DeveRetornarUrlLoginKitV2()
    {
        var handler = CreateHandler(_tenantId);
        var result = await handler.Handle(
            new StartOrganicAuthorizationQuery(_workspaceId, OrganicPlatform.TikTok, RedirectUri),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var uri = new Uri(result.Value.AuthorizationUrl);
        uri.Scheme.Should().Be(Uri.UriSchemeHttps);
        uri.Host.Should().Be("www.tiktok.com");
        uri.AbsolutePath.Should().Be("/v2/auth/authorize/");
        uri.Query.Should().Contain("client_key=tiktok-client-key");
        uri.Query.Should().NotContain("business-api.tiktok.com");
    }

    /// <summary>
    /// Workspace vazio retorna Social.WorkspaceRequired sem emitir state.
    /// </summary>
    [Fact]
    public async Task Handle_QuandoWorkspaceVazio_DeveRetornarWorkspaceRequired()
    {
        var handler = CreateHandler(_tenantId);
        var result = await handler.Handle(
            new StartOrganicAuthorizationQuery(Guid.Empty, OrganicPlatform.Instagram, RedirectUri),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.WorkspaceRequired.Code);
    }

    /// <summary>
    /// Plataforma fora do enum suportado retorna Social.PlatformUnsupported.
    /// </summary>
    [Fact]
    public async Task Handle_QuandoPlataformaInvalida_DeveRetornarPlatformUnsupported()
    {
        var handler = CreateHandler(_tenantId);
        var invalid = (OrganicPlatform)999;
        var result = await handler.Handle(
            new StartOrganicAuthorizationQuery(_workspaceId, invalid, RedirectUri),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.PlatformUnsupported.Code);
    }

    private StartOrganicAuthorizationQueryHandler CreateHandler(Guid tenantId)
    {
        var throwingHandler = new ThrowOnSendHttpMessageHandler();
        var httpClient = new HttpClient(throwingHandler);

        var tenantAccessor = new TenantContextAccessor
        {
            TenantContext = TenantContext.Create(tenantId, null, TenantResolutionSource.Header, tenantId.ToString()),
        };

        var stateService = new OrganicOAuthStateService(TestSigningKey);
        IReadOnlyList<IOrganicPlatformAdapter> adapters =
        [
            new MetaInstagramOrganicAdapter(httpClient, Options.Create(_options)),
            new MetaFacebookPageOrganicAdapter(httpClient, Options.Create(_options)),
            new TikTokContentPostingAdapter(httpClient, Options.Create(_options)),
        ];

        return new StartOrganicAuthorizationQueryHandler(tenantAccessor, stateService, adapters);
    }

    private sealed class ThrowOnSendHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("BuildAuthorizationUrl não deve realizar chamadas HTTP.");
        }
    }
}
