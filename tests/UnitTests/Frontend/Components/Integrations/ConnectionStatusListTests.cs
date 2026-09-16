using BuildingBlocks.Domain.Primitives;
using Bunit;
using FluentAssertions;
using Integrations.Application.OAuth.DTOs;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using UnitTests.Frontend.Common;
using WebApp.Components.Integrations;
using WebApp.Services;
using Xunit;

namespace UnitTests.Frontend.Components.Integrations;

/// <summary>
/// Testes de componente bUnit para o painel de status de conexões e saúde de credenciais (<see cref="ConnectionStatusList"/>).
/// </summary>
public sealed class ConnectionStatusListTests : BunitTestBase
{
    private readonly IOAuthIntegrationsClientService _oauthClient = Substitute.For<IOAuthIntegrationsClientService>();

    /// <summary>
    /// Registra o mock do cliente OAuth nas dependências do teste bUnit.
    /// </summary>
    public ConnectionStatusListTests()
    {
        Services.AddSingleton(_oauthClient);
    }

    /// <summary>
    /// Valida que ao renderizar, o componente lista os 4 cartões de plataformas padrão.
    /// </summary>
    [Fact]
    public void ConnectionStatusList_WhenRendered_ShouldDisplayAllStandardPlatforms()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        _oauthClient.GetConnectionsStatusAsync(workspaceId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<OAuthConnectionStatusDto>>.Success(Array.Empty<OAuthConnectionStatusDto>())));

        // Act
        var cut = Render<ConnectionStatusList>(parameters => parameters
            .Add(p => p.WorkspaceId, workspaceId));

        // Assert
        cut.Find("[data-testid='connection-status-panel']").Should().NotBeNull();
        cut.Find("[data-testid='platform-card-metaads']").Should().NotBeNull();
        cut.Find("[data-testid='platform-card-googleads']").Should().NotBeNull();
        cut.Find("[data-testid='platform-card-tiktokads']").Should().NotBeNull();
        cut.Find("[data-testid='platform-card-bingads']").Should().NotBeNull();
    }

    /// <summary>
    /// Valida que uma conexão com token ativo exibe badge de status 'Válido &amp; Ativo'.
    /// </summary>
    [Fact]
    public void ConnectionStatusList_WhenActiveToken_ShouldDisplayValidBadge()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var status = new List<OAuthConnectionStatusDto>
        {
            new(
                Id: Guid.NewGuid(),
                WorkspaceId: workspaceId,
                Platform: "MetaAds",
                ExternalAccountId: "act_102030",
                ExternalAccountName: "Conta Principal Meta",
                Status: "Active",
                Scopes: "ads_read,ads_management",
                AccessTokenExpiresAtUtc: DateTime.UtcNow.AddDays(45),
                IsExpiringSoon: false,
                CreatedAtUtc: DateTime.UtcNow.AddDays(-10),
                UpdatedAtUtc: DateTime.UtcNow.AddDays(-1))
        };

        _oauthClient.GetConnectionsStatusAsync(workspaceId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<OAuthConnectionStatusDto>>.Success(status)));

        // Act
        var cut = Render<ConnectionStatusList>(parameters => parameters
            .Add(p => p.WorkspaceId, workspaceId));

        // Assert
        var metaCard = cut.Find("[data-testid='platform-card-metaads']");
        var badge = metaCard.QuerySelector("[data-testid='badge-status']");
        badge.Should().NotBeNull();
        badge!.TextContent.Should().Contain("Válido & Ativo");
        metaCard.TextContent.Should().Contain("Conta Principal Meta");
    }

    /// <summary>
    /// Valida que conexão com expiração iminente exibe badge 'Expira em Breve'.
    /// </summary>
    [Fact]
    public void ConnectionStatusList_WhenExpiringSoonToken_ShouldDisplayWarningBadge()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var status = new List<OAuthConnectionStatusDto>
        {
            new(
                Id: Guid.NewGuid(),
                WorkspaceId: workspaceId,
                Platform: "GoogleAds",
                ExternalAccountId: "123-456-7890",
                ExternalAccountName: "Conta Google Ads Principal",
                Status: "Active",
                Scopes: "https://www.googleapis.com/auth/adwords",
                AccessTokenExpiresAtUtc: DateTime.UtcNow.AddHours(12),
                IsExpiringSoon: true,
                CreatedAtUtc: DateTime.UtcNow.AddDays(-30),
                UpdatedAtUtc: DateTime.UtcNow.AddDays(-6))
        };

        _oauthClient.GetConnectionsStatusAsync(workspaceId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<OAuthConnectionStatusDto>>.Success(status)));

        // Act
        var cut = Render<ConnectionStatusList>(parameters => parameters
            .Add(p => p.WorkspaceId, workspaceId));

        // Assert
        var googleCard = cut.Find("[data-testid='platform-card-googleads']");
        var badge = googleCard.QuerySelector("[data-testid='badge-status']");
        badge.Should().NotBeNull();
        badge!.TextContent.Should().Contain("Expira em Breve");
    }

    /// <summary>
    /// Valida que ao clicar no botão de renovar credenciais, aciona o cliente HTTP e exibe mensagem de sucesso.
    /// </summary>
    [Fact]
    public async Task ConnectionStatusList_WhenRefreshClicked_ShouldCallClientAndShowSuccessAlert()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        _oauthClient.GetConnectionsStatusAsync(workspaceId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<OAuthConnectionStatusDto>>.Success(Array.Empty<OAuthConnectionStatusDto>())));

        _oauthClient.RefreshExpiringTokensAsync(Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<int>.Success(2)));

        var cut = Render<ConnectionStatusList>(parameters => parameters
            .Add(p => p.WorkspaceId, workspaceId));

        // Act
        var refreshButton = cut.Find("[data-testid='btn-refresh-tokens']");
        await refreshButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert
        await _oauthClient.Received(1).RefreshExpiringTokensAsync(Arg.Any<int?>(), Arg.Any<CancellationToken>());
        var alert = cut.Find("[data-testid='feedback-alert']");
        alert.TextContent.Should().Contain("Credenciais renovadas com sucesso! 2 token(s) atualizado(s).");
    }
}
