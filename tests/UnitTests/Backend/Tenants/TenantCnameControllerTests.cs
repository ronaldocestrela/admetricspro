using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using Master.Application.Tenants.Commands.ConfigureTenantCustomDomain;
using Master.Application.Tenants.Queries.GetTenantCustomDomain;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using WebApi.Controllers.v1;
using WebApi.Models;
using Xunit;

namespace UnitTests.Backend.Tenants;

/// <summary>
/// Unit tests for <see cref="TenantCnameController"/>.
/// </summary>
public sealed class TenantCnameControllerTests
{
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly ITenantContextAccessor _tenantContextAccessor = Substitute.For<ITenantContextAccessor>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly TenantCnameController _controller;
    private readonly Guid _tenantId = Guid.NewGuid();

    /// <summary>
    /// Initializes a new instance of <see cref="TenantCnameControllerTests"/>.
    /// </summary>
    public TenantCnameControllerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _tenantContextAccessor.TenantContext.Returns(_tenantContext);
        _controller = new TenantCnameController(_sender, _tenantContextAccessor);
    }

    /// <summary>
    /// Verifies that GetCnameDetails returns Ok when query succeeds.
    /// </summary>
    [Fact]
    public async Task GetCnameDetails_WhenSuccessful_ShouldReturnOk()
    {
        // Arrange
        var dto = new TenantCustomDomainDto(
            _tenantId,
            "relatorios.agencia.com.br",
            "cname.admetricspro.com",
            true,
            true);

        _sender.Send(Arg.Any<GetTenantCustomDomainQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<TenantCustomDomainDto>.Success(dto));

        // Act
        var response = await _controller.GetCnameDetails(CancellationToken.None);

        // Assert
        var okResult = response.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        var result = okResult!.Value as Result<TenantCustomDomainDto>;
        result!.IsSuccess.Should().BeTrue();
        result.Value.CustomDomain.Should().Be("relatorios.agencia.com.br");
    }

    /// <summary>
    /// Verifies that GetCnameDetails returns Unauthorized when tenant is not resolved.
    /// </summary>
    [Fact]
    public async Task GetCnameDetails_WhenTenantUnresolved_ShouldReturnUnauthorized()
    {
        // Arrange
        _tenantContext.TenantId.Returns(Guid.Empty);

        // Act
        var response = await _controller.GetCnameDetails(CancellationToken.None);

        // Assert
        var unauthorized = response.Result as UnauthorizedObjectResult;
        unauthorized.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that ConfigureCname returns Ok when command succeeds.
    /// </summary>
    [Fact]
    public async Task ConfigureCname_WithValidData_ShouldReturnOk()
    {
        // Arrange
        var request = new ConfigureTenantCustomDomainApiRequest("relatorios.agencia.com.br");
        _sender.Send(Arg.Any<ConfigureTenantCustomDomainCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        var response = await _controller.ConfigureCname(request, CancellationToken.None);

        // Assert
        var okResult = response.Result as OkObjectResult;
        okResult.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that ConfigureCname with null body returns BadRequest.
    /// </summary>
    [Fact]
    public async Task ConfigureCname_WithNullRequest_ShouldReturnBadRequest()
    {
        // Act
        var response = await _controller.ConfigureCname(null!, CancellationToken.None);

        // Assert
        var badRequest = response.Result as BadRequestObjectResult;
        badRequest.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that ConfigureCname returns Conflict when domain is already in use.
    /// </summary>
    [Fact]
    public async Task ConfigureCname_WhenConflict_ShouldReturnConflict()
    {
        // Arrange
        var request = new ConfigureTenantCustomDomainApiRequest("relatorios.agencia.com.br");
        _sender.Send(Arg.Any<ConfigureTenantCustomDomainCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(Error.Conflict("Tenant.CustomDomainConflict", "Domínio em uso.")));

        // Act
        var response = await _controller.ConfigureCname(request, CancellationToken.None);

        // Assert
        var conflict = response.Result as ConflictObjectResult;
        conflict.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that ConfigureCname returns Forbidden when plan lacks feature.
    /// </summary>
    [Fact]
    public async Task ConfigureCname_WhenPlanForbidden_ShouldReturnForbidden()
    {
        // Arrange
        var request = new ConfigureTenantCustomDomainApiRequest("relatorios.agencia.com.br");
        _sender.Send(Arg.Any<ConfigureTenantCustomDomainCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(Error.Forbidden("Tenant.PlanLacksCustomCname", "Plano sem CNAME.")));

        // Act
        var response = await _controller.ConfigureCname(request, CancellationToken.None);

        // Assert
        var objectResult = response.Result as ObjectResult;
        objectResult.Should().NotBeNull();
        objectResult!.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    /// <summary>
    /// Verifies that RemoveCname returns Ok when removal succeeds.
    /// </summary>
    [Fact]
    public async Task RemoveCname_WhenSuccessful_ShouldReturnOk()
    {
        // Arrange
        _sender.Send(Arg.Any<RemoveTenantCustomDomainCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        var response = await _controller.RemoveCname(CancellationToken.None);

        // Assert
        var okResult = response.Result as OkObjectResult;
        okResult.Should().NotBeNull();
    }
}
