using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using Master.Application.Billing.Dunning;
using NSubstitute;

namespace UnitTests.Backend.Dunning;

/// <summary>
/// Unit tests for <see cref="ExecuteDunningCycleCommandHandler"/>.
/// </summary>
public sealed class ExecuteDunningCycleCommandHandlerTests
{
    private readonly IDunningEngineService _dunningEngineService = Substitute.For<IDunningEngineService>();

    /// <summary>
    /// Verifies that the command handler delegates execution to the dunning engine service.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldDelegateToDunningEngineService()
    {
        // Arrange
        var referenceUtc = DateTime.UtcNow;
        var command = new ExecuteDunningCycleCommand(referenceUtc);
        var expectedSummary = new DunningExecutionSummary(5, 2, 1, 3, referenceUtc);

        _dunningEngineService.ProcessDunningCycleAsync(referenceUtc, Arg.Any<CancellationToken>())
            .Returns(Result<DunningExecutionSummary>.Success(expectedSummary));

        var handler = new ExecuteDunningCycleCommandHandler(_dunningEngineService);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expectedSummary);
        await _dunningEngineService.Received(1).ProcessDunningCycleAsync(referenceUtc, Arg.Any<CancellationToken>());
    }
}
