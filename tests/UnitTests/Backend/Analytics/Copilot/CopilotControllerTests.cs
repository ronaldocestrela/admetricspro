using Analytics.Application.Copilot.Commands.ExecuteCopilotRecommendation;
using Analytics.Application.Copilot.DTOs;
using Analytics.Application.Copilot.Queries.GetDailyDiagnostic;
using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using WebApi.Controllers.v1;
using WebApi.Models;
using Xunit;

namespace UnitTests.Backend.Analytics.Copilot;

/// <summary>
/// Testes unitários para o controlador <see cref="CopilotController"/> (Subfase 5.3).
/// </summary>
public sealed class CopilotControllerTests
{
    private readonly ISender _sender;
    private readonly CopilotController _controller;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="CopilotControllerTests"/>.
    /// </summary>
    public CopilotControllerTests()
    {
        _sender = Substitute.For<ISender>();
        _controller = new CopilotController(_sender);
    }

    /// <summary>
    /// Valida que GetDiagnostic retorna status HTTP 200 OK com o relatório diário quando a consulta é bem-sucedida.
    /// </summary>
    [Fact]
    public async Task GetDiagnostic_ShouldReturnOk_WhenQuerySucceeds()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var reportDto = new DailyDiagnosticReportDto(
            WorkspaceId: workspaceId,
            ReportDate: DateTime.UtcNow.Date,
            ExecutiveSummary: "Resumo",
            WinsSummary: "Vitórias",
            RisksSummary: "Riscos",
            AudienceOverlapAnomalies: Array.Empty<AudienceOverlapAnomalyDto>(),
            SearchCannibalizationAnomalies: Array.Empty<SearchCannibalizationAnomalyDto>(),
            Actions: Array.Empty<CopilotRecommendationActionDto>(),
            EstimatedMonthlySavings: 500m,
            CriticalAnomaliesCount: 0,
            HighAnomaliesCount: 0
        );

        _sender.Send(Arg.Any<GetDailyDiagnosticQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<DailyDiagnosticReportDto>.Success(reportDto));

        // Act
        var actionResult = await _controller.GetDiagnostic(workspaceId, null);

        // Assert
        actionResult.Result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)actionResult.Result!;
        okResult.Value.Should().BeOfType<Result<DailyDiagnosticReportDto>>();
        var value = (Result<DailyDiagnosticReportDto>)okResult.Value!;
        value.IsSuccess.Should().BeTrue();
        value.Value.EstimatedMonthlySavings.Should().Be(500m);
    }

    /// <summary>
    /// Valida que GetDiagnostic retorna HTTP 400 BadRequest quando a consulta falha na regra de negócio.
    /// </summary>
    [Fact]
    public async Task GetDiagnostic_ShouldReturnBadRequest_WhenQueryFails()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        _sender.Send(Arg.Any<GetDailyDiagnosticQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<DailyDiagnosticReportDto>.Failure(Error.Validation("Copilot.Error", "Falha de validação")));

        // Act
        var actionResult = await _controller.GetDiagnostic(workspaceId, null);

        // Assert
        actionResult.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Valida que ExecuteAction retorna HTTP 200 OK quando a execução em 1 clique é concluída.
    /// </summary>
    [Fact]
    public async Task ExecuteAction_ShouldReturnOk_WhenExecutionSucceeds()
    {
        // Arrange
        var request = new ExecuteCopilotActionApiRequest(
            workspaceId: Guid.NewGuid(),
            actionId: Guid.NewGuid(),
            actionType: "PauseAdSet",
            targetEntityId: Guid.NewGuid(),
            targetEntityName: "Conjunto Teste",
            platform: "MetaAds",
            title: "Pausar conjunto",
            description: "Descrição da ação"
        );

        var resultDto = new ExecuteCopilotActionResultDto(
            ActionId: request.ActionId,
            Success: true,
            Message: "Sucesso",
            ExecutedAtUtc: DateTime.UtcNow
        );

        _sender.Send(Arg.Any<ExecuteCopilotRecommendationCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<ExecuteCopilotActionResultDto>.Success(resultDto));

        // Act
        var actionResult = await _controller.ExecuteAction(request);

        // Assert
        actionResult.Result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)actionResult.Result!;
        var value = (Result<ExecuteCopilotActionResultDto>)okResult.Value!;
        value.IsSuccess.Should().BeTrue();
        value.Value.Success.Should().BeTrue();
    }

    /// <summary>
    /// Valida que ExecuteAction retorna HTTP 400 BadRequest quando o corpo da requisição é nulo.
    /// </summary>
    [Fact]
    public async Task ExecuteAction_ShouldReturnBadRequest_WhenRequestIsNull()
    {
        // Act
        var actionResult = await _controller.ExecuteAction(null!);

        // Assert
        actionResult.Result.Should().BeOfType<BadRequestObjectResult>();
    }
}
