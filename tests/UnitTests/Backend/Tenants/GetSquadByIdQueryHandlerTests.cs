using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using FluentAssertions;
using NSubstitute;
using Tenants.Application.Squads.Queries.GetSquadById;
using Tenants.Application.Squads.Repositories;
using Tenants.Application.Users.Repositories;
using Tenants.Application.Workspaces.Repositories;
using Xunit;

namespace UnitTests.Backend.Tenants;

/// <summary>
/// Testes unitários para <see cref="GetSquadByIdQueryHandler"/>.
/// </summary>
public sealed class GetSquadByIdQueryHandlerTests
{
    private readonly ISquadRepository _squadRepository = Substitute.For<ISquadRepository>();
    private readonly ITenantUserRepository _userRepository = Substitute.For<ITenantUserRepository>();
    private readonly IWorkspaceRepository _workspaceRepository = Substitute.For<IWorkspaceRepository>();
    private readonly GetSquadByIdQueryHandler _handler;

    /// <summary>
    /// Construtor dos testes.
    /// </summary>
    public GetSquadByIdQueryHandlerTests()
    {
        _handler = new GetSquadByIdQueryHandler(
            _squadRepository,
            _userRepository,
            _workspaceRepository);
    }

    /// <summary>
    /// Valida que a consulta carrega os detalhes completos do squad com membros e clientes.
    /// </summary>
    [Fact]
    public async Task Handle_QuandoSquadExiste_DeveRetornarDetalhesCompletos()
    {
        // Arrange
        var squadId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();

        var squad = Squad.Create(squadId, "Squad Alpha", "Desc").Value;
        squad.AddMember(userId);
        squad.AssignWorkspace(workspaceId);

        var user = TenantUser.Create(userId, "Carlos Gestor", "carlos@agencia.com", null, "hash", TenantRole.SquadLeader).Value;
        var workspace = Workspace.Create(workspaceId, "Loja Moda", "123.456.789-09", 15000m, "Varejo").Value;

        _squadRepository.GetByIdAsync(squadId, Arg.Any<CancellationToken>()).Returns(squad);
        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);
        _workspaceRepository.GetByIdAsync(workspaceId, Arg.Any<CancellationToken>()).Returns(workspace);

        var query = new GetSquadByIdQuery(squadId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Id.Should().Be(squadId);
        result.Value.Members.Should().ContainSingle(m => m.UserId == userId && m.FullName == "Carlos Gestor");
        result.Value.Workspaces.Should().ContainSingle(w => w.WorkspaceId == workspaceId && w.Name == "Loja Moda");
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

        var query = new GetSquadByIdQuery(squadId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Squad.NotFound");
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }
}
