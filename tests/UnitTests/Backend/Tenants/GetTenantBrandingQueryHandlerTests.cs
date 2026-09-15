using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using FluentAssertions;
using NSubstitute;
using Tenants.Application.Branding.DTOs;
using Tenants.Application.Branding.Queries.GetTenantBranding;
using Tenants.Application.Branding.Repositories;

namespace UnitTests.Backend.Tenants;

/// <summary>
/// Unit tests for <see cref="GetTenantBrandingQueryHandler"/>.
/// </summary>
public sealed class GetTenantBrandingQueryHandlerTests
{
    private readonly ITenantBrandingRepository _brandingRepository = Substitute.For<ITenantBrandingRepository>();
    private readonly ITenantContextAccessor _tenantContextAccessor = Substitute.For<ITenantContextAccessor>();
    private readonly GetTenantBrandingQueryHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();

    /// <summary>
    /// Initializes a new instance of <see cref="GetTenantBrandingQueryHandlerTests"/>.
    /// </summary>
    public GetTenantBrandingQueryHandlerTests()
    {
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.IsResolved.Returns(true);
        tenantContext.TenantId.Returns(_tenantId);
        _tenantContextAccessor.TenantContext.Returns(tenantContext);

        _handler = new GetTenantBrandingQueryHandler(_brandingRepository, _tenantContextAccessor);
    }

    /// <summary>
    /// Verifies that when branding exists, it is returned.
    /// </summary>
    [Fact]
    public async Task Handle_WhenBrandingExists_ShouldReturnConfiguredBranding()
    {
        // Arrange
        var branding = TenantBranding.Create(
            Guid.NewGuid(),
            "#10B981",
            "#1E293B",
            "https://cdn.example.com/logo-light.png",
            "https://cdn.example.com/logo-dark.png",
            "https://cdn.example.com/fav.ico").Value;

        _brandingRepository.GetAsync(Arg.Any<CancellationToken>()).Returns(branding);

        // Act
        var result = await _handler.Handle(new GetTenantBrandingQuery(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PrimaryColor.Should().Be("#10B981");
        result.Value.SecondaryColor.Should().Be("#1E293B");
        result.Value.LightLogoUrl.Should().Be("https://cdn.example.com/logo-light.png");
        result.Value.DarkLogoUrl.Should().Be("https://cdn.example.com/logo-dark.png");
        result.Value.FaviconUrl.Should().Be("https://cdn.example.com/fav.ico");
    }

    /// <summary>
    /// Verifies that when branding does not exist, default values are returned.
    /// </summary>
    [Fact]
    public async Task Handle_WhenBrandingDoesNotExist_ShouldReturnDefaultBranding()
    {
        // Arrange
        _brandingRepository.GetAsync(Arg.Any<CancellationToken>()).Returns((TenantBranding?)null);

        // Act
        var result = await _handler.Handle(new GetTenantBrandingQuery(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PrimaryColor.Should().Be("#2563EB");
        result.Value.SecondaryColor.Should().Be("#0F172A");
        result.Value.LightLogoUrl.Should().BeNull();
    }
}
