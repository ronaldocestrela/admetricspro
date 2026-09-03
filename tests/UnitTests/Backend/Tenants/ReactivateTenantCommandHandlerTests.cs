using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using Master.Application.Repositories;
using Master.Application.Tenants.Commands.ReactivateTenant;
using Master.Domain.Tenants;
using NSubstitute;

namespace UnitTests.Backend.Tenants;

/// <summary>
/// Unit tests for <see cref="ReactivateTenantCommandHandler"/>.
/// </summary>
public sealed class ReactivateTenantCommandHandlerTests
{
    private readonly ITenantRepository _tenantRepository = Substitute.For<ITenantRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    /// <summary>
    /// Verifies that handler returns a NotFound failure when the tenant does not exist.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenTenantDoesNotExist()
    {
        // Arrange
        var handler = new ReactivateTenantCommandHandler(_tenantRepository, _unitOfWork);
        var tenantId = TenantId.New();
        var command = new ReactivateTenantCommand(tenantId);

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
    /// Verifies that handler reactivates an existing suspended tenant and commits unit of work.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldReactivateTenantAndCommit_WhenTenantExists()
    {
        // Arrange
        var handler = new ReactivateTenantCommandHandler(_tenantRepository, _unitOfWork);
        var tenant = Tenant.Create("Alpha Corp", "11222333000181", "alpha").Value;
        tenant.Suspend("Pagamento pendente");

        _tenantRepository.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>())
            .Returns(tenant);

        var command = new ReactivateTenantCommand(tenant.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        tenant.Status.Should().Be(TenantStatus.Active);
        _tenantRepository.Received(1).Update(tenant);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}
