using BackofficeApp.Components.Pages;
using BackofficeApp.Models;
using BackofficeApp.Services;
using BuildingBlocks.Domain.Primitives;
using Bunit;
using FluentAssertions;
using Master.Application.Plans.DTOs;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using UnitTests.Frontend.Common;

namespace UnitTests.Frontend.Components.Pages;

/// <summary>
/// Testes de componente bUnit para a página de Gestão de Planos &amp; Limites do Backoffice (<see cref="PlansPage"/>).
/// Valida a exibição do catálogo de planos cadastrados, estado vazio, abertura do formulário de criação e edição,
/// e atualização após persistência.
/// </summary>
public sealed class BackofficePlansPageTests : BunitTestBase
{
    private readonly IPlanManagementService _planService;

    private static readonly List<PlanDto> MockPlans =
    [
        new(
            Id: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Name: "Starter Growth",
            Description: "Plano para pequenas agências em crescimento.",
            Tier: "Starter",
            MonthlyPrice: 199.00m,
            AnnualDiscountPercentage: 15,
            MaxSeats: 3,
            MaxWorkspaces: 2,
            MonthlyAdSpendCap: 15000m,
            HasWhiteLabel: false,
            HasCustomCname: false,
            HasAiCopilot: false,
            HasCrossNetworkAutomations: true,
            IsActive: true,
            CreatedAtUtc: DateTime.UtcNow.AddMonths(-2),
            UpdatedAtUtc: null),
        new(
            Id: Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Name: "Agência Enterprise Scale",
            Description: "Plano corporativo com recursos ilimitados.",
            Tier: "Enterprise",
            MonthlyPrice: 999.00m,
            AnnualDiscountPercentage: 20,
            MaxSeats: 25,
            MaxWorkspaces: 15,
            MonthlyAdSpendCap: 150000m,
            HasWhiteLabel: true,
            HasCustomCname: true,
            HasAiCopilot: true,
            HasCrossNetworkAutomations: true,
            IsActive: true,
            CreatedAtUtc: DateTime.UtcNow.AddMonths(-1),
            UpdatedAtUtc: null)
    ];

    public BackofficePlansPageTests()
    {
        _planService = Substitute.For<IPlanManagementService>();
        _planService.GetPlansAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<PlanDto>>.Success(MockPlans));

        Services.AddSingleton(_planService);
    }

    /// <summary>
    /// Valida que a página renderiza o catálogo com os planos cadastrados recuperados do serviço.
    /// </summary>
    [Fact]
    public void PlansPage_OnInitialized_ShouldLoadAndDisplayPlansInCatalog()
    {
        // Act
        var cut = Render<PlansPage>();

        // Assert
        cut.Find(".page-title").TextContent.Should().Contain("Gestão de Planos");
        cut.Markup.Should().Contain("Starter Growth");
        cut.Markup.Should().Contain("Agência Enterprise Scale");
        cut.Markup.Should().Contain("R$ 199,00");
        cut.Markup.Should().Contain("R$ 999,00");
    }

    /// <summary>
    /// Valida que quando não há planos cadastrados, o estado vazio explicativo é exibido com botão para criar o primeiro plano.
    /// </summary>
    [Fact]
    public void PlansPage_WhenNoPlansExist_ShouldDisplayEmptyStateAndCreateButton()
    {
        // Arrange
        _planService.GetPlansAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<PlanDto>>.Success(new List<PlanDto>()));

        // Act
        var cut = Render<PlansPage>();

        // Assert
        cut.Find(".empty-catalog-state").Should().NotBeNull();
        cut.Markup.Should().Contain("Nenhum plano cadastrado");
        cut.Find("button.btn-create-first-plan").Should().NotBeNull();
    }

    /// <summary>
    /// Valida que ao clicar em "Novo Plano", o formulário do PlanBuilder é exibido.
    /// </summary>
    [Fact]
    public void PlansPage_WhenClickingNewPlan_ShouldOpenPlanBuilder()
    {
        // Act
        var cut = Render<PlansPage>();
        var newPlanBtn = cut.Find("button.btn-new-plan");
        newPlanBtn.Click();

        // Assert
        cut.Find(".plan-builder-container").Should().NotBeNull();
        cut.Find("input[name='Name']").Should().NotBeNull();
    }

    /// <summary>
    /// Valida que ao clicar no botão "Editar" de um plano, o PlanBuilder é aberto com os dados daquele plano.
    /// </summary>
    [Fact]
    public void PlansPage_WhenClickingEditPlan_ShouldOpenPlanBuilderWithSelectedPlan()
    {
        // Arrange
        var cut = Render<PlansPage>();

        // Act
        var editButtons = cut.FindAll("button.btn-edit-plan");
        editButtons.Should().NotBeEmpty();
        editButtons.First().Click();

        // Assert
        cut.Find(".plan-builder-container").Should().NotBeNull();
        var nameInput = cut.Find("input[name='Name']");
        nameInput.GetAttribute("value").Should().Be("Starter Growth");
    }

    /// <summary>
    /// Valida que ao cancelar a criação ou edição, o PlanBuilder é fechado e o catálogo permanece visível.
    /// </summary>
    [Fact]
    public void PlansPage_WhenClickingCancelInBuilder_ShouldCloseBuilder()
    {
        // Arrange
        var cut = Render<PlansPage>();
        cut.Find("button.btn-new-plan").Click();
        cut.Find(".plan-builder-container").Should().NotBeNull();

        // Act
        var cancelBtn = cut.Find("button.btn-cancel");
        cancelBtn.Click();

        // Assert
        cut.FindAll(".plan-builder-container").Should().BeEmpty();
        cut.Find(".plans-catalog-card").Should().NotBeNull();
    }

    /// <summary>
    /// Valida que após salvar um plano com sucesso via PlanBuilder, a lista é recarregada e um banner de notificação é apresentado.
    /// </summary>
    [Fact]
    public async Task PlansPage_WhenPlanBuilderSavesSuccessfully_ShouldRefreshCatalogAndShowNotification()
    {
        // Arrange
        var newPlanId = Guid.NewGuid();
        _planService.CreatePlanAsync(Arg.Any<PlanFormViewModel>(), Arg.Any<CancellationToken>())
            .Returns(Result<Guid>.Success(newPlanId));

        var cut = Render<PlansPage>();
        cut.Find("button.btn-new-plan").Click();

        // Act
        cut.Find("input[name='Name']").Change("Novo Plano Pro Scale");
        cut.Find("form").Submit();

        // Assert
        await _planService.Received(2).GetPlansAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>());
        cut.Find(".alert-banner.alert-success").Should().NotBeNull();
        cut.Markup.Should().Contain("Novo Plano Pro Scale");
    }
}
