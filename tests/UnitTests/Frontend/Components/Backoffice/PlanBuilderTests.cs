using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using UnitTests.Frontend.Common;
using WebApp.Components.Backoffice;
using WebApp.Models;
using WebApp.Services;
using BuildingBlocks.Domain.Primitives;
using Master.Domain.Tenants;

namespace UnitTests.Frontend.Components.Backoffice;

/// <summary>
/// Unit and component tests using bUnit for <see cref="PlanBuilder"/> component.
/// </summary>
public sealed class PlanBuilderTests : BunitTestBase
{
    private readonly IPlanManagementService _planService = Substitute.For<IPlanManagementService>();

    /// <summary>
    /// Initializes test dependencies including the mocked plan management service.
    /// </summary>
    public PlanBuilderTests()
    {
        Services.AddSingleton<IPlanManagementService>(_planService);
    }

    /// <summary>
    /// Verifies that the builder renders default inputs for a new plan.
    /// </summary>
    [Fact]
    public void PlanBuilder_ShouldRenderCreateForm_WhenModelIsNull()
    {
        // Act
        var cut = Render<PlanBuilder>();

        // Assert
        cut.Find(".plan-builder-container").Should().NotBeNull();
        cut.Find("input[name='Name']").Should().NotBeNull();
        cut.Find("select[name='Tier']").Should().NotBeNull();
        cut.Find("input[name='MonthlyPrice']").Should().NotBeNull();
        cut.Find("input[name='MaxSeats']").Should().NotBeNull();
        cut.Find("input[name='MaxWorkspaces']").Should().NotBeNull();
        cut.Find("input[name='MonthlyAdSpendCap']").Should().NotBeNull();
        cut.Find("button[type='submit']").TextContent.Should().Contain("Salvar Plano");
    }

    /// <summary>
    /// Verifies that passing an existing plan model populates input fields.
    /// </summary>
    [Fact]
    public void PlanBuilder_ShouldPopulateFields_WhenEditingExistingPlan()
    {
        // Arrange
        var model = new PlanFormViewModel
        {
            PlanId = Guid.NewGuid(),
            Name = "Plano Enterprise Custom",
            Description = "Plano customizado",
            Tier = SubscriptionTier.Enterprise,
            MonthlyPrice = 1499.00m,
            AnnualDiscountPercentage = 25,
            MaxSeats = 50,
            MaxWorkspaces = 20,
            MonthlyAdSpendCap = 500_000m,
            HasWhiteLabel = true,
            HasCustomCname = true,
            HasAiCopilot = true,
            HasCrossNetworkAutomations = true
        };

        // Act
        var cut = Render<PlanBuilder>(parameters => parameters.Add(p => p.Model, model));

        // Assert
        var nameInput = cut.Find("input[name='Name']");
        nameInput.GetAttribute("value").Should().Be("Plano Enterprise Custom");

        var whiteLabelCheckbox = cut.Find("input[name='HasWhiteLabel']");
        whiteLabelCheckbox.HasAttribute("checked").Should().BeTrue();
    }

    /// <summary>
    /// Verifies that clicking cancel triggers the OnCancel callback.
    /// </summary>
    [Fact]
    public void PlanBuilder_ClickingCancel_ShouldTriggerOnCancelCallback()
    {
        // Arrange
        var cancelTriggered = false;
        var cut = Render<PlanBuilder>(parameters => parameters
            .Add(p => p.OnCancel, () => { cancelTriggered = true; }));

        // Act
        var cancelButton = cut.Find("button.btn-cancel");
        cancelButton.Click();

        // Assert
        cancelTriggered.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that successful submission calls create service and triggers OnSaveSuccess.
    /// </summary>
    [Fact]
    public async Task PlanBuilder_SubmitValidNewPlan_ShouldCallServiceAndTriggerOnSaveSuccess()
    {
        // Arrange
        var createdId = Guid.NewGuid();
        _planService.CreatePlanAsync(Arg.Any<PlanFormViewModel>(), Arg.Any<CancellationToken>())
            .Returns(Result<Guid>.Success(createdId));

        PlanFormViewModel? savedModel = null;
        var cut = Render<PlanBuilder>(parameters => parameters
            .Add(p => p.OnSaveSuccess, (PlanFormViewModel model) => { savedModel = model; }));

        // Act
        var nameInput = cut.Find("input[name='Name']");
        nameInput.Change("Plano Agência Plus");

        var form = cut.Find("form");
        form.Submit();

        // Assert
        await _planService.Received(1).CreatePlanAsync(Arg.Any<PlanFormViewModel>(), Arg.Any<CancellationToken>());
        savedModel.Should().NotBeNull();
        savedModel!.Name.Should().Be("Plano Agência Plus");
    }

    /// <summary>
    /// Verifies that service failure displays error message without throwing exception.
    /// </summary>
    [Fact]
    public void PlanBuilder_SubmitWithServiceFailure_ShouldDisplayErrorBanner()
    {
        // Arrange
        _planService.CreatePlanAsync(Arg.Any<PlanFormViewModel>(), Arg.Any<CancellationToken>())
            .Returns(Result<Guid>.Failure(Error.Conflict("Plan.NameAlreadyExists", "Nome de plano já cadastrado.")));

        var cut = Render<PlanBuilder>();

        // Act
        var nameInput = cut.Find("input[name='Name']");
        nameInput.Change("Plano Duplicado");

        var form = cut.Find("form");
        form.Submit();

        // Assert
        var errorBanner = cut.Find(".plan-error-banner");
        errorBanner.TextContent.Should().Contain("Nome de plano já cadastrado.");
    }
}
