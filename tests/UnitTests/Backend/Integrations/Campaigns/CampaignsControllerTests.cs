using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using Integrations.Application.Campaigns.Commands.SyncCampaignHierarchy;
using Integrations.Application.Campaigns.DTOs;
using Integrations.Application.Campaigns.Queries.GetCampaignHierarchy;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using WebApi.Controllers.v1;
using WebApi.Models;

namespace UnitTests.Backend.Integrations.Campaigns;

/// <summary>
/// Testes unitários para o controlador REST <see cref="CampaignsController"/>.
/// </summary>
public sealed class CampaignsControllerTests
{
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly CampaignsController _controller;

    /// <summary>
    /// Inicializa o controlador com mediador mockado.
    /// </summary>
    public CampaignsControllerTests()
    {
        _controller = new CampaignsController(_sender);
    }

    /// <summary>
    /// Valida que requisição com payload nulo retorna HTTP 400 Bad Request.
    /// </summary>
    [Fact]
    public async Task SyncCampaigns_WhenRequestIsNull_ShouldReturnBadRequest()
    {
        // Act
        var result = await _controller.SyncCampaigns(null!);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Valida que a sincronização bem-sucedida retorna HTTP 200 OK com o resumo.
    /// </summary>
    [Fact]
    public async Task SyncCampaigns_WhenSuccessful_ShouldReturnOk()
    {
        // Arrange
        var request = new SyncCampaignHierarchyApiRequest(Guid.NewGuid());
        var summary = new SyncCampaignHierarchySummaryDto(1, 3, 4, 8, DateTime.UtcNow, new[] { "MetaAds" });

        _sender.Send(Arg.Any<SyncCampaignHierarchyCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<SyncCampaignHierarchySummaryDto>.Success(summary)));

        // Act
        var result = await _controller.SyncCampaigns(request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var val = okResult.Value.Should().BeOfType<Result<SyncCampaignHierarchySummaryDto>>().Subject;
        val.IsSuccess.Should().BeTrue();
        val.Value.TotalCampaignsSynced.Should().Be(3);
    }

    /// <summary>
    /// Valida que erro NotFound no comando resulta em HTTP 404.
    /// </summary>
    [Fact]
    public async Task SyncCampaigns_WhenAccountNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var request = new SyncCampaignHierarchyApiRequest(Guid.NewGuid(), Guid.NewGuid());
        _sender.Send(Arg.Any<SyncCampaignHierarchyCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<SyncCampaignHierarchySummaryDto>.Failure(
                Error.NotFound("SyncCampaignHierarchy.AccountNotFound", "Conta não encontrada"))));

        // Act
        var result = await _controller.SyncCampaigns(request);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    /// <summary>
    /// Valida que a consulta de campanhas retorna HTTP 200 OK com a lista.
    /// </summary>
    [Fact]
    public async Task GetCampaigns_WhenSuccessful_ShouldReturnOkWithList()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        _sender.Send(Arg.Any<GetCampaignHierarchyQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<CampaignHierarchyDto>>.Success(
                Array.Empty<CampaignHierarchyDto>())));

        // Act
        var result = await _controller.GetCampaigns(workspaceId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var val = okResult.Value.Should().BeOfType<Result<IReadOnlyList<CampaignHierarchyDto>>>().Subject;
        val.IsSuccess.Should().BeTrue();
    }
}
