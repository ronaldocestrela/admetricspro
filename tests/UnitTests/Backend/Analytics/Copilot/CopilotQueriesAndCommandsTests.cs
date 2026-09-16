using Analytics.Application.Copilot.Commands.ExecuteCopilotRecommendation;
using Analytics.Application.Copilot.Queries.GetDailyDiagnostic;
using Analytics.Domain.Copilot;
using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace UnitTests.Backend.Analytics.Copilot;

/// <summary>
/// Testes unitários para as queries e commands da camada de aplicação do Copiloto de IA (Subfase 5.3.2).
/// </summary>
public sealed class CopilotQueriesAndCommandsTests
{
    private readonly ICopilotDataProvider _dataProvider;
    private readonly IAudienceOverlapDetector _overlapDetector;
    private readonly ISearchTermCannibalizationDetector _cannibalizationDetector;
    private readonly ITrafficAuditorSynthesizer _synthesizer;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="CopilotQueriesAndCommandsTests"/>.
    /// </summary>
    public CopilotQueriesAndCommandsTests()
    {
        _dataProvider = Substitute.For<ICopilotDataProvider>();
        _overlapDetector = Substitute.For<IAudienceOverlapDetector>();
        _cannibalizationDetector = Substitute.For<ISearchTermCannibalizationDetector>();
        _synthesizer = Substitute.For<ITrafficAuditorSynthesizer>();
    }

    /// <summary>
    /// Valida que o manipulador de consulta de diagnóstico diário retorna sucesso estruturado quando os dados são válidos.
    /// </summary>
    [Fact]
    public async Task GetDailyDiagnosticQueryHandler_ShouldReturnSuccess_WhenWorkspaceIsValid()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var query = new GetDailyDiagnosticQuery(workspaceId, new DateTime(2026, 9, 16));

        _dataProvider.GetMetaAdSetsForAuditAsync(workspaceId, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<AdSetAudienceTargeting>>.Success(Array.Empty<AdSetAudienceTargeting>()));

        _dataProvider.GetSearchKeywordsForAuditAsync(workspaceId, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<SearchKeywordPerformance>>.Success(Array.Empty<SearchKeywordPerformance>()));

        _overlapDetector.DetectOverlaps(Arg.Any<IReadOnlyList<AdSetAudienceTargeting>>())
            .Returns(Array.Empty<AudienceOverlapAnomaly>());

        _cannibalizationDetector.DetectCannibalization(Arg.Any<IReadOnlyList<SearchKeywordPerformance>>())
            .Returns(Array.Empty<SearchCannibalizationAnomaly>());

        var report = new DailyDiagnosticReport(
            workspaceId,
            new DateTime(2026, 9, 16),
            "Resumo Executivo do Dia",
            "Vitórias",
            "Riscos",
            Array.Empty<AudienceOverlapAnomaly>(),
            Array.Empty<SearchCannibalizationAnomaly>(),
            Array.Empty<CopilotRecommendationAction>(),
            0m);

        _synthesizer.SynthesizeDailyDiagnostic(workspaceId, Arg.Any<DateTime>(), Arg.Any<IReadOnlyList<AudienceOverlapAnomaly>>(), Arg.Any<IReadOnlyList<SearchCannibalizationAnomaly>>())
            .Returns(report);

        var handler = new GetDailyDiagnosticQueryHandler(_dataProvider, _overlapDetector, _cannibalizationDetector, _synthesizer);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.WorkspaceId.Should().Be(workspaceId);
        result.Value.ExecutiveSummary.Should().Be("Resumo Executivo do Dia");
    }

    /// <summary>
    /// Valida que a consulta falha quando o identificador do workspace for vazio.
    /// </summary>
    [Fact]
    public async Task GetDailyDiagnosticQueryHandler_ShouldReturnValidationFailure_WhenWorkspaceIsEmpty()
    {
        // Arrange
        var query = new GetDailyDiagnosticQuery(Guid.Empty);
        var handler = new GetDailyDiagnosticQueryHandler(_dataProvider, _overlapDetector, _cannibalizationDetector, _synthesizer);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Copilot.InvalidWorkspaceId");
    }

    /// <summary>
    /// Valida execução em 1 clique bem-sucedida pelo manipulador de comando.
    /// </summary>
    [Fact]
    public async Task ExecuteCopilotRecommendationCommandHandler_ShouldReturnSuccess_WhenCommandIsValid()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var actionId = Guid.NewGuid();
        var targetEntityId = Guid.NewGuid();

        var command = new ExecuteCopilotRecommendationCommand(
            WorkspaceId: workspaceId,
            ActionId: actionId,
            ActionType: CopilotActionType.PauseAdSet,
            TargetEntityId: targetEntityId,
            TargetEntityName: "Conjunto Redundante Teste",
            Platform: "MetaAds",
            Title: "Pausar conjunto",
            Description: "Pausar conjunto redundante para conter CPM"
        );

        _dataProvider.ExecuteRecommendationActionAsync(workspaceId, Arg.Any<CopilotRecommendationAction>(), Arg.Any<CancellationToken>())
            .Returns(Result<bool>.Success(true));

        var handler = new ExecuteCopilotRecommendationCommandHandler(_dataProvider);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.ActionId.Should().Be(actionId);
        result.Value.Success.Should().BeTrue();
        result.Value.Message.Should().Contain("executada com sucesso em 1 clique");
    }

    /// <summary>
    /// Valida que a execução de ação falha na validação caso a entidade alvo seja Guid vazio.
    /// </summary>
    [Fact]
    public async Task ExecuteCopilotRecommendationCommandHandler_ShouldReturnValidationFailure_WhenTargetEntityIsEmpty()
    {
        // Arrange
        var command = new ExecuteCopilotRecommendationCommand(
            WorkspaceId: Guid.NewGuid(),
            ActionId: Guid.NewGuid(),
            ActionType: CopilotActionType.PauseAdSet,
            TargetEntityId: Guid.Empty,
            TargetEntityName: "Invalido",
            Platform: "MetaAds",
            Title: "Pausar",
            Description: "Desc"
        );

        var handler = new ExecuteCopilotRecommendationCommandHandler(_dataProvider);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Copilot.InvalidTargetEntityId");
    }
}
