using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using Master.Application.Plans.Commands.CreatePlan;
using Master.Application.Repositories;
using Master.Domain.Plans;
using Master.Domain.Tenants;
using NSubstitute;

namespace UnitTests.Backend.Plans;

/// <summary>
/// Unit tests for <see cref="CreatePlanCommandHandler"/>.
/// </summary>
public sealed class CreatePlanCommandHandlerTests
{
    private readonly IPlanRepository _planRepository = Substitute.For<IPlanRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    /// <summary>
    /// Verifies that handler successfully creates and persists a plan when input is valid and name is unique.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenPlanIsValidAndNameIsUnique()
    {
        // Arrange
        var handler = new CreatePlanCommandHandler(_planRepository, _unitOfWork);
        var command = new CreatePlanCommand(
            Name: "Agência Starter",
            Description: "Plano para pequenas agências",
            Tier: SubscriptionTier.Starter,
            MonthlyPrice: 199.00m,
            AnnualDiscountPercentage: 10,
            MaxSeats: 5,
            MaxWorkspaces: 2,
            MonthlyAdSpendCap: 15_000m,
            HasWhiteLabel: false,
            HasCustomCname: false,
            HasAiCopilot: false,
            HasCrossNetworkAutomations: true);

        _planRepository.ExistsByNameAsync(command.Name, null, Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        await _planRepository.Received(1).AddAsync(Arg.Any<SubscriptionPlan>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that handler returns a conflict error when a plan with the same name already exists.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenPlanNameAlreadyExists()
    {
        // Arrange
        var handler = new CreatePlanCommandHandler(_planRepository, _unitOfWork);
        var command = new CreatePlanCommand(
            Name: "Agência Existente",
            Description: "Plano duplicado",
            Tier: SubscriptionTier.Starter,
            MonthlyPrice: 199.00m,
            AnnualDiscountPercentage: 10,
            MaxSeats: 5,
            MaxWorkspaces: 2,
            MonthlyAdSpendCap: 15_000m,
            HasWhiteLabel: false,
            HasCustomCname: false,
            HasAiCopilot: false,
            HasCrossNetworkAutomations: true);

        _planRepository.ExistsByNameAsync(command.Name, null, Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Plan.NameAlreadyExists");
        await _planRepository.DidNotReceive().AddAsync(Arg.Any<SubscriptionPlan>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that handler returns a validation error when limits violate business invariants.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenLimitsAreInvalid()
    {
        // Arrange
        var handler = new CreatePlanCommandHandler(_planRepository, _unitOfWork);
        var command = new CreatePlanCommand(
            Name: "Plano Assentos Zero",
            Description: "Inválido",
            Tier: SubscriptionTier.Starter,
            MonthlyPrice: 199.00m,
            AnnualDiscountPercentage: 10,
            MaxSeats: 0,
            MaxWorkspaces: 2,
            MonthlyAdSpendCap: 15_000m,
            HasWhiteLabel: false,
            HasCustomCname: false,
            HasAiCopilot: false,
            HasCrossNetworkAutomations: true);

        _planRepository.ExistsByNameAsync(command.Name, null, Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Plan.InvalidSeats");
        await _planRepository.DidNotReceive().AddAsync(Arg.Any<SubscriptionPlan>(), Arg.Any<CancellationToken>());
    }
}
