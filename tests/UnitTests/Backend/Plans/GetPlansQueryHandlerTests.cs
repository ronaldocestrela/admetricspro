using FluentAssertions;
using Master.Application.Plans.DTOs;
using Master.Application.Plans.Queries.GetPlanById;
using Master.Application.Plans.Queries.GetPlans;
using Master.Application.Repositories;
using Master.Domain.Plans;
using NSubstitute;

namespace UnitTests.Backend.Plans;

/// <summary>
/// Unit tests for <see cref="GetPlansQueryHandler"/> and <see cref="GetPlanByIdQueryHandler"/>.
/// </summary>
public sealed class GetPlansQueryHandlerTests
{
    private readonly IPlanReadOnlyRepository _readOnlyRepository = Substitute.For<IPlanReadOnlyRepository>();

    /// <summary>
    /// Verifies that handler returns a list of plan DTOs from read-only repository.
    /// </summary>
    [Fact]
    public async Task GetPlansQueryHandler_ShouldReturnListOfPlans()
    {
        // Arrange
        var handler = new GetPlansQueryHandler(_readOnlyRepository);
        var expectedPlans = new List<PlanDto>
        {
            new(
                Guid.NewGuid(), "Starter", "Desc", "Starter", 99m, 10,
                5, 2, 10_000m, false, false, false, false, true, DateTime.UtcNow, null),
            new(
                Guid.NewGuid(), "Pro", "Desc", "Pro", 299m, 15,
                15, 10, 50_000m, true, true, true, true, true, DateTime.UtcNow, null)
        };

        _readOnlyRepository.ListAllAsync(false, Arg.Any<CancellationToken>())
            .Returns(expectedPlans);

        // Act
        var result = await handler.Handle(new GetPlansQuery(false), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().BeEquivalentTo(expectedPlans);
    }

    /// <summary>
    /// Verifies that handler returns plan DTO when plan is found by id.
    /// </summary>
    [Fact]
    public async Task GetPlanByIdQueryHandler_ShouldReturnPlan_WhenFound()
    {
        // Arrange
        var handler = new GetPlanByIdQueryHandler(_readOnlyRepository);
        var planId = Guid.NewGuid();
        var expectedPlan = new PlanDto(
            planId, "Pro", "Desc", "Pro", 299m, 15,
            15, 10, 50_000m, true, true, true, true, true, DateTime.UtcNow, null);

        _readOnlyRepository.GetByIdAsync(Arg.Is<PlanId>(id => id.Value == planId), Arg.Any<CancellationToken>())
            .Returns(expectedPlan);

        // Act
        var result = await handler.Handle(new GetPlanByIdQuery(planId), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be(planId);
    }

    /// <summary>
    /// Verifies that handler returns null value without throwing when plan is not found.
    /// </summary>
    [Fact]
    public async Task GetPlanByIdQueryHandler_ShouldReturnNullValue_WhenNotFound()
    {
        // Arrange
        var handler = new GetPlanByIdQueryHandler(_readOnlyRepository);
        var planId = Guid.NewGuid();

        _readOnlyRepository.GetByIdAsync(Arg.Any<PlanId>(), Arg.Any<CancellationToken>())
            .Returns((PlanDto?)null);

        // Act
        var result = await handler.Handle(new GetPlanByIdQuery(planId), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }
}
