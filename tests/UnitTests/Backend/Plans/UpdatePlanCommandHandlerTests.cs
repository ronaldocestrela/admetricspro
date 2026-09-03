using BuildingBlocks.Application.Persistence;
using FluentAssertions;
using Master.Application.Plans.Commands.UpdatePlan;
using Master.Application.Repositories;
using Master.Domain.Plans;
using Master.Domain.Tenants;
using NSubstitute;

namespace UnitTests.Backend.Plans;

/// <summary>
/// Unit tests for <see cref="UpdatePlanCommandHandler"/>.
/// </summary>
public sealed class UpdatePlanCommandHandlerTests
{
    private readonly IPlanRepository _planRepository = Substitute.For<IPlanRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    /// <summary>
    /// Verifies that handler returns a not found error when target plan does not exist.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenPlanDoesNotExist()
    {
        // Arrange
        var handler = new UpdatePlanCommandHandler(_planRepository, _unitOfWork);
        var planId = Guid.NewGuid();
        var command = new UpdatePlanCommand(
            PlanId: planId,
            Name: "Novo Nome",
            Description: "Nova Desc",
            MonthlyPrice: 250m,
            AnnualDiscountPercentage: 15,
            MaxSeats: 10,
            MaxWorkspaces: 5,
            MonthlyAdSpendCap: 25_000m,
            HasWhiteLabel: true,
            HasCustomCname: false,
            HasAiCopilot: false,
            HasCrossNetworkAutomations: true);

        _planRepository.GetByIdAsync(Arg.Any<PlanId>(), Arg.Any<CancellationToken>())
            .Returns((SubscriptionPlan?)null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Plan.NotFound");
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that handler returns a conflict error when updating to a name used by another plan.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenNameConflictsWithAnotherPlan()
    {
        // Arrange
        var handler = new UpdatePlanCommandHandler(_planRepository, _unitOfWork);
        var limits = PlanLimits.Create(5, 2, 10_000m).Value;
        var features = PlanFeatures.Default();
        var existingPlan = SubscriptionPlan.Create("Plano Original", "Desc", SubscriptionTier.Starter, 100m, 10, limits, features).Value;

        _planRepository.GetByIdAsync(Arg.Is<PlanId>(id => id.Value == existingPlan.Id.Value), Arg.Any<CancellationToken>())
            .Returns(existingPlan);

        _planRepository.ExistsByNameAsync("Nome Conflitante", Arg.Any<PlanId>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var command = new UpdatePlanCommand(
            PlanId: existingPlan.Id.Value,
            Name: "Nome Conflitante",
            Description: "Desc",
            MonthlyPrice: 200m,
            AnnualDiscountPercentage: 10,
            MaxSeats: 5,
            MaxWorkspaces: 2,
            MonthlyAdSpendCap: 10_000m,
            HasWhiteLabel: false,
            HasCustomCname: false,
            HasAiCopilot: false,
            HasCrossNetworkAutomations: false);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Plan.NameAlreadyExists");
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that handler successfully updates the plan and commits the transaction.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenUpdateIsValid()
    {
        // Arrange
        var handler = new UpdatePlanCommandHandler(_planRepository, _unitOfWork);
        var limits = PlanLimits.Create(5, 2, 10_000m).Value;
        var features = PlanFeatures.Default();
        var existingPlan = SubscriptionPlan.Create("Plano Original", "Desc", SubscriptionTier.Starter, 100m, 10, limits, features).Value;

        _planRepository.GetByIdAsync(Arg.Is<PlanId>(id => id.Value == existingPlan.Id.Value), Arg.Any<CancellationToken>())
            .Returns(existingPlan);

        _planRepository.ExistsByNameAsync("Plano Atualizado", Arg.Any<PlanId>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var command = new UpdatePlanCommand(
            PlanId: existingPlan.Id.Value,
            Name: "Plano Atualizado",
            Description: "Nova Descrição",
            MonthlyPrice: 350m,
            AnnualDiscountPercentage: 20,
            MaxSeats: 15,
            MaxWorkspaces: 10,
            MonthlyAdSpendCap: 100_000m,
            HasWhiteLabel: true,
            HasCustomCname: true,
            HasAiCopilot: true,
            HasCrossNetworkAutomations: true);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        existingPlan.Name.Should().Be("Plano Atualizado");
        existingPlan.MonthlyPrice.Should().Be(350m);
        existingPlan.Limits.MaxSeats.Should().Be(15);
        existingPlan.Features.HasAiCopilot.Should().BeTrue();
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}
