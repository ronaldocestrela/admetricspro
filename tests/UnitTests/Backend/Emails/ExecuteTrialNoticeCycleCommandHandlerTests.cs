using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using Master.Application.Billing.Trial;
using NSubstitute;
using Xunit;

namespace UnitTests.Backend.Emails;

/// <summary>
/// Testes unitários para o comando <see cref="ExecuteTrialNoticeCycleCommand"/>.
/// </summary>
public sealed class ExecuteTrialNoticeCycleCommandHandlerTests
{
    private readonly ITrialNotificationEngineService _engineService = Substitute.For<ITrialNotificationEngineService>();

    /// <summary>
    /// Valida que o comando delega a execução para o motor de notificações e repassa o sumário retornado.
    /// </summary>
    [Fact]
    public async Task Handle_WhenExecuted_ShouldDelegateToEngineService()
    {
        // Arrange
        var referenceUtc = DateTime.UtcNow;
        var expectedSummary = new TrialNoticeExecutionSummary(10, 2, 1, 1, 0, 0, referenceUtc);

        _engineService.ProcessTrialNoticesCycleAsync(referenceUtc, Arg.Any<CancellationToken>())
            .Returns(Result<TrialNoticeExecutionSummary>.Success(expectedSummary));

        var handler = new ExecuteTrialNoticeCycleCommandHandler(_engineService);
        var command = new ExecuteTrialNoticeCycleCommand(referenceUtc);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.EvaluatedCount.Should().Be(10);
        result.Value.TotalNoticesSent.Should().Be(4);
        await _engineService.Received(1).ProcessTrialNoticesCycleAsync(referenceUtc, Arg.Any<CancellationToken>());
    }
}
