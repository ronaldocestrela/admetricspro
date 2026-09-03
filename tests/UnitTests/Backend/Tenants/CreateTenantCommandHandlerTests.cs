using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using Master.Application.Services;
using Master.Application.Tenants.Commands.CreateTenant;
using Master.Domain.Tenants;
using NSubstitute;

namespace UnitTests.Backend.Tenants;

/// <summary>
/// Unit tests for <see cref="CreateTenantCommandHandler"/>.
/// </summary>
public sealed class CreateTenantCommandHandlerTests
{
    private readonly ITenantProvisioningService _provisioningService = Substitute.For<ITenantProvisioningService>();

    /// <summary>
    /// Verifies that handler successfully delegates to provisioning service and returns the tenant identifier.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenProvisioningSucceeds()
    {
        // Arrange
        var handler = new CreateTenantCommandHandler(_provisioningService);
        var command = new CreateTenantCommand("Alpha Corp", "11222333000181", "alpha", SubscriptionTier.Pro);
        var expectedTenantId = TenantId.New();

        _provisioningService.ProvisionTenantDatabaseAsync(
            Arg.Is<ProvisionTenantCommand>(c =>
                c.CompanyName == command.CompanyName &&
                c.Cnpj == command.Cnpj &&
                c.Subdomain == command.Subdomain &&
                c.Tier == command.Tier),
            Arg.Any<CancellationToken>())
            .Returns(Result<TenantId>.Success(expectedTenantId));

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expectedTenantId);
        await _provisioningService.Received(1).ProvisionTenantDatabaseAsync(
            Arg.Any<ProvisionTenantCommand>(),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that handler propagates domain or provisioning errors returned by the service.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenProvisioningFails()
    {
        // Arrange
        var handler = new CreateTenantCommandHandler(_provisioningService);
        var command = new CreateTenantCommand("Alpha Corp", "11222333000181", "alpha");
        var domainError = Error.Conflict("Tenant.SubdomainAlreadyExists", "Subdomain is already registered.");

        _provisioningService.ProvisionTenantDatabaseAsync(
            Arg.Any<ProvisionTenantCommand>(),
            Arg.Any<CancellationToken>())
            .Returns(Result<TenantId>.Failure(domainError));

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Tenant.SubdomainAlreadyExists");
    }
}
