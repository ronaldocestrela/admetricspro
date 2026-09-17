using AngleSharp.Dom;
using BuildingBlocks.Domain.Primitives;
using Bunit;
using FluentAssertions;
using NSubstitute;
using Tenants.Application.Workspaces.DTOs;
using UnitTests.Frontend.Common;
using WebApp.Components.Pages;
using Xunit;

namespace UnitTests.Frontend.Components.Pages;

/// <summary>
/// Testes unitários com bUnit para a página do Copiloto de IA (<see cref="TrafficCopilotPage"/>).
/// Valida carregamento inicial, restauração de sessão em reload, estado vazio quando sem workspaces e sincronização de contexto.
/// </summary>
public sealed class TrafficCopilotPageTests : BunitTestBase
{
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly WorkspaceDto _workspace1;
    private readonly WorkspaceDto _workspace2;

    /// <summary>
    /// Configura workspaces padrão para os testes do Copiloto.
    /// </summary>
    public TrafficCopilotPageTests()
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
    /// Valida que ao renderizar sem workspaces cadastrados, a página exibe um estado orientativo informando que não há clientes.
    /// </summary>
    [Fact]
    public void TrafficCopilotPage_WhenNoWorkspaces_ShouldRenderEmptyState()
    {
        // Arrange
        WorkspaceClientService.GetWorkspacesAsync(Arg.Any<bool?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<WorkspaceDto>>.Success(Array.Empty<WorkspaceDto>())));

        // Act
        var cut = Render<TrafficCopilotPage>();

        // Assert
        cut.Find("[data-testid='copilot-empty-workspaces']").Should().NotBeNull();
    }

    /// <summary>
    /// Valida que quando a página inicializa sem sessão e ela é restaurada (ex: reload F5),
    /// os workspaces e o diagnóstico são recarregados reativamente.
    /// </summary>
    [Fact]
    public void TrafficCopilotPage_WhenSessionRestoredAfterInitialRender_ShouldAutomaticallyReloadWorkspaces()
    {
        // Arrange
        var callCount = 0;
        WorkspaceClientService.GetWorkspacesAsync(Arg.Any<bool?>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                callCount++;
                return Task.FromResult(callCount == 1
                    ? Result<IReadOnlyList<WorkspaceDto>>.Success(Array.Empty<WorkspaceDto>())
                    : Result<IReadOnlyList<WorkspaceDto>>.Success(new[] { _workspace1 }));
            });

        var cut = Render<TrafficCopilotPage>();

        // Assert inicial: estado vazio
        cut.Find("[data-testid='copilot-empty-workspaces']").Should().NotBeNull();

        // Act - Simula restauração de sessão pelo layout ou circuito SignalR
        var userDto = new Tenants.Application.Auth.DTOs.AuthenticatedTenantUserDto(
            AccessToken: "jwt_token_restored",
            TokenType: "Bearer",
            ExpiresIn: 3600,
            UserId: Guid.NewGuid(),
            Email: "gestor@restaurado.com",
            FullName: "Gestor Restaurado",
            Role: "Owner",
            TenantId: Guid.NewGuid(),
            Subdomain: "agencia-restaurada",
            Branding: null);

        TenantSessionStateProvider.SetSession(userDto);

        // Assert reativo: seletor de workspace deve ser renderizado e diagnóstico consultado
        cut.WaitForAssertion(() =>
        {
            cut.Find("#copilot-workspace-select").Should().NotBeNull();
        });

        TrafficCopilotClientService.Received().GetDailyDiagnosticAsync(_workspace1.Id, Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Valida que ao alterar o workspace ativo via WorkspaceContextStateProvider, a página sincroniza o workspace e recarrega o diagnóstico.
    /// </summary>
    [Fact]
    public async Task TrafficCopilotPage_WhenWorkspaceContextChanged_ShouldUpdateActiveWorkspaceAndReloadDiagnostic()
    {
        // Arrange
        WorkspaceClientService.GetWorkspacesAsync(Arg.Any<bool?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<WorkspaceDto>>.Success(new[] { _workspace1, _workspace2 })));

        var cut = Render<TrafficCopilotPage>();

        // Act - Workspace contextual é alterado externamente (ex: TopHeader)
        await WorkspaceContextStateProvider.SetActiveWorkspaceAsync(_workspace2.Id, _workspace2.Name);

        // Assert
        cut.WaitForAssertion(() =>
        {
            var select = cut.Find("#copilot-workspace-select");
            select.GetAttribute("value").Should().Be(_workspace2.Id.ToString());
        });

        await TrafficCopilotClientService.Received().GetDailyDiagnosticAsync(_workspace2.Id, Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }
}
