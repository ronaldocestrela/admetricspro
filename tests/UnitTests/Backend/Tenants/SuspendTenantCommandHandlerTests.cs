using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using Master.Application.Repositories;
using Master.Application.Tenants.Commands.SuspendTenant;
using Master.Domain.Tenants;
using NSubstitute;

namespace UnitTests.Backend.Tenants;

/// <summary>
/// Unit tests for <see cref="SuspendTenantCommandHandler"/>.
/// </summary>
public sealed class SuspendTenantCommandHandlerTests
{
    private readonly ITenantRepository _tenantRepository = Substitute.For<ITenantRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    /// <summary>
    /// Verifies that handler returns a NotFound failure when tenant is not found.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenTenantDoesNotExist()
    {
        // Arrange
        var handler = new SuspendTenantCommandHandler(_tenantRepository, _unitOfWork);
        var tenantId = TenantId.New();
        var command = new SuspendTenantCommand(tenantId, "Inadimplência recorrente");

        _tenantRepository.GetByIdAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns((Tenant?)null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Tenant.NotFound");
        _tenantRepository.DidNotReceive().Update(Arg.Any<Tenant>());
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that handler suspends the tenant and commits changes when input is valid.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldSuspendTenantAndCommit_WhenTenantExistsAndReasonIsValid()
    {
        // Arrange
        var handler = new SuspendTenantCommandHandler(_tenantRepository, _unitOfWork);
        var tenant = Tenant.Create("Alpha Corp", "11222333000181", "alpha").Value;
        var command = new SuspendTenantCommand(tenant.Id, "Inadimplência após régua de cobrança");

        _tenantRepository.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>())
            .Returns(tenant);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        tenant.Status.Should().Be(TenantStatus.Suspended);
        _tenantRepository.Received(1).Update(tenant);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that handler propagates domain validation failures when suspension reason is invalid.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenDomainValidationFails()
    {
        // Arrange
        var handler = new SuspendTenantCommandHandler(_tenantRepository, _unitOfWork);
        var tenant = Tenant.Create("Alpha Corp", "11222333000181", "alpha").Value;
        var command = new SuspendTenantCommand(tenant.Id, "   ");

        _tenantRepository.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>())
            .Returns(tenant);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Tenant.SuspensionReasonRequired");
        _tenantRepository.DidNotReceive().Update(Arg.Any<Tenant>());
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }
}
