using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using FluentAssertions;
using NSubstitute;
using Tenants.Application.Persistence;
using Tenants.Application.Squads.Commands.AddSquadMember;
using Tenants.Application.Squads.Repositories;
using Tenants.Application.Users.Repositories;
using Xunit;

namespace UnitTests.Backend.Tenants;

/// <summary>
/// Testes unitários para <see cref="AddSquadMemberCommandHandler"/>.
/// </summary>
public sealed class AddSquadMemberCommandHandlerTests
{
    private readonly ISquadRepository _squadRepository = Substitute.For<ISquadRepository>();
    private readonly ITenantUserRepository _userRepository = Substitute.For<ITenantUserRepository>();
    private readonly ITenantUnitOfWork _unitOfWork = Substitute.For<ITenantUnitOfWork>();
    private readonly ITenantContextAccessor _tenantContextAccessor = Substitute.For<ITenantContextAccessor>();
    private readonly AddSquadMemberCommandHandler _handler;

    /// <summary>
    /// Construtor dos testes.
    /// </summary>
    public AddSquadMemberCommandHandlerTests()
    {
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.TenantId.Returns(Guid.NewGuid());
        tenantContext.IsResolved.Returns(true);
        _tenantContextAccessor.TenantContext.Returns(tenantContext);

        _handler = new AddSquadMemberCommandHandler(
            _squadRepository,
            _userRepository,
            _unitOfWork,
            _tenantContextAccessor);
    }

    /// <summary>
    /// Valida que usuário ativo é adicionado com sucesso ao squad.
    /// </summary>
    [Fact]
    public async Task Handle_QuandoUsuarioESquadValidos_DeveAdicionarMembro()
    {
        // Arrange
        var squadId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var squad = Squad.Create(squadId, "Squad Growth", null).Value;
        var user = TenantUser.Create(userId, "Maria Analista", "maria@agencia.com", null, "hash", TenantRole.Analyst).Value;

        _squadRepository.GetByIdAsync(squadId, Arg.Any<CancellationToken>()).Returns(squad);
        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);

        var command = new AddSquadMemberCommand(squadId, userId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        squad.Members.Should().ContainSingle(m => m.UserId == userId);

        _squadRepository.Received(1).Update(squad);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Valida que vincular colaborador inexistente retorna NotFound.
    /// </summary>
    [Fact]
    public async Task Handle_QuandoUsuarioNaoExiste_DeveRetornarNotFound()
    {
        // Arrange
        var squadId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var squad = Squad.Create(squadId, "Squad Growth", null).Value;

        _squadRepository.GetByIdAsync(squadId, Arg.Any<CancellationToken>()).Returns(squad);
        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns((TenantUser?)null);

        var command = new AddSquadMemberCommand(squadId, userId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.NotFound");
    }

    /// <summary>
    /// Valida que vincular colaborador inativo retorna erro de validação.
    /// </summary>
    [Fact]
    public async Task Handle_QuandoUsuarioInativo_DeveRetornarErroDeValidacao()
    {
        // Arrange
        var squadId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var squad = Squad.Create(squadId, "Squad Growth", null).Value;
        var user = TenantUser.Create(userId, "Pedro Inativo", "pedro@agencia.com", null, "hash", TenantRole.Analyst).Value;
        user.Deactivate();

        _squadRepository.GetByIdAsync(squadId, Arg.Any<CancellationToken>()).Returns(squad);
        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);

        var command = new AddSquadMemberCommand(squadId, userId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.Inactive");
    }

    /// <summary>
    /// Valida que membro duplicado no mesmo squad retorna erro de conflito.
    /// </summary>
    [Fact]
    public async Task Handle_QuandoMembroJaEstaNoSquad_DeveRetornarConflict()
    {
        // Arrange
        var squadId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var squad = Squad.Create(squadId, "Squad Growth", null).Value;
        squad.AddMember(userId);

        var user = TenantUser.Create(userId, "Maria Analista", "maria@agencia.com", null, "hash", TenantRole.Analyst).Value;

        _squadRepository.GetByIdAsync(squadId, Arg.Any<CancellationToken>()).Returns(squad);
        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);

        var command = new AddSquadMemberCommand(squadId, userId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Squad.MemberAlreadyExists");
        result.Error.Type.Should().Be(ErrorType.Conflict);
    }
}
