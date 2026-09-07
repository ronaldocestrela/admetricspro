using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using FluentAssertions;
using NSubstitute;
using Tenants.Application.Persistence;
using Tenants.Application.Squads.Commands.ToggleSquadStatus;
using Tenants.Application.Squads.Repositories;
using Xunit;

namespace UnitTests.Backend.Tenants;

/// <summary>
/// Testes unitários para <see cref="ToggleSquadStatusCommandHandler"/>.
/// </summary>
public sealed class ToggleSquadStatusCommandHandlerTests
{
    private readonly ISquadRepository _squadRepository = Substitute.For<ISquadRepository>();
    private readonly ITenantUnitOfWork _unitOfWork = Substitute.For<ITenantUnitOfWork>();
    private readonly ITenantContextAccessor _tenantContextAccessor = Substitute.For<ITenantContextAccessor>();
    private readonly ToggleSquadStatusCommandHandler _handler;

    /// <summary>
    /// Construtor dos testes.
    /// </summary>
    public ToggleSquadStatusCommandHandlerTests()
    {
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.TenantId.Returns(Guid.NewGuid());
        tenantContext.IsResolved.Returns(true);
        _tenantContextAccessor.TenantContext.Returns(tenantContext);

        _handler = new ToggleSquadStatusCommandHandler(
            _squadRepository,
            _unitOfWork,
            _tenantContextAccessor);
    }

    /// <summary>
    /// Valida que a alteração de status do squad persiste o novo estado com sucesso.
    /// </summary>
    [Fact]
    public async Task Handle_QuandoSquadExiste_DeveAlternarStatus()
    {
        // Arrange
        var squadId = Guid.NewGuid();
        var squad = Squad.Create(squadId, "Squad Alpha", null).Value;
        _squadRepository.GetByIdAsync(squadId, Arg.Any<CancellationToken>()).Returns(squad);

        var command = new ToggleSquadStatusCommand(squadId, false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        squad.IsActive.Should().BeFalse();

        _squadRepository.Received(1).Update(squad);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Valida que squad inexistente retorna NotFound.
    /// </summary>
    [Fact]
    public async Task Handle_QuandoSquadNaoExiste_DeveRetornarNotFound()
    {
        // Arrange
        var squadId = Guid.NewGuid();
        _squadRepository.GetByIdAsync(squadId, Arg.Any<CancellationToken>()).Returns((Squad?)null);

        var command = new ToggleSquadStatusCommand(squadId, false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Squad.NotFound");
    }
}
