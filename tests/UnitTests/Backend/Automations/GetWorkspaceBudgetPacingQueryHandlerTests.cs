using Automations.Application.Pacing.Queries.GetWorkspaceBudgetPacing;
using Automations.Application.Pacing.Services;
using Automations.Domain.Pacing;
using BuildingBlocks.Domain.Automations.Pacing;
using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using NSubstitute;

namespace UnitTests.Backend.Automations;

/// <summary>
/// Testes unitários para o manipulador GetWorkspaceBudgetPacingQueryHandler.
/// </summary>
public sealed class GetWorkspaceBudgetPacingQueryHandlerTests
{
    private readonly IBudgetPacingDataProvider _dataProvider = Substitute.For<IBudgetPacingDataProvider>();
    private readonly IBudgetPacingCalculator _calculator = Substitute.For<IBudgetPacingCalculator>();
    private readonly GetWorkspaceBudgetPacingQueryHandler _handler;

    /// <summary>
    /// Inicializa a suíte de testes do handler de pacing de workspace.
    /// </summary>
    public GetWorkspaceBudgetPacingQueryHandlerTests()
    {
        _handler = new GetWorkspaceBudgetPacingQueryHandler(_dataProvider, _calculator);
    }

    /// <summary>
    /// Valida que ao consultar um workspace com ID vazio, retorna erro de validação.
    /// </summary>
    [Fact]
    public async Task Handle_WithEmptyWorkspaceId_ShouldReturnValidationFailure()
    {
        // Arrange
        var query = new GetWorkspaceBudgetPacingQuery(Guid.Empty);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Pacing.InvalidWorkspaceId");
    }

    /// <summary>
    /// Valida que ao consultar um mês inválido (&lt; 1 ou &gt; 12), retorna erro de validação.
    /// </summary>
    [Fact]
    public async Task Handle_WithInvalidMonth_ShouldReturnValidationFailure()
    {
        // Arrange
        var query = new GetWorkspaceBudgetPacingQuery(Guid.NewGuid(), 2026, 13);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Pacing.InvalidMonth");
    }

    /// <summary>
    /// Valida o fluxo de sucesso de consulta e cálculo de pacing com detalhamento por campanha.
    /// </summary>
    [Fact]
    public async Task Handle_WhenWorkspaceExists_ShouldReturnPacingDtoWithCampaignBreakdown()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var asOfDate = new DateTime(2026, 9, 15, 12, 0, 0, DateTimeKind.Utc);
        var query = new GetWorkspaceBudgetPacingQuery(workspaceId, 2026, 9, asOfDate);

        var campaign1 = new CampaignSpendRawData(Guid.NewGuid(), "Campanha Meta Top", "MetaAds", 100m, 1500m);
        var rawData = new WorkspacePacingRawData(workspaceId, "Loja Alpha", 10_000m, 5_000m, new[] { campaign1 });

        _dataProvider.GetWorkspacePacingDataAsync(
            workspaceId,
            Arg.Any<DateTime>(),
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>())
            .Returns(Result<WorkspacePacingRawData>.Success(rawData));

        var calcResult = new BudgetPacingCalculationResult(
            10_000m,
            5_000m,
            5_000m,
            30,
            15,
            15,
            5_000m,
            1.0m,
            100m,
            333.33m,
            333.33m,
            333.33m,
            10_000m,
            0m,
            0m,
            PacingStatus.OnTrack,
            "No Ritmo");

        _calculator.Calculate(
            10_000m,
            5_000m,
            Arg.Any<DateTime>(),
            Arg.Any<DateTime>(),
            asOfDate,
            0.10m)
            .Returns(Result<BudgetPacingCalculationResult>.Success(calcResult));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var dto = result.Value;

        dto.WorkspaceId.Should().Be(workspaceId);
        dto.WorkspaceName.Should().Be("Loja Alpha");
        dto.TargetBudget.Should().Be(10_000m);
        dto.CurrentSpend.Should().Be(5_000m);
        dto.Status.Should().Be(PacingStatus.OnTrack);
        dto.CampaignBreakdown.Should().HaveCount(1);
        dto.CampaignBreakdown[0].CampaignName.Should().Be("Campanha Meta Top");
    }
}
