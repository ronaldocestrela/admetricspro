using FluentAssertions;
using Master.Application.Integrations.Commands.RecordApiConsumption;
using Master.Application.Integrations.DTOs;
using Master.Application.Integrations.Services;
using Master.Domain.Integrations;
using NSubstitute;
using BuildingBlocks.Domain.Primitives;

namespace UnitTests.Backend.Integrations;

/// <summary>
/// Unit tests for <see cref="RecordApiConsumptionCommandHandler"/> and validator.
/// </summary>
public sealed class RecordApiConsumptionCommandHandlerTests
{
    private readonly IApiQuotaTrackerService _quotaService = Substitute.For<IApiQuotaTrackerService>();
    private readonly RecordApiConsumptionCommandHandler _handler;

    /// <summary>
    /// Initializes test dependencies.
    /// </summary>
    public RecordApiConsumptionCommandHandlerTests()
    {
        _handler = new RecordApiConsumptionCommandHandler(_quotaService);
    }

    /// <summary>
    /// Verifies validator rejects zero or negative units.
    /// </summary>
    /// <param name="units">Units to test.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Validator_ShouldFail_WhenUnitsAreZeroOrNegative(long units)
    {
        // Arrange
        var validator = new RecordApiConsumptionCommandValidator();
        var command = new RecordApiConsumptionCommand(AdPlatform.Meta, units);

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RecordApiConsumptionCommand.Units));
    }

    /// <summary>
    /// Verifies handler delegates to quota tracker service and returns status DTO.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldDelegateToQuotaTrackerService_AndReturnStatus()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var command = new RecordApiConsumptionCommand(AdPlatform.Meta, 500, now);

        var expectedDto = new PlatformQuotaStatusDto(
            Platform: AdPlatform.Meta,
            PlatformName: "Meta Graph API",
            MaxLimit: 1000,
            CurrentConsumption: 500,
            UsagePercentage: 50.0,
            AlertLevel: QuotaAlertLevel.Normal,
            IsWarning: false,
            WindowDuration: TimeSpan.FromHours(1),
            WindowStartUtc: now,
            LastUpdatedUtc: now);

        _quotaService.RecordUsageAsync(AdPlatform.Meta, 500, now, Arg.Any<CancellationToken>())
            .Returns(Result<PlatformQuotaStatusDto>.Success(expectedDto));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(expectedDto);
        await _quotaService.Received(1).RecordUsageAsync(AdPlatform.Meta, 500, now, Arg.Any<CancellationToken>());
    }
}
