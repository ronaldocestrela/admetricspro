using Automations.Application.SafetyGuards.Commands.CheckLandingPages;
using Automations.Application.SafetyGuards.Commands.CheckOverspending;
using Automations.Application.SafetyGuards.DTOs;
using Automations.Application.SafetyGuards.Queries.ListSafetyIncidents;
using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using WebApi.Controllers.v1;
using WebApi.Models;
using Xunit;

namespace UnitTests.Backend.Automations;

/// <summary>
/// Testes unitários para o controlador de travas de segurança SecurityGuardsController (Subfase 4.2).
/// Valida os contratos HTTP RESTful, códigos de status semânticos e envelope Result&lt;T&gt;.
/// </summary>
public sealed class SecurityGuardsControllerTests
{
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly SecurityGuardsController _controller;
    private readonly Guid _workspaceId = Guid.NewGuid();

    /// <summary>
    /// Inicializa o controlador com mock do MediatR.
    /// </summary>
    public SecurityGuardsControllerTests()
    {
        _controller = new SecurityGuardsController(_sender);
    }

    /// <summary>
    /// Deve retornar 200 OK quando a verificação de Overspending for executada com sucesso.
    /// </summary>
    [Fact]
    public async Task CheckOverspending_ShouldReturnOk_WhenSuccessful()
    {
        // Arrange
        var request = new CheckOverspendingApiRequest(_workspaceId, 1.20m);
        var expectedResult = new CheckOverspendingResultDto(
            _workspaceId,
            EvaluatedCampaignsCount: 5,
            ViolatedCampaignsCount: 1,
            PausedCampaignsCount: 1,
            Incidents: Array.Empty<SafetyIncidentDto>());

        _sender.Send(Arg.Any<CheckOverspendingCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<CheckOverspendingResultDto>.Success(expectedResult)));

        // Act
        var response = await _controller.CheckOverspending(request, CancellationToken.None);

        // Assert
        var okResult = response.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);

        var result = okResult.Value as Result<CheckOverspendingResultDto>;
        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeTrue();
        result.Value.EvaluatedCampaignsCount.Should().Be(5);
        result.Value.ViolatedCampaignsCount.Should().Be(1);
    }

    /// <summary>
    /// Deve retornar 400 BadRequest quando a verificação de Overspending falhar.
    /// </summary>
    [Fact]
    public async Task CheckOverspending_ShouldReturnBadRequest_WhenFailed()
    {
        // Arrange
        var request = new CheckOverspendingApiRequest(_workspaceId, 1.20m);
        _sender.Send(Arg.Any<CheckOverspendingCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<CheckOverspendingResultDto>.Failure(
                Error.Validation("Workspace.Invalid", "Workspace inválido"))));

        // Act
        var response = await _controller.CheckOverspending(request, CancellationToken.None);

        // Assert
        var badRequestResult = response.Result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
        badRequestResult!.StatusCode.Should().Be(400);
    }

    /// <summary>
    /// Deve retornar 200 OK quando a verificação de Landing Pages for executada com sucesso.
    /// </summary>
    [Fact]
    public async Task CheckLandingPages_ShouldReturnOk_WhenSuccessful()
    {
        // Arrange
        var request = new CheckLandingPagesApiRequest(_workspaceId);
        var expectedResult = new CheckLandingPagesResultDto(
            _workspaceId,
            EvaluatedAdsCount: 10,
            HealthyAdsCount: 8,
            BrokenAdsCount: 2,
            PausedAdsCount: 2,
            Incidents: Array.Empty<SafetyIncidentDto>());

        _sender.Send(Arg.Any<CheckLandingPagesCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<CheckLandingPagesResultDto>.Success(expectedResult)));

        // Act
        var response = await _controller.CheckLandingPages(request, CancellationToken.None);

        // Assert
        var okResult = response.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);

        var result = okResult.Value as Result<CheckLandingPagesResultDto>;
        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeTrue();
        result.Value.EvaluatedAdsCount.Should().Be(10);
        result.Value.BrokenAdsCount.Should().Be(2);
    }

    /// <summary>
    /// Deve retornar 200 OK com lista de incidentes quando a consulta for bem-sucedida.
    /// </summary>
    [Fact]
    public async Task ListIncidents_ShouldReturnOk_WithIncidents()
    {
        // Arrange
        var incidents = new List<SafetyIncidentDto>();
        _sender.Send(Arg.Any<ListSafetyIncidentsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<SafetyIncidentDto>>.Success(incidents)));

        // Act
        var response = await _controller.ListIncidents(_workspaceId, limit: 20);

        // Assert
        var okResult = response.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);

        var result = okResult.Value as Result<IReadOnlyList<SafetyIncidentDto>>;
        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeTrue();
    }
}
