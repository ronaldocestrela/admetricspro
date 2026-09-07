using AngleSharp.Dom;
using Bunit;
using FluentAssertions;
using Tenants.Application.Auth.DTOs;
using UnitTests.Frontend.Common;
using WebApp.Components.Pages;
using WebApp.State;
using Xunit;

namespace UnitTests.Frontend.Components.Pages;

/// <summary>
/// Testes unitários com bUnit para o componente de Visão Geral operacional do Inquilino (<see cref="TenantDashboardPage"/>).
/// Valida saudação personalizada, fallback sem sessão, cards de métricas zeradas, empty state orientativo,
/// painel de 4 redes de anúncios (Meta, Google, TikTok, Bing) e reatividade do circuito Blazor.
/// </summary>
public sealed class TenantDashboardPageTests : BunitTestBase
{
    /// <summary>
    /// Valida que ao haver uma sessão autenticada ativa, o header renderiza a saudação personalizada com o nome do gestor.
    /// </summary>
    [Fact]
    public void TenantDashboardPage_WhenUserAuthenticated_ShouldRenderPersonalizedWelcomeHeader()
    {
        // Arrange
        var session = new AuthenticatedTenantUserDto(
            AccessToken: "test-token",
            TokenType: "Bearer",
            ExpiresIn: 3600,
            UserId: Guid.NewGuid(),
            Email: "carlos@vanguarda.com.br",
            FullName: "Carlos Silva",
            Role: "Owner",
            TenantId: Guid.NewGuid(),
            Subdomain: "vanguarda",
            Branding: null);

        TenantSessionStateProvider.SetSession(session);

        // Act
        var cut = Render<TenantDashboardPage>();

        // Assert
        var headerTitle = cut.Find(".dashboard-welcome-title");
        headerTitle.Should().NotBeNull();
        headerTitle.TextContent.Trim().Should().Contain("Olá, Carlos Silva! Bem-vindo à sua central de tráfego.");
    }

    /// <summary>
    /// Valida que na ausência de sessão ativa ou nome informado, o header exibe o fallback gracioso para 'Gestor'.
    /// </summary>
    [Fact]
    public void TenantDashboardPage_WhenNoUserInSession_ShouldRenderFallbackWelcomeHeader()
    {
        // Arrange
        TenantSessionStateProvider.ClearSession();

        // Act
        var cut = Render<TenantDashboardPage>();

        // Assert
        var headerTitle = cut.Find(".dashboard-welcome-title");
        headerTitle.Should().NotBeNull();
        headerTitle.TextContent.Trim().Should().Contain("Olá, Gestor! Bem-vindo à sua central de tráfego.");
    }

    /// <summary>
    /// Valida que o subtítulo da página de visão geral menciona o nome da agência ativa no TenantState.
    /// </summary>
    [Fact]
    public void TenantDashboardPage_ShouldRenderAgencyNameInSubtitle()
    {
        // Arrange
        var customTenant = new TenantState(
            TenantId: Guid.NewGuid(),
            Name: "Vanguarda Performance",
            Slug: "vanguarda",
            CustomDomain: null,
            Branding: TenantBranding.Default);

        SetTenant(customTenant);

        // Act
        var cut = Render<TenantDashboardPage>();

        // Assert
        var subtitle = cut.Find(".dashboard-welcome-subtitle");
        subtitle.Should().NotBeNull();
        subtitle.TextContent.Should().Contain("Vanguarda Performance");
    }

    /// <summary>
    /// Valida que a página renderiza os 6 cards de KPI em estado zerado inicial (Empty State elegante).
    /// </summary>
    [Fact]
    public void TenantDashboardPage_ShouldRenderZeroedKpiMetricCards()
    {
        // Act
        var cut = Render<TenantDashboardPage>();

        // Assert
        var metricCards = cut.FindAll(".metric-card");
        metricCards.Should().HaveCount(6);

        var metricTitles = cut.FindAll(".metric-title").Select(e => e.TextContent.Trim()).ToList();
        metricTitles.Should().Contain("Investimento Total");
        metricTitles.Should().Contain("Receita Atribuída");
        metricTitles.Should().Contain("ROAS Consolidado");
        metricTitles.Should().Contain("MER Médio");
        metricTitles.Should().Contain("Cliques & Impressões");
        metricTitles.Should().Contain("CPA Médio");

        var pageText = cut.Markup;
        pageText.Should().Contain("R$ 0,00");
        pageText.Should().Contain("0,00x");
        pageText.Should().Contain("0,00%");
    }

    /// <summary>
    /// Valida que a seção de Empty State é exibida com os 3 atalhos de onboarding operacional (Integrations, Workspaces, Squads).
    /// </summary>
    [Fact]
    public void TenantDashboardPage_ShouldRenderEmptyStateWithActionShortcuts()
    {
        // Act
        var cut = Render<TenantDashboardPage>();

        // Assert
        var emptyState = cut.Find(".dashboard-empty-state");
        emptyState.Should().NotBeNull();
        emptyState.TextContent.Should().Contain("Sua central de tráfego unificada está pronta");

        var integrationLink = cut.Find("a[href='integrations']");
        integrationLink.Should().NotBeNull();

        var workspaceLink = cut.Find("a[href='workspaces']");
        workspaceLink.Should().NotBeNull();

        var squadsLink = cut.Find("a[href='squads']");
        squadsLink.Should().NotBeNull();
    }

    /// <summary>
    /// Valida que o painel de redes exibe as 4 plataformas mandatárias de anúncios (Meta, Google, TikTok, Bing) em estado desconectado.
    /// </summary>
    [Fact]
    public void TenantDashboardPage_ShouldRenderAllFourSupportedAdPlatformsWithDisconnectedStatus()
    {
        // Act
        var cut = Render<TenantDashboardPage>();

        // Assert
        var platformCards = cut.FindAll(".ad-platform-card");
        platformCards.Should().HaveCount(4);

        var markup = cut.Markup;
        markup.Should().Contain("Meta Ads");
        markup.Should().Contain("Google Ads");
        markup.Should().Contain("TikTok Ads");
        markup.Should().Contain("Bing Ads");

        var disconnectedBadges = cut.FindAll(".platform-status-badge.disconnected");
        disconnectedBadges.Should().HaveCount(4);
    }

    /// <summary>
    /// Valida que quando o estado de sessão é atualizado reativamente, o nome do gestor é atualizado no template.
    /// </summary>
    [Fact]
    public void TenantDashboardPage_WhenSessionStateChanges_ShouldReRenderWithNewName()
    {
        // Arrange
        TenantSessionStateProvider.ClearSession();
        var cut = Render<TenantDashboardPage>();
        cut.Find(".dashboard-welcome-title").TextContent.Should().Contain("Olá, Gestor!");

        var newSession = new AuthenticatedTenantUserDto(
            AccessToken: "new-token",
            TokenType: "Bearer",
            ExpiresIn: 3600,
            UserId: Guid.NewGuid(),
            Email: "ana@agencia.com",
            FullName: "Ana Beatriz",
            Role: "Admin",
            TenantId: Guid.NewGuid(),
            Subdomain: "agencia",
            Branding: null);

        // Act
        TenantSessionStateProvider.SetSession(newSession);

        // Assert
        cut.Find(".dashboard-welcome-title").TextContent.Should().Contain("Olá, Ana Beatriz!");
    }
}
