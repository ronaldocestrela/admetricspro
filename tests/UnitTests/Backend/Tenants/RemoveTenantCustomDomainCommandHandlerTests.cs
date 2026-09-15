using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using Master.Application.Repositories;
using Master.Application.Tenants.Commands.ConfigureTenantCustomDomain;
using Master.Domain.Tenants;
using NSubstitute;
using Xunit;

namespace UnitTests.Backend.Tenants;

/// <summary>
/// Unit tests for <see cref="RemoveTenantCustomDomainCommandHandler"/>.
/// </summary>
public sealed class RemoveTenantCustomDomainCommandHandlerTests
{
    private readonly ITenantRepository _tenantRepository = Substitute.For<ITenantRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly RemoveTenantCustomDomainCommandHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();

    /// <summary>
    /// Initializes a new instance of <see cref="RemoveTenantCustomDomainCommandHandlerTests"/>.
    /// </summary>
    public RemoveTenantCustomDomainCommandHandlerTests()
    {
        _handler = new RemoveTenantCustomDomainCommandHandler(
            _tenantRepository,
            _unitOfWork);
    }

    /// <summary>
    /// Verifies that removing custom domain from an existing tenant succeeds and commits.
    /// </summary>
    [Fact]
    public async Task Handle_WhenTenantExists_ShouldClearDomainAndCommit()
    {
        // Arrange
        var tenant = Tenant.Create(
            companyName: "Empresa Teste",
            cnpj: "12345678000195",
            subdomain: "empresa-teste",
            tier: SubscriptionTier.Enterprise,
            customDomain: "relatorios.empresa.com.br").Value;

        _tenantRepository.GetByIdAsync(Arg.Any<TenantId>(), Arg.Any<CancellationToken>())
            .Returns(tenant);

        var command = new RemoveTenantCustomDomainCommand(_tenantId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        tenant.CustomDomain.Should().BeNull();
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that attempting to remove custom domain from a non-existent tenant returns NotFound error.
    /// </summary>
    [Fact]
    public async Task Handle_WhenTenantDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        _tenantRepository.GetByIdAsync(Arg.Any<TenantId>(), Arg.Any<CancellationToken>())
            .Returns((Tenant?)null);

        var command = new RemoveTenantCustomDomainCommand(_tenantId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Tenant.NotFound");
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }
}
