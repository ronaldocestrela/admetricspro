using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using FluentAssertions;
using NSubstitute;
using Tenants.Application.Persistence;
using Tenants.Application.Squads.Commands.AssignSquadWorkspace;
using Tenants.Application.Squads.Repositories;
using Tenants.Application.Workspaces.Repositories;
using Xunit;

namespace UnitTests.Backend.Tenants;

/// <summary>
/// Testes unitários para <see cref="AssignSquadWorkspaceCommandHandler"/>.
/// </summary>
public sealed class AssignSquadWorkspaceCommandHandlerTests
{
    private readonly ISquadRepository _squadRepository = Substitute.For<ISquadRepository>();
    private readonly IWorkspaceRepository _workspaceRepository = Substitute.For<IWorkspaceRepository>();
    private readonly ITenantUnitOfWork _unitOfWork = Substitute.For<ITenantUnitOfWork>();
    private readonly ITenantContextAccessor _tenantContextAccessor = Substitute.For<ITenantContextAccessor>();
    private readonly AssignSquadWorkspaceCommandHandler _handler;

    /// <summary>
    /// Construtor dos testes.
    /// </summary>
    public AssignSquadWorkspaceCommandHandlerTests()
    {
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.TenantId.Returns(Guid.NewGuid());
        tenantContext.IsResolved.Returns(true);
        _tenantContextAccessor.TenantContext.Returns(tenantContext);

        _handler = new AssignSquadWorkspaceCommandHandler(
            _squadRepository,
            _workspaceRepository,
            _unitOfWork,
            _tenantContextAccessor);
    }

    /// <summary>
    /// Valida que workspace ativo é alocado à carteira do squad com sucesso.
    /// </summary>
    [Fact]
    public async Task Handle_QuandoWorkspaceESquadValidos_DeveAlocarWorkspace()
    {
        // Arrange
        var squadId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var squad = Squad.Create(squadId, "Squad Performance", null).Value;
        var workspace = Workspace.Create(workspaceId, "Cliente Exemplo", "123.456.789-09", 5000m, "Moda").Value;

        _squadRepository.GetByIdAsync(squadId, Arg.Any<CancellationToken>()).Returns(squad);
        _workspaceRepository.GetByIdAsync(workspaceId, Arg.Any<CancellationToken>()).Returns(workspace);

        var command = new AssignSquadWorkspaceCommand(squadId, workspaceId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        squad.Workspaces.Should().ContainSingle(w => w.WorkspaceId == workspaceId);

        _squadRepository.Received(1).Update(squad);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Valida que workspace inexistente retorna NotFound.
    /// </summary>
    [Fact]
    public async Task Handle_QuandoWorkspaceNaoExiste_DeveRetornarNotFound()
    {
        // Arrange
        var squadId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var squad = Squad.Create(squadId, "Squad Performance", null).Value;

        _squadRepository.GetByIdAsync(squadId, Arg.Any<CancellationToken>()).Returns(squad);
        _workspaceRepository.GetByIdAsync(workspaceId, Arg.Any<CancellationToken>()).Returns((Workspace?)null);

        var command = new AssignSquadWorkspaceCommand(squadId, workspaceId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Workspace.NotFound");
    }

    /// <summary>
    /// Valida que workspace pausado/inativo não pode ser alocado.
    /// </summary>
    [Fact]
    public async Task Handle_QuandoWorkspaceInativo_DeveRetornarErroDeValidacao()
    {
        // Arrange
        var squadId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var squad = Squad.Create(squadId, "Squad Performance", null).Value;
        var workspace = Workspace.Create(workspaceId, "Cliente Pausado", "123.456.789-09", 5000m, "Moda").Value;
        workspace.Deactivate();

        _squadRepository.GetByIdAsync(squadId, Arg.Any<CancellationToken>()).Returns(squad);
        _workspaceRepository.GetByIdAsync(workspaceId, Arg.Any<CancellationToken>()).Returns(workspace);

        var command = new AssignSquadWorkspaceCommand(squadId, workspaceId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Workspace.Inactive");
    }
}
