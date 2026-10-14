using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Tenants.Application.Auth.DTOs;
using Tenants.Application.Workspaces.DTOs;
using UnitTests.Frontend.Common;
using WebApp.Components.Layout;
using WebApp.Components.Pages;
using WebApp.State;

namespace UnitTests.Frontend.Components.Layout;

/// <summary>Verifica a área do cliente com ações no menu lateral e identidade configurável.</summary>
public sealed class TenantShellPresentationTests : BunitTestBase
{
    /// <summary>O seletor lateral mantém e limpa o contexto global do cliente ativo.</summary>
    [Fact]
    public async Task Sidebar_ShouldSelectWorkspaceAndReturnToConsolidatedView()
    {
        var workspaceId = Guid.NewGuid();
        WorkspaceClientService.GetWorkspacesAsync(true, Arg.Any<CancellationToken>()).Returns(
            Result<IReadOnlyList<WorkspaceDto>>.Success([new(workspaceId, "Cliente Alfa", "12345678000195", 1000m, "E-commerce", true, DateTime.UtcNow, null)]));
        SetTenant(new TenantState(Guid.NewGuid(), "Agência Alfa", "alfa", null, TenantBranding.Default));
        var cut = Render<TenantSidebar>();
        cut.Find("#sidebar-workspace-select").Change(workspaceId.ToString());
        WorkspaceContextStateProvider.CurrentWorkspaceId.Should().Be(workspaceId);
        WorkspaceContextStateProvider.CurrentWorkspaceName.Should().Be("Cliente Alfa");
        cut.Find("#sidebar-workspace-select").Change("");
        WorkspaceContextStateProvider.CurrentWorkspaceId.Should().BeNull();
        await Task.CompletedTask;
    }

    /// <summary>Logout lateral encerra sessão, limpa workspace e volta ao login.</summary>
    [Fact]
    public async Task Sidebar_ShouldLogoutAndClearSelectedWorkspace()
    {
        TenantSessionStateProvider.SetSession(new AuthenticatedTenantUserDto("qa", "Bearer", 3600, Guid.NewGuid(),
            "qa@example.test", "Gestor Alfa", "Owner", Guid.NewGuid(), "alfa", null));
        await WorkspaceContextStateProvider.SetActiveWorkspaceAsync(Guid.NewGuid(), "Cliente Alfa");
        var cut = Render<TenantSidebar>();
        cut.Find(".sidebar-logout-btn").Click();
        TenantSessionStateProvider.IsAuthenticated.Should().BeFalse();
        WorkspaceContextStateProvider.CurrentWorkspaceId.Should().BeNull();
        Services.GetRequiredService<NavigationManager>().Uri.Should().EndWith("/login");
    }

    /// <summary>Logout limpa o contexto enquanto aguarda o navegador, sem bloquear o circuito.</summary>
    [Fact]
    public async Task Sidebar_ShouldAwaitStoredSessionRemovalWithoutBlockingWorkspaceCleanup()
    {
        TenantSessionStateProvider.SetSession(new AuthenticatedTenantUserDto("qa", "Bearer", 3600, Guid.NewGuid(),
            "qa@example.test", "Gestor Alfa", "Owner", Guid.NewGuid(), "alfa", null));
        await WorkspaceContextStateProvider.SetActiveWorkspaceAsync(Guid.NewGuid(), "Cliente Alfa");
        var removal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        BrowserStorageService.RemoveItemAsync("admetricspro_tenant_session", Arg.Any<CancellationToken>())
            .Returns(new ValueTask(removal.Task));
        var cut = Render<TenantSidebar>();
        var logout = Task.Run(() => cut.Find(".sidebar-logout-btn").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs()));
        try
        {
            cut.WaitForAssertion(() => WorkspaceContextStateProvider.CurrentWorkspaceId.Should().BeNull());
            logout.IsCompleted.Should().BeFalse("a navegação deve aguardar a remoção da sessão persistida");
        }
        finally { removal.TrySetResult(true); }
        await logout;
        await BrowserStorageService.Received().RemoveItemAsync("admetricspro_active_workspace", Arg.Any<CancellationToken>());
        Services.GetRequiredService<NavigationManager>().Uri.Should().EndWith("/login");
    }

    /// <summary>Mantém textos do menu lateral sem destaque de cor em nomes compostos.</summary>
    [Fact]
    public void Sidebar_ShouldKeepCompoundNamesWithoutAccent()
    {
        SetTenant(new TenantState(Guid.NewGuid(), "Vanguarda Digital", "vanguarda", null,
            new TenantBranding("#EC4899", "#831843", "#F472B6", CompanyName: "Vanguarda Digital")));
        var cut = Render<TenantSidebar>();
        cut.Find(".sidebar-brand-name").TextContent.Should().Contain("Vanguarda Digital");
        cut.FindAll(".tenant-name-accent").Should().BeEmpty();
    }

    /// <summary>Destaca o segundo nome do título mantendo o componente da página.</summary>
    [Fact]
    public void Workspaces_ShouldKeepTitleAndAccentSecondName()
    {
        var cut = Render<WorkspacesPage>();
        cut.Find("h1").TextContent.Should().Contain("Clientes & Workspaces");
        cut.Find("h1 .tenant-title-accent").TextContent.Should().Be("Workspaces");
    }
}
