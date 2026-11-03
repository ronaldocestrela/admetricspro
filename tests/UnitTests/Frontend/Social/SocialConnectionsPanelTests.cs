using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using SocialPublishing.Application.Connections.Commands.CompleteOrganicAuthorization;
using SocialPublishing.Application.Connections.DTOs;
using SocialPublishing.Application.Connections.Queries.StartOrganicAuthorization;
using SocialPublishing.Domain.Connections;
using UnitTests.Frontend.Common;
using WebApp.Components.Social;
using WebApp.Services.Social;
using Xunit;

namespace UnitTests.Frontend.Social;

/// <summary>
/// Testes bUnit do painel <see cref="SocialConnectionsPanel"/>.
/// </summary>
public sealed class SocialConnectionsPanelTests : BunitTestBase
{
    private readonly ISocialPublishingClient _socialClient = Substitute.For<ISocialPublishingClient>();

    /// <summary>
    /// Registra mock do cliente social.
    /// </summary>
    public SocialConnectionsPanelTests()
    {
        Services.AddSingleton(_socialClient);
    }

    /// <summary>
    /// Lista vazia exibe estado vazio e três botões de conectar.
    /// </summary>
    [Fact]
    public void Panel_WhenEmpty_ShouldShowEmptyStateAndThreeConnectButtons()
    {
        var workspaceId = Guid.NewGuid();
        _socialClient.ListConnectionsAsync(workspaceId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<OrganicConnectionSummaryDto>>.Success(Array.Empty<OrganicConnectionSummaryDto>())));

        var cut = Render<SocialConnectionsPanel>(p => p.Add(x => x.WorkspaceId, workspaceId));

        cut.Find("[data-testid='social-connections-empty']").Should().NotBeNull();
        cut.Find("[data-testid='connect-instagram']").Should().NotBeNull();
        cut.Find("[data-testid='connect-facebook-page']").Should().NotBeNull();
        cut.Find("[data-testid='connect-tiktok']").Should().NotBeNull();
    }

    /// <summary>
    /// Clique em conectar chama StartAuthorizationAsync e navega para a URL devolvida.
    /// </summary>
    [Fact]
    public async Task Panel_WhenConnectClicked_ShouldCallClientAndNavigate()
    {
        var workspaceId = Guid.NewGuid();
        _socialClient.ListConnectionsAsync(workspaceId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<OrganicConnectionSummaryDto>>.Success(Array.Empty<OrganicConnectionSummaryDto>())));

        _socialClient.StartAuthorizationAsync(
                workspaceId,
                OrganicPlatform.Instagram,
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<StartOrganicAuthorizationResponse>.Success(
                new StartOrganicAuthorizationResponse("https://provider.test/oauth", "state", DateTime.UtcNow.AddMinutes(15)))));

        var cut = Render<SocialConnectionsPanel>(p => p.Add(x => x.WorkspaceId, workspaceId));
        var nav = Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>();
        var uriBefore = nav.Uri;

        await cut.Find("[data-testid='connect-instagram']").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        await _socialClient.Received(1).StartAuthorizationAsync(
            workspaceId,
            OrganicPlatform.Instagram,
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());

        nav.Uri.Should().Be("https://provider.test/oauth");
        nav.Uri.Should().NotBe(uriBefore);
    }

    /// <summary>
    /// Falha ao conectar exibe descrição do erro e não navega.
    /// </summary>
    [Fact]
    public async Task Panel_WhenConnectFails_ShouldShowErrorAndNotNavigate()
    {
        var workspaceId = Guid.NewGuid();
        _socialClient.ListConnectionsAsync(workspaceId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<OrganicConnectionSummaryDto>>.Success(Array.Empty<OrganicConnectionSummaryDto>())));

        _socialClient.StartAuthorizationAsync(
                Arg.Any<Guid>(),
                Arg.Any<OrganicPlatform>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<StartOrganicAuthorizationResponse>.Failure(
                Error.Validation("Social.OAuthConfigurationMissing", "Configuração OAuth incompleta."))));

        var cut = Render<SocialConnectionsPanel>(p => p.Add(x => x.WorkspaceId, workspaceId));
        var nav = Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>();
        var uriBefore = nav.Uri;

        await cut.Find("[data-testid='connect-instagram']").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        cut.Find("[data-testid='social-connections-error']").TextContent.Should().Contain("Configuração OAuth incompleta");
        nav.Uri.Should().Be(uriBefore);
    }

    /// <summary>
    /// Cartão conectado oferece revogar e não renderiza token.
    /// </summary>
    [Fact]
    public void Panel_WhenConnected_ShouldOfferRevokeWithoutTokenInMarkup()
    {
        var workspaceId = Guid.NewGuid();
        const string secretMarker = "cipher-must-not-appear";
        var connection = new OrganicConnectionSummaryDto(
            Guid.NewGuid(),
            workspaceId,
            OrganicPlatform.FacebookPage,
            "Page Oficial",
            "page-1",
            OrganicConnectionStatus.Active,
            DateTime.UtcNow.AddDays(2));

        _socialClient.ListConnectionsAsync(workspaceId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<OrganicConnectionSummaryDto>>.Success(new[] { connection })));

        var cut = Render<SocialConnectionsPanel>(p => p.Add(x => x.WorkspaceId, workspaceId));

        cut.Find("[data-testid='revoke-FacebookPage']").Should().NotBeNull();
        cut.Markup.Should().NotContain(secretMarker);
        cut.Markup.Should().NotContain("EncryptedAccessToken");
    }

    /// <summary>
    /// Parâmetros OAuth disparam CompleteAuthorizationAsync na carga.
    /// </summary>
    [Fact]
    public async Task Panel_WhenOAuthQueryPresent_ShouldCompleteAuthorization()
    {
        var workspaceId = Guid.NewGuid();
        _socialClient.ListConnectionsAsync(workspaceId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<OrganicConnectionSummaryDto>>.Success(Array.Empty<OrganicConnectionSummaryDto>())));

        _socialClient.CompleteAuthorizationAsync(
                workspaceId,
                "code-123",
                "state-abc",
                Arg.Any<string>(),
                null,
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<CompleteOrganicAuthorizationResponse>.Success(
                new CompleteOrganicAuthorizationResponse(Guid.NewGuid(), OrganicPlatform.Instagram, "brand", DateTime.UtcNow.AddDays(30)))));

        Render<SocialConnectionsPanel>(p => p
            .Add(x => x.WorkspaceId, workspaceId)
            .Add(x => x.OAuthCode, "code-123")
            .Add(x => x.OAuthState, "state-abc"));

        await Task.Delay(50);

        await _socialClient.Received(1).CompleteAuthorizationAsync(
            workspaceId,
            "code-123",
            "state-abc",
            Arg.Any<string>(),
            null,
            Arg.Any<CancellationToken>());
    }
}
