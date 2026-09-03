using Bunit;
using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using UnitTests.Frontend.Common;
using WebApp.Components.Shared;
using WebApp.Services;
using WebApp.State;

namespace UnitTests.Frontend.Components.Shared;

/// <summary>
/// Testes de componente bUnit para <see cref="ImpersonationBanner"/>.
/// Valida a sinalização visual de destaque em Shadow Mode e o encerramento imediato da sessão.
/// </summary>
public sealed class ImpersonationBannerTests : BunitTestBase
{
    /// <summary>
    /// Valida que quando não há sessão de impersonation ativa, nada é renderizado no DOM.
    /// </summary>
    [Fact]
    public void ImpersonationBanner_WhenSessionIsInactive_ShouldNotRenderBanner()
    {
        // Arrange
        ImpersonationStateProvider.ClearSession();

        // Act
        var cut = Render<ImpersonationBanner>();

        // Assert
        cut.FindAll(".impersonation-banner").Should().BeEmpty();
    }

    /// <summary>
    /// Valida que quando há sessão de impersonation ativa, renderiza tarja de aviso destacada,
    /// exibindo o número do chamado, identificador do superadmin e aviso de acesso auditado.
    /// </summary>
    [Fact]
    public void ImpersonationBanner_WhenSessionIsActive_ShouldRenderHighContrastWarning()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var superAdminId = Guid.NewGuid();
        var ticket = "INC-84920";

        ImpersonationStateProvider.SetSession(new ImpersonationSessionState(
            IsActive: true,
            SessionId: sessionId,
            TenantId: tenantId,
            TenantName: "Alpha Marketing",
            SuperAdminId: superAdminId,
            SupportTicketId: ticket,
            Reason: "Diagnóstico de campanhas",
            ExpiresAtUtc: DateTime.UtcNow.AddMinutes(45)));

        // Act
        var cut = Render<ImpersonationBanner>();

        // Assert
        cut.Find(".impersonation-banner").Should().NotBeNull();
        cut.Find(".banner-badge").TextContent.Should().Contain("MODO SHADOW ATIVO");
        cut.Find(".ticket-info").TextContent.Should().Contain(ticket);
        cut.Find(".admin-info").TextContent.Should().Contain(superAdminId.ToString());
        cut.Find("button.terminate-btn").Should().NotBeNull();
    }

    /// <summary>
    /// Valida que clicar no botão de encerramento invoca o serviço de revogação e limpa a sessão.
    /// </summary>
    [Fact]
    public async Task ImpersonationBanner_WhenClickingTerminate_ShouldInvokeServiceAndClearSession()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var superAdminId = Guid.NewGuid();

        ImpersonationStateProvider.SetSession(new ImpersonationSessionState(
            IsActive: true,
            SessionId: sessionId,
            TenantId: tenantId,
            TenantName: "Alpha Marketing",
            SuperAdminId: superAdminId,
            SupportTicketId: "INC-84920",
            Reason: "Diagnóstico de campanhas",
            ExpiresAtUtc: DateTime.UtcNow.AddMinutes(45)));

        ImpersonationClientService.TerminateSessionAsync(tenantId, sessionId, Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success()));

        var cut = Render<ImpersonationBanner>();

        // Act
        var terminateBtn = cut.Find("button.terminate-btn");
        await cut.InvokeAsync(() => terminateBtn.Click());

        // Assert
        await ImpersonationClientService.Received(1).TerminateSessionAsync(tenantId, sessionId, Arg.Any<string?>(), Arg.Any<CancellationToken>());
        ImpersonationStateProvider.CurrentSession.IsActive.Should().BeFalse();
    }
}
