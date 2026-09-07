using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using FluentAssertions;
using NSubstitute;
using Tenants.Application.Persistence;
using Tenants.Application.Squads.Commands.UpdateSquad;
using Tenants.Application.Squads.Repositories;
using Xunit;

namespace UnitTests.Backend.Tenants;

/// <summary>
/// Testes unitários para o manipulador <see cref="UpdateSquadCommandHandler"/>.
/// </summary>
public sealed class UpdateSquadCommandHandlerTests
{
    private readonly ISquadRepository _squadRepository = Substitute.For<ISquadRepository>();
    private readonly ITenantUnitOfWork _unitOfWork = Substitute.For<ITenantUnitOfWork>();
    private readonly ITenantContextAccessor _tenantContextAccessor = Substitute.For<ITenantContextAccessor>();
    private readonly UpdateSquadCommandHandler _handler;

    /// <summary>
    /// Inicializa a suíte com o contexto de inquilino ativo pré-configurado.
    /// </summary>
    public UpdateSquadCommandHandlerTests()
    {
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.TenantId.Returns(Guid.NewGuid());
        tenantContext.IsResolved.Returns(true);
        _tenantContextAccessor.TenantContext.Returns(tenantContext);

        _handler = new UpdateSquadCommandHandler(
            _squadRepository,
            _unitOfWork,
            _tenantContextAccessor);
    }

    /// <summary>
    /// Valida que a atualização de um squad existente atualiza os dados e confirma a transação.
    /// </summary>
    [Fact]
    public async Task Handle_QuandoSquadExisteENomeValido_DeveAtualizarComSucesso()
    {
        // Arrange
        var squadId = Guid.NewGuid();
        var squad = Squad.Create(squadId, "Nome Antigo", "Desc antiga").Value;

        _squadRepository.GetByIdAsync(squadId, Arg.Any<CancellationToken>()).Returns(squad);
        _squadRepository.ExistsByNameAsync("Nome Novo", squadId, Arg.Any<CancellationToken>()).Returns(false);

        var command = new UpdateSquadCommand(squadId, "Nome Novo", "Nova desc");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        squad.Name.Should().Be("Nome Novo");
        squad.Description.Should().Be("Nova desc");

        _squadRepository.Received(1).Update(squad);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Valida que tentativa de atualizar um squad inexistente retorna NotFound.
    /// </summary>
    [Fact]
    public async Task Handle_QuandoSquadNaoExiste_DeveRetornarNotFound()
    {
        // Arrange
        var squadId = Guid.NewGuid();
        _squadRepository.GetByIdAsync(squadId, Arg.Any<CancellationToken>()).Returns((Squad?)null);

        var command = new UpdateSquadCommand(squadId, "Nome Qualquer", null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Squad.NotFound");
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    /// <summary>
    /// Valida que alteração para um nome já em uso por outro squad retorna Conflict.
    /// </summary>
    [Fact]
    public async Task Handle_QuandoNomeJaExisteEmOutroSquad_DeveRetornarConflict()
    {
        // Arrange
        var squadId = Guid.NewGuid();
        var squad = Squad.Create(squadId, "Squad Alpha", null).Value;

        _squadRepository.GetByIdAsync(squadId, Arg.Any<CancellationToken>()).Returns(squad);
        _squadRepository.ExistsByNameAsync("Squad Beta", squadId, Arg.Any<CancellationToken>()).Returns(true);

        var command = new UpdateSquadCommand(squadId, "Squad Beta", null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Squad.NameAlreadyExists");
        result.Error.Type.Should().Be(ErrorType.Conflict);
    }
}
