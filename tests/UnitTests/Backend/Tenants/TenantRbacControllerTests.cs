using BuildingBlocks.Application.Security;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Tenants.Application.Audit.DTOs;
using Tenants.Application.Audit.Queries.GetTenantAuditLogs;
using Tenants.Application.Rbac.DTOs;
using Tenants.Application.Rbac.Queries.GetTenantRbacMatrix;
using Tenants.Application.Rbac.Queries.GetTenantUserPermissions;
using Tenants.Application.Users.Commands.ChangeTenantUserRole;
using WebApi.Controllers.v1;
using WebApi.Models;
using Xunit;

namespace UnitTests.Backend.Tenants;

/// <summary>
/// Suíte de testes unitários para o controlador <see cref="TenantRbacController"/>.
/// </summary>
public sealed class TenantRbacControllerTests
{
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly ICurrentUserContext _currentUserContext = Substitute.For<ICurrentUserContext>();
    private readonly TenantRbacController _controller;

    /// <summary>
    /// Inicializa a suíte com o controlador e dependências mockadas.
    /// </summary>
    public TenantRbacControllerTests()
    {
        _controller = new TenantRbacController(_sender, _currentUserContext)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    /// <summary>
    /// Valida que GetRbacMatrix retorna 200 OK com o mapeamento completo.
    /// </summary>
    [Fact]
    public async Task GetRbacMatrix_DeveRetornarOkComMatriz()
    {
        // Arrange
        var matrixDto = new TenantRbacMatrixDto(
            new Dictionary<string, IReadOnlyList<string>>
            {
                ["Owner"] = ["ManageBilling", "ManageSettings"]
            },
            ["ManageBilling", "ManageSettings"]);

        _sender.Send(Arg.Any<GetTenantRbacMatrixQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<TenantRbacMatrixDto>.Success(matrixDto));

        // Act
        var actionResult = await _controller.GetRbacMatrix();

        // Assert
        var okResult = actionResult.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(StatusCodes.Status200OK);

        var result = okResult.Value as Result<TenantRbacMatrixDto>;
        result.Should().NotBeNull();
        result!.Value.Roles.Should().ContainKey("Owner");
    }

    /// <summary>
    /// Valida que GetUserPermissions retorna 200 OK com permissões do colaborador.
    /// </summary>
    [Fact]
    public async Task GetUserPermissions_QuandoColaboradorExiste_DeveRetornarOk()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var permissionsDto = new TenantUserPermissionsDto(userId, "MediaManager", ["ViewCampaigns", "EditCampaigns"]);

        _sender.Send(Arg.Is<GetTenantUserPermissionsQuery>(q => q.UserId == userId), Arg.Any<CancellationToken>())
            .Returns(Result<TenantUserPermissionsDto>.Success(permissionsDto));

        // Act
        var actionResult = await _controller.GetUserPermissions(userId);

        // Assert
        var okResult = actionResult.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(StatusCodes.Status200OK);

        var result = okResult.Value as Result<TenantUserPermissionsDto>;
        result.Should().NotBeNull();
        result!.Value.Role.Should().Be("MediaManager");
    }

    /// <summary>
    /// Valida que GetUserPermissions retorna 404 NotFound quando colaborador não existe.
    /// </summary>
    [Fact]
    public async Task GetUserPermissions_QuandoColaboradorNaoExiste_DeveRetornarNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _sender.Send(Arg.Is<GetTenantUserPermissionsQuery>(q => q.UserId == userId), Arg.Any<CancellationToken>())
            .Returns(Result<TenantUserPermissionsDto>.Failure(Error.NotFound("TenantUser.NotFound", "Não localizado.")));

        // Act
        var actionResult = await _controller.GetUserPermissions(userId);

        // Assert
        var notFoundResult = actionResult.Result as NotFoundObjectResult;
        notFoundResult.Should().NotBeNull();
        notFoundResult!.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    /// <summary>
    /// Valida que ChangeUserRole executa com sucesso e retorna 200 OK.
    /// </summary>
    [Fact]
    public async Task ChangeUserRole_ComDadosValidos_DeveRetornarOk()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var operatorId = Guid.NewGuid();
        _currentUserContext.UserId.Returns(operatorId);
        _currentUserContext.UserEmail.Returns("admin@agencia.com");

        _sender.Send(Arg.Any<ChangeTenantUserRoleCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var request = new ChangeTenantUserRoleApiRequest(TenantRole.SquadLeader);

        // Act
        var actionResult = await _controller.ChangeUserRole(userId, request);

        // Assert
        var okResult = actionResult.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    /// <summary>
    /// Valida que GetAuditLogs retorna 200 OK com logs paginados.
    /// </summary>
    [Fact]
    public async Task GetAuditLogs_DeveRetornarOkComRespostaPaginada()
    {
        // Arrange
        var responseDto = new TenantAuditLogsResponse([], 0, 1, 50);

        _sender.Send(Arg.Any<GetTenantAuditLogsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<TenantAuditLogsResponse>.Success(responseDto));

        // Act
        var actionResult = await _controller.GetAuditLogs();

        // Assert
        var okResult = actionResult.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(StatusCodes.Status200OK);
    }
}
