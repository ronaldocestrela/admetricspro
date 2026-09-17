using AngleSharp.Dom;
using BuildingBlocks.Domain.Primitives;
using Bunit;
using FluentAssertions;
using Integrations.Application.OAuth.DTOs;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Tenants.Application.Ftux.DTOs;
using Tenants.Application.Workspaces.DTOs;
using UnitTests.Frontend.Common;
using WebApp.Components.Pages;
using WebApp.Models;
using Xunit;

namespace UnitTests.Frontend.Components.Pages;

/// <summary>
/// Testes unitários com bUnit para a página central de Integrações de Anúncios (<see cref="IntegrationsPage"/>).
/// Valida carregamento inicial, seletor de workspaces, exibição dos 4 canais suportados (Meta, Google, TikTok, Bing),
/// renovação preventiva de credenciais, ativação de conta demo (FTUX) e revogação de tokens com modal de confirmação.
/// </summary>
public sealed class IntegrationsPageTests : BunitTestBase
{
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly WorkspaceDto _workspace1;
    private readonly WorkspaceDto _workspace2;

    /// <summary>
    /// Configura o ambiente de teste com workspaces padrão.
    /// </summary>
    public IntegrationsPageTests()
    {
        _workspace1 = new WorkspaceDto(
            Id: _workspaceId,
            Name: "Cliente Alpha E-commerce",
            CnpjOrCpf: "12.345.678/0001-95",
            MonthlyAdSpendBudget: 15000m,
            Segment: "E-commerce",
            IsActive: true,
            CreatedAtUtc: DateTime.UtcNow.AddMonths(-1),
            UpdatedAtUtc: null);

        _workspace2 = new WorkspaceDto(
            Id: Guid.NewGuid(),
            Name: "Cliente Beta Serviços",
            CnpjOrCpf: "98.765.432/0001-10",
            MonthlyAdSpendBudget: 8000m,
            Segment: "Serviços",
            IsActive: true,
            CreatedAtUtc: DateTime.UtcNow.AddDays(-15),
            UpdatedAtUtc: null);
    }

    /// <summary>
    /// Valida que ao renderizar sem nenhum workspace cadastrado, a página exibe um estado vazio orientativo com link para cadastro.
    /// </summary>
    [Fact]
    public void IntegrationsPage_WhenNoWorkspaces_ShouldRenderEmptyStateWithCreateButton()
    {
        // Arrange
        WorkspaceClientService.GetWorkspacesAsync(Arg.Any<bool?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<WorkspaceDto>>.Success(Array.Empty<WorkspaceDto>())));

        // Act
        var cut = Render<IntegrationsPage>();

        // Assert
        cut.Find("[data-testid='integrations-empty-workspaces']").Should().NotBeNull();
        cut.Find("[data-testid='btn-create-first-workspace']").Should().NotBeNull();
    }

    /// <summary>
    /// Valida que ao carregar com workspaces existentes, o seletor é exibido e os 4 canais de mídia padrão são renderizados.
    /// </summary>
    [Fact]
    public void IntegrationsPage_WhenWorkspacesExist_ShouldRenderSelectorAndPlatformCards()
    {
        // Arrange
        WorkspaceClientService.GetWorkspacesAsync(Arg.Any<bool?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<WorkspaceDto>>.Success(new[] { _workspace1, _workspace2 })));

        var connections = new List<OAuthConnectionStatusDto>
        {
            new(
                Id: Guid.NewGuid(),
                WorkspaceId: _workspaceId,
                Platform: "MetaAds",
                ExternalAccountId: "act_10203040",
                ExternalAccountName: "Conta Principal Meta",
                Status: "Active",
                Scopes: "ads_read,ads_management",
                AccessTokenExpiresAtUtc: DateTime.UtcNow.AddDays(40),
                IsExpiringSoon: false,
                CreatedAtUtc: DateTime.UtcNow.AddDays(-20),
                UpdatedAtUtc: null)
        };

        OAuthIntegrationsClientService.GetConnectionsStatusAsync(_workspaceId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<OAuthConnectionStatusDto>>.Success(connections)));

        // Act
        var cut = Render<IntegrationsPage>();

        // Assert
        cut.Find("[data-testid='workspace-select']").Should().NotBeNull();
        cut.Find("[data-testid='card-platform-metaads']").Should().NotBeNull();
        cut.Find("[data-testid='card-platform-googleads']").Should().NotBeNull();
        cut.Find("[data-testid='card-platform-tiktokads']").Should().NotBeNull();
        cut.Find("[data-testid='card-platform-bingads']").Should().NotBeNull();

        // Meta Ads deve estar ativo
        var metaCard = cut.Find("[data-testid='card-platform-metaads']");
        metaCard.TextContent.Should().Contain("Conta Principal Meta");
        metaCard.QuerySelector("[data-testid='badge-status']").Should().NotBeNull();
        metaCard.QuerySelector("[data-testid='badge-status']")!.TextContent.Should().Contain("Válido & Ativo");

        // Google Ads deve estar desconectado
        var googleCard = cut.Find("[data-testid='card-platform-googleads']");
        googleCard.QuerySelector("[data-testid='badge-status']").Should().NotBeNull();
        googleCard.QuerySelector("[data-testid='badge-status']")!.TextContent.Should().Contain("Não Conectado");
    }

    /// <summary>
    /// Valida que a troca de workspace no select dispara nova consulta de status para o workspace selecionado.
    /// </summary>
    [Fact]
    public async Task IntegrationsPage_WhenWorkspaceChanged_ShouldReloadStatusForSelectedWorkspace()
    {
        // Arrange
        WorkspaceClientService.GetWorkspacesAsync(Arg.Any<bool?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<WorkspaceDto>>.Success(new[] { _workspace1, _workspace2 })));

        var cut = Render<IntegrationsPage>();

        // Act
        var select = cut.Find("[data-testid='workspace-select']");
        await select.ChangeAsync(new ChangeEventArgs { Value = _workspace2.Id.ToString() });

        // Assert
        await OAuthIntegrationsClientService.Received(1)
            .GetConnectionsStatusAsync(_workspace2.Id, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Valida que o clique no botão de renovar credenciais aciona o serviço e exibe feedback afirmativo na tela.
    /// </summary>
    [Fact]
    public async Task IntegrationsPage_WhenRefreshTokensClicked_ShouldShowFeedbackAlert()
    {
        // Arrange
        WorkspaceClientService.GetWorkspacesAsync(Arg.Any<bool?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<WorkspaceDto>>.Success(new[] { _workspace1 })));

        OAuthIntegrationsClientService.RefreshExpiringTokensAsync(Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<int>.Success(3)));

        var cut = Render<IntegrationsPage>();

        // Act
        var refreshBtn = cut.Find("[data-testid='btn-refresh-tokens']");
        await refreshBtn.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert
        await OAuthIntegrationsClientService.Received(1).RefreshExpiringTokensAsync(Arg.Any<int?>(), Arg.Any<CancellationToken>());
        var alert = cut.Find("[data-testid='feedback-alert']");
        alert.TextContent.Should().Contain("Credenciais renovadas com sucesso! 3 token(s) atualizado(s).");
    }

    /// <summary>
    /// Valida que ao clicar em conectar OAuth em um canal desconectado, o serviço de autorização é acionado.
    /// </summary>
    [Fact]
    public async Task IntegrationsPage_WhenConnectOAuthClicked_ShouldInitiateOAuthFlow()
    {
        // Arrange
        WorkspaceClientService.GetWorkspacesAsync(Arg.Any<bool?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<WorkspaceDto>>.Success(new[] { _workspace1 })));

        OAuthIntegrationsClientService.InitiateOAuthFlowAsync(_workspaceId, "GoogleAds", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<OAuthAuthorizationUrlDto>.Success(new OAuthAuthorizationUrlDto("https://accounts.google.com/oauth", "dummy-state"))));

        var cut = Render<IntegrationsPage>();

        // Act
        var connectBtn = cut.Find("[data-testid='btn-connect-googleads']");
        await connectBtn.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert
        await OAuthIntegrationsClientService.Received(1)
            .InitiateOAuthFlowAsync(_workspaceId, "GoogleAds", Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Valida que a ativação do modo demonstração invoca o cliente de FTUX e recarrega os status das conexões.
    /// </summary>
    [Fact]
    public async Task IntegrationsPage_WhenConnectDemoClicked_ShouldInvokeFtuxAndReload()
    {
        // Arrange
        WorkspaceClientService.GetWorkspacesAsync(Arg.Any<bool?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<WorkspaceDto>>.Success(new[] { _workspace1 })));

        TenantFtuxClientService.ConnectDemoAccountAsync(Arg.Any<ConnectDemoAccountModel>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<Guid>.Success(Guid.NewGuid())));

        var cut = Render<IntegrationsPage>();

        // Act
        var demoBtn = cut.Find("[data-testid='btn-demo-tiktokads']");
        await demoBtn.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert
        await TenantFtuxClientService.Received(1)
            .ConnectDemoAccountAsync(Arg.Is<ConnectDemoAccountModel>(m => m.WorkspaceId == _workspaceId && m.Platform == "TikTokAds"), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Valida que a revogação de conexão exibe modal de confirmação e ao confirmar remove as credenciais do cofre.
    /// </summary>
    [Fact]
    public async Task IntegrationsPage_WhenRevokeConnectionConfirmed_ShouldCallRevokeService()
    {
        // Arrange
        WorkspaceClientService.GetWorkspacesAsync(Arg.Any<bool?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<WorkspaceDto>>.Success(new[] { _workspace1 })));

        var connections = new List<OAuthConnectionStatusDto>
        {
            new(
                Id: Guid.NewGuid(),
                WorkspaceId: _workspaceId,
                Platform: "MetaAds",
                ExternalAccountId: "act_10203040",
                ExternalAccountName: "Conta Meta",
                Status: "Active",
                Scopes: "ads_read",
                AccessTokenExpiresAtUtc: DateTime.UtcNow.AddDays(10),
                IsExpiringSoon: false,
                CreatedAtUtc: DateTime.UtcNow.AddDays(-5),
                UpdatedAtUtc: null)
        };

        OAuthIntegrationsClientService.GetConnectionsStatusAsync(_workspaceId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<OAuthConnectionStatusDto>>.Success(connections)));

        OAuthIntegrationsClientService.RevokeConnectionAsync(_workspaceId, "MetaAds", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success()));

        var cut = Render<IntegrationsPage>();

        // Act - Abre modal de confirmação
        var revokeBtn = cut.Find("[data-testid='btn-revoke-metaads']");
        await revokeBtn.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert - Modal de confirmação visível
        cut.Find("[data-testid='revoke-confirm-modal']").Should().NotBeNull();

        // Act - Confirma a revogação
        var confirmBtn = cut.Find("[data-testid='btn-confirm-revoke']");
        await confirmBtn.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert - Chamada realizada e modal fechado
        await OAuthIntegrationsClientService.Received(1)
            .RevokeConnectionAsync(_workspaceId, "MetaAds", Arg.Any<CancellationToken>());
        cut.FindAll("[data-testid='revoke-confirm-modal']").Should().BeEmpty();
    }
}
