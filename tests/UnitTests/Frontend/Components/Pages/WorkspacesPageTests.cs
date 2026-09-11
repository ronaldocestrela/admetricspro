using AngleSharp.Dom;
using Bunit;
using FluentAssertions;
using NSubstitute;
using Tenants.Application.Workspaces.DTOs;
using UnitTests.Frontend.Common;
using WebApp.Components.Pages;
using WebApp.State;
using Xunit;

namespace UnitTests.Frontend.Components.Pages;

/// <summary>
/// Testes unitários com bUnit para a página de Gestão de Clientes e Workspaces (<see cref="WorkspacesPage"/>).
/// Valida carregamento inicial, cards de métricas da carteira, renderização da tabela, filtros e alternância de status.
/// </summary>
public sealed class WorkspacesPageTests : BunitTestBase
{
    private readonly List<WorkspaceDto> _sampleWorkspaces = new()
    {
        new WorkspaceDto(
            Id: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Name: "Loja Alpha Calçados",
            CnpjOrCpf: "12345678000195",
            MonthlyAdSpendBudget: 15000.00m,
            Segment: "E-commerce",
            IsActive: true,
            CreatedAtUtc: DateTime.UtcNow.AddDays(-10),
            UpdatedAtUtc: null),
        new WorkspaceDto(
            Id: Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Name: "Agência Imobiliária Premium",
            CnpjOrCpf: "98765432000109",
            MonthlyAdSpendBudget: 8500.00m,
            Segment: "Imobiliário",
            IsActive: false,
            CreatedAtUtc: DateTime.UtcNow.AddDays(-30),
            UpdatedAtUtc: null)
    };

    /// <summary>
    /// Valida que a página renderiza o cabeçalho com o nome institucional da agência ativa.
    /// </summary>
    [Fact]
    public void WorkspacesPage_WhenRendered_ShouldDisplayPageHeaderAndAgencyName()
    {
        // Arrange
        var tenant = new TenantState(
            TenantId: Guid.NewGuid(),
            Name: "Vanguarda Performance",
            Slug: "vanguarda",
            CustomDomain: null,
            Branding: TenantBranding.Default);

        SetTenant(tenant);

        WorkspaceClientService.GetWorkspacesAsync(Arg.Any<bool?>(), Arg.Any<CancellationToken>())
            .Returns(BuildingBlocks.Domain.Primitives.Result<IReadOnlyList<WorkspaceDto>>.Success(Array.Empty<WorkspaceDto>()));

        // Act
        var cut = Render<WorkspacesPage>();

        // Assert
        var title = cut.Find(".page-title");
        title.TextContent.Trim().Should().Be("Clientes & Workspaces");

        var subtitle = cut.Find(".page-subtitle");
        subtitle.TextContent.Should().Contain("Vanguarda Performance");
    }

    /// <summary>
    /// Valida que ao retornar lista vazia de workspaces, o empty state com ação rápida é apresentado.
    /// </summary>
    [Fact]
    public void WorkspacesPage_WhenWorkspacesListIsEmpty_ShouldRenderEmptyState()
    {
        // Arrange
        WorkspaceClientService.GetWorkspacesAsync(Arg.Any<bool?>(), Arg.Any<CancellationToken>())
            .Returns(BuildingBlocks.Domain.Primitives.Result<IReadOnlyList<WorkspaceDto>>.Success(Array.Empty<WorkspaceDto>()));

        // Act
        var cut = Render<WorkspacesPage>();

        // Assert
        var emptyState = cut.Find(".empty-state");
        emptyState.Should().NotBeNull();
        emptyState.TextContent.Should().Contain("Nenhum cliente/workspace encontrado");
    }

    /// <summary>
    /// Valida que ao retornar workspaces, os cards de resumo contabilizam corretamente clientes totais, ativos e verba total.
    /// </summary>
    [Fact]
    public void WorkspacesPage_WhenWorkspacesLoaded_ShouldRenderSummaryCards()
    {
        // Arrange
        WorkspaceClientService.GetWorkspacesAsync(Arg.Any<bool?>(), Arg.Any<CancellationToken>())
            .Returns(BuildingBlocks.Domain.Primitives.Result<IReadOnlyList<WorkspaceDto>>.Success(_sampleWorkspaces));

        // Act
        var cut = Render<WorkspacesPage>();

        // Assert
        var summaryValues = cut.FindAll(".summary-value");
        summaryValues.Should().HaveCount(3);

        // Card 1: Total de Clientes (2)
        summaryValues[0].TextContent.Trim().Should().Be("2");

        // Card 2: Clientes Ativos (1)
        summaryValues[1].TextContent.Trim().Should().Be("1");

        // Card 3: Verba Ativa (R$ 15.000,00)
        summaryValues[2].TextContent.Trim().Should().Contain("15.000");
    }

    /// <summary>
    /// Valida que a tabela é renderizada com todas as linhas de clientes e seus status correspondentes.
    /// </summary>
    [Fact]
    public void WorkspacesPage_WhenWorkspacesExist_ShouldRenderTableWithCorrectRows()
    {
        // Arrange
        WorkspaceClientService.GetWorkspacesAsync(Arg.Any<bool?>(), Arg.Any<CancellationToken>())
            .Returns(BuildingBlocks.Domain.Primitives.Result<IReadOnlyList<WorkspaceDto>>.Success(_sampleWorkspaces));

        // Act
        var cut = Render<WorkspacesPage>();

        // Assert
        var rows = cut.FindAll(".workspaces-table tbody tr");
        rows.Should().HaveCount(2);

        var clientNames = cut.FindAll(".client-name").Select(e => e.TextContent.Trim()).ToList();
        clientNames.Should().Contain("Loja Alpha Calçados");
        clientNames.Should().Contain("Agência Imobiliária Premium");

        var activeBadges = cut.FindAll(".status-badge.badge-active");
        activeBadges.Should().HaveCount(1);

        var pausedBadges = cut.FindAll(".status-badge.badge-paused");
        pausedBadges.Should().HaveCount(1);
    }

    /// <summary>
    /// Valida que ao clicar no botão de alternar status, o método ToggleWorkspaceStatusAsync é acionado com o identificador correto.
    /// </summary>
    [Fact]
    public async Task WorkspacesPage_WhenToggleStatusClicked_ShouldCallToggleWorkspaceStatusAsync()
    {
        // Arrange
        WorkspaceClientService.GetWorkspacesAsync(Arg.Any<bool?>(), Arg.Any<CancellationToken>())
            .Returns(BuildingBlocks.Domain.Primitives.Result<IReadOnlyList<WorkspaceDto>>.Success(_sampleWorkspaces));

        WorkspaceClientService.ToggleWorkspaceStatusAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(BuildingBlocks.Domain.Primitives.Result.Success());

        var cut = Render<WorkspacesPage>();

        // Act
        var toggleButtons = cut.FindAll(".btn-icon.btn-pause");
        toggleButtons.Should().NotBeEmpty();

        await cut.InvokeAsync(() => toggleButtons[0].Click());

        // Assert
        await WorkspaceClientService.Received(1)
            .ToggleWorkspaceStatusAsync(Guid.Parse("11111111-1111-1111-1111-111111111111"), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Valida que ao clicar em '+ Novo Workspace', o modal interativo é aberto.
    /// </summary>
    [Fact]
    public void WorkspacesPage_WhenOpenCreateModalClicked_ShouldRenderModal()
    {
        // Arrange
        WorkspaceClientService.GetWorkspacesAsync(Arg.Any<bool?>(), Arg.Any<CancellationToken>())
            .Returns(BuildingBlocks.Domain.Primitives.Result<IReadOnlyList<WorkspaceDto>>.Success(_sampleWorkspaces));

        var cut = Render<WorkspacesPage>();

        // Act
        var newBtn = cut.Find("#btn-open-create-workspace");
        newBtn.Click();

        // Assert
        var modal = cut.Find(".modal-dialog");
        modal.Should().NotBeNull();
        cut.Find(".modal-title").TextContent.Should().Be("Novo Workspace");
    }
}
