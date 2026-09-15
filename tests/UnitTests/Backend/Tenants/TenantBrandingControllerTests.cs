using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Tenants.Application.Branding.Commands.UpdateTenantBranding;
using Tenants.Application.Branding.DTOs;
using Tenants.Application.Branding.Queries.GetTenantBranding;
using WebApi.Controllers.v1;
using WebApi.Models;

namespace UnitTests.Backend.Tenants;

/// <summary>
/// Unit tests for <see cref="TenantBrandingController"/>.
/// </summary>
public sealed class TenantBrandingControllerTests
{
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly TenantBrandingController _controller;

    /// <summary>
    /// Initializes a new instance of <see cref="TenantBrandingControllerTests"/>.
    /// </summary>
    public TenantBrandingControllerTests()
    {
        _controller = new TenantBrandingController(_sender);
    }

    /// <summary>
    /// Verifies that GetBranding returns Ok when successful.
    /// </summary>
    [Fact]
    public async Task GetBranding_WhenSuccessful_ShouldReturnOk()
    {
        // Arrange
        var dto = new TenantBrandingDetailsDto(
            "#10B981",
            "#1E293B",
            "https://cdn.example.com/logo.png",
            null,
            "https://cdn.example.com/fav.ico",
            DateTime.UtcNow);

        _sender.Send(Arg.Any<GetTenantBrandingQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<TenantBrandingDetailsDto>.Success(dto));

        // Act
        var response = await _controller.GetBranding(CancellationToken.None);

        // Assert
        var okResult = response.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        var result = okResult!.Value as Result<TenantBrandingDetailsDto>;
        result!.IsSuccess.Should().BeTrue();
        result.Value.PrimaryColor.Should().Be("#10B981");
    }

    /// <summary>
    /// Verifies that UpdateBranding returns Ok when command succeeds.
    /// </summary>
    [Fact]
    public async Task UpdateBranding_WithValidData_ShouldReturnOk()
    {
        // Arrange
        var request = new UpdateTenantBrandingApiRequest(
            "#2563EB",
            "#0F172A",
            "https://cdn.example.com/logo.svg",
            "https://cdn.example.com/dark.svg",
            "https://cdn.example.com/favicon.png");

        var dto = new TenantBrandingDetailsDto(
            request.PrimaryColor,
            request.SecondaryColor,
            request.LightLogoUrl,
            request.DarkLogoUrl,
            request.FaviconUrl,
            DateTime.UtcNow);

        _sender.Send(Arg.Any<UpdateTenantBrandingCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<TenantBrandingDetailsDto>.Success(dto));

        // Act
        var response = await _controller.UpdateBranding(request, CancellationToken.None);

        // Assert
        var okResult = response.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        var result = okResult!.Value as Result<TenantBrandingDetailsDto>;
        result!.IsSuccess.Should().BeTrue();
        result.Value.PrimaryColor.Should().Be("#2563EB");
    }

    /// <summary>
    /// Verifies that UpdateBranding with null body returns BadRequest.
    /// </summary>
    [Fact]
    public async Task UpdateBranding_WithNullRequest_ShouldReturnBadRequest()
    {
        // Act
        var response = await _controller.UpdateBranding(null!, CancellationToken.None);

        // Assert
        var badRequest = response.Result as BadRequestObjectResult;
        badRequest.Should().NotBeNull();
    }
}
