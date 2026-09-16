using Automations.Application.Pacing.DTOs;
using Automations.Application.Pacing.Queries.GetWorkspaceBudgetPacing;
using Automations.Application.Pacing.Queries.ListPortfolioBudgetPacing;
using Automations.Application.Pacing.Queries.SimulateBudgetPacing;
using BuildingBlocks.Domain.Automations.Pacing;
using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using WebApi.Controllers.v1;
using WebApi.Models;

namespace UnitTests.Backend.Automations;

/// <summary>
/// Testes unitários para o controlador de gestão de pacing e orçamento BudgetPacingController (Subfase 4.3).
/// Valida os contratos HTTP RESTful, códigos de status semânticos e envelope Result&lt;T&gt;.
/// </summary>
public sealed class BudgetPacingControllerTests
{
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly BudgetPacingController _controller;
    private readonly Guid _workspaceId = Guid.NewGuid();

    /// <summary>
    /// Inicializa uma nova instância de <see cref="BudgetPacingControllerTests"/>.
    /// </summary>
    public BudgetPacingControllerTests()
    {
        _controller = new BudgetPacingController(_sender);
    }

    /// <summary>
    /// Deve retornar 200 OK quando o pacing de um workspace for localizado e calculado com sucesso.
    /// </summary>
    [Fact]
    public async Task GetWorkspacePacing_WhenFound_ShouldReturnOk()
    {
        // Arrange
        var expectedDto = new WorkspaceBudgetPacingDto
        {
            WorkspaceId = _workspaceId,
            WorkspaceName = "Cliente Exemplo",
            TargetBudget = 5000m,
            CurrentSpend = 2500m,
            Status = PacingStatus.OnTrack
        };

        _sender.Send(Arg.Any<GetWorkspaceBudgetPacingQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<WorkspaceBudgetPacingDto>.Success(expectedDto));

        // Act
        var response = await _controller.GetWorkspacePacing(_workspaceId, 2026, 9, null, CancellationToken.None);

        // Assert
        var okResult = response.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);

        var result = okResult.Value as Result<WorkspaceBudgetPacingDto>;
        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeTrue();
        result.Value.WorkspaceName.Should().Be("Cliente Exemplo");
    }

    /// <summary>
    /// Deve retornar 404 NotFound quando o workspace não for localizado.
    /// </summary>
    [Fact]
    public async Task GetWorkspacePacing_WhenNotFound_ShouldReturnNotFound()
    {
        // Arrange
        _sender.Send(Arg.Any<GetWorkspaceBudgetPacingQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<WorkspaceBudgetPacingDto>.Failure(Error.NotFound("Workspace.NotFound", "Workspace não localizado.")));

        // Act
        var response = await _controller.GetWorkspacePacing(_workspaceId, 2026, 9, null, CancellationToken.None);

        // Assert
        var notFoundResult = response.Result as NotFoundObjectResult;
        notFoundResult.Should().NotBeNull();
        notFoundResult!.StatusCode.Should().Be(404);
    }

    /// <summary>
    /// Deve retornar 200 OK com o sumário executivo da carteira ao consultar portfolio.
    /// </summary>
    [Fact]
    public async Task GetPortfolioPacing_ShouldReturnOkWithSummary()
    {
        // Arrange
        var summaryDto = new PortfolioPacingSummaryDto
        {
            TotalWorkspaces = 3,
            OnTrackCount = 2,
            OverCount = 1,
            UnderCount = 0,
            TotalContractedBudget = 30_000m,
            TotalCurrentSpend = 15_000m
        };

        _sender.Send(Arg.Any<ListPortfolioBudgetPacingQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<PortfolioPacingSummaryDto>.Success(summaryDto));

        // Act
        var response = await _controller.GetPortfolioPacing(null, 2026, 9, null, CancellationToken.None);

        // Assert
        var okResult = response.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);

        var result = okResult.Value as Result<PortfolioPacingSummaryDto>;
        result!.IsSuccess.Should().BeTrue();
        result.Value.TotalWorkspaces.Should().Be(3);
    }

    /// <summary>
    /// Deve retornar 200 OK ao simular pacing dinâmico com parâmetros válidos.
    /// </summary>
    [Fact]
    public async Task SimulatePacing_WithValidPayload_ShouldReturnOk()
    {
        // Arrange
        var request = new SimulateBudgetPacingApiRequest
        {
            TargetBudget = 10_000m,
            CurrentSpend = 5_000m,
            StartDateUtc = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDateUtc = new DateTime(2026, 9, 30, 23, 59, 59, DateTimeKind.Utc),
            AsOfDateUtc = new DateTime(2026, 9, 15, 0, 0, 0, DateTimeKind.Utc)
        };

        var simulatedDto = new WorkspaceBudgetPacingDto
        {
            TargetBudget = 10_000m,
            CurrentSpend = 5_000m,
            Status = PacingStatus.OnTrack
        };

        _sender.Send(Arg.Any<SimulateBudgetPacingQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<WorkspaceBudgetPacingDto>.Success(simulatedDto));

        // Act
        var response = await _controller.SimulatePacing(request, CancellationToken.None);

        // Assert
        var okResult = response.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);

        var result = okResult.Value as Result<WorkspaceBudgetPacingDto>;
        result!.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(PacingStatus.OnTrack);
    }
}
