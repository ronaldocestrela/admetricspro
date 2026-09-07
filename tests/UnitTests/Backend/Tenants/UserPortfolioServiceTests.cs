using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using FluentAssertions;
using NSubstitute;
using Tenants.Application.Squads.Repositories;
using Tenants.Application.Users.Repositories;
using Tenants.Application.Workspaces.Repositories;
using Tenants.Infrastructure.Squads;
using Xunit;

namespace UnitTests.Backend.Tenants;

/// <summary>
/// Testes unitários para <see cref="UserPortfolioService"/> cobrindo todas as regras de isolamento por carteira.
/// </summary>
public sealed class UserPortfolioServiceTests
{
    private readonly ITenantUserRepository _userRepository = Substitute.For<ITenantUserRepository>();
    private readonly ISquadRepository _squadRepository = Substitute.For<ISquadRepository>();
    private readonly IWorkspaceRepository _workspaceRepository = Substitute.For<IWorkspaceRepository>();
    private readonly UserPortfolioService _service;

    /// <summary>
    /// Inicializa a suíte com as dependências mockadas.
    /// </summary>
    public UserPortfolioServiceTests()
    {
        _service = new UserPortfolioService(
            _userRepository,
            _squadRepository,
            _workspaceRepository);
    }

    /// <summary>
    /// Valida que usuários com papel Owner possuem acesso a qualquer workspace do inquilino.
    /// </summary>
    [Fact]
    public async Task HasAccessToWorkspace_QuandoUsuarioOwner_DevePermitirAcessoIrrestrito()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var owner = TenantUser.Create(ownerId, "Dono da Agência", "owner@agencia.com", null, "hash", TenantRole.Owner).Value;
        var workspace = Workspace.Create(workspaceId, "Cliente VIP", "123.456.789-09", 10000m, "SaaS").Value;

        _userRepository.GetByIdAsync(ownerId, Arg.Any<CancellationToken>()).Returns(owner);
        _workspaceRepository.GetByIdAsync(workspaceId, Arg.Any<CancellationToken>()).Returns(workspace);

        // Act
        var result = await _service.HasAccessToWorkspaceAsync(ownerId, workspaceId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();

        // Não deve consultar squads para papéis globais
        await _squadRepository.DidNotReceive().GetSquadsByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Valida que usuários com papel Admin possuem acesso a qualquer workspace do inquilino.
    /// </summary>
    [Fact]
    public async Task HasAccessToWorkspace_QuandoUsuarioAdmin_DevePermitirAcessoIrrestrito()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var admin = TenantUser.Create(adminId, "Admin da Agência", "admin@agencia.com", null, "hash", TenantRole.Admin).Value;
        var workspace = Workspace.Create(workspaceId, "Cliente VIP", "123.456.789-09", 10000m, "SaaS").Value;

        _userRepository.GetByIdAsync(adminId, Arg.Any<CancellationToken>()).Returns(admin);
        _workspaceRepository.GetByIdAsync(workspaceId, Arg.Any<CancellationToken>()).Returns(workspace);

        // Act
        var result = await _service.HasAccessToWorkspaceAsync(adminId, workspaceId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    /// <summary>
    /// Valida que um Analista pertencente ao Squad A tem acesso aos clientes do Squad A,
    /// mas é bloqueado de acessar clientes pertencentes exclusivamente ao Squad B.
    /// </summary>
    [Fact]
    public async Task HasAccessToWorkspace_IsolamentoDeCarteira_AnalistaSquadADeveAcessarAENaoB()
    {
        // Arrange
        var analystId = Guid.NewGuid();
        var workspaceAId = Guid.NewGuid();
        var workspaceBId = Guid.NewGuid();

        var analyst = TenantUser.Create(analystId, "Analista Junior", "analista@agencia.com", null, "hash", TenantRole.Analyst).Value;
        var workspaceA = Workspace.Create(workspaceAId, "Cliente Squad A", "123.456.789-09", 5000m, "Moda").Value;
        var workspaceB = Workspace.Create(workspaceBId, "Cliente Squad B", "987.654.321-00", 8000m, "Tech").Value;

        var squadA = Squad.Create(Guid.NewGuid(), "Squad A", null).Value;
        squadA.AddMember(analystId);
        squadA.AssignWorkspace(workspaceAId);

        _userRepository.GetByIdAsync(analystId, Arg.Any<CancellationToken>()).Returns(analyst);
        _workspaceRepository.GetByIdAsync(workspaceAId, Arg.Any<CancellationToken>()).Returns(workspaceA);
        _workspaceRepository.GetByIdAsync(workspaceBId, Arg.Any<CancellationToken>()).Returns(workspaceB);
        _squadRepository.GetSquadsByUserIdAsync(analystId, Arg.Any<CancellationToken>()).Returns(new List<Squad> { squadA });

        // Act 1: Acesso ao cliente do próprio squad
        var accessResultA = await _service.HasAccessToWorkspaceAsync(analystId, workspaceAId);

        // Act 2: Acesso ao cliente de outro squad (Squad B)
        var accessResultB = await _service.HasAccessToWorkspaceAsync(analystId, workspaceBId);

        // Assert
        accessResultA.IsSuccess.Should().BeTrue();
        accessResultA.Value.Should().BeTrue("o analista pertence ao Squad A que gerencia este cliente");

        accessResultB.IsSuccess.Should().BeTrue();
        accessResultB.Value.Should().BeFalse("o analista NÃO pertence ao squad que gerencia o Cliente B");
    }

    /// <summary>
    /// Valida que analista não vinculado a nenhum squad possui carteira vazia (acesso zero).
    /// </summary>
    [Fact]
    public async Task GetAccessibleWorkspaceIds_QuandoAnalistaSemSquad_DeveRetornarCarteiraVazia()
    {
        // Arrange
        var analystId = Guid.NewGuid();
        var analyst = TenantUser.Create(analystId, "Analista Sem Squad", "analista2@agencia.com", null, "hash", TenantRole.Analyst).Value;

        _userRepository.GetByIdAsync(analystId, Arg.Any<CancellationToken>()).Returns(analyst);
        _squadRepository.GetSquadsByUserIdAsync(analystId, Arg.Any<CancellationToken>()).Returns(new List<Squad>());

        // Act
        var result = await _service.GetAccessibleWorkspaceIdsAsync(analystId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    /// <summary>
    /// Valida que analista vinculado a múltiplos squads tem acesso à união consolidada das carteiras.
    /// </summary>
    [Fact]
    public async Task GetAccessibleWorkspaceIds_QuandoAnalistaEmMultiplosSquads_DeveRetornarUniaoDeWorkspaces()
    {
        // Arrange
        var analystId = Guid.NewGuid();
        var ws1 = Guid.NewGuid();
        var ws2 = Guid.NewGuid();

        var analyst = TenantUser.Create(analystId, "Gestor Sênior", "gestor@agencia.com", null, "hash", TenantRole.MediaManager).Value;

        var squad1 = Squad.Create(Guid.NewGuid(), "Squad Alpha", null).Value;
        squad1.AddMember(analystId);
        squad1.AssignWorkspace(ws1);

        var squad2 = Squad.Create(Guid.NewGuid(), "Squad Beta", null).Value;
        squad2.AddMember(analystId);
        squad2.AssignWorkspace(ws2);

        _userRepository.GetByIdAsync(analystId, Arg.Any<CancellationToken>()).Returns(analyst);
        _squadRepository.GetSquadsByUserIdAsync(analystId, Arg.Any<CancellationToken>()).Returns(new List<Squad> { squad1, squad2 });

        // Act
        var result = await _service.GetAccessibleWorkspaceIdsAsync(analystId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(new[] { ws1, ws2 });
    }

    /// <summary>
    /// Valida que colaborador inativo não tem permissão para acessar carteira.
    /// </summary>
    [Fact]
    public async Task HasAccessToWorkspace_QuandoUsuarioInativo_DeveRetornarErroDeValidacao()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = TenantUser.Create(userId, "Inativo", "inativo@agencia.com", null, "hash", TenantRole.Analyst).Value;
        user.Deactivate();

        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);

        // Act
        var result = await _service.HasAccessToWorkspaceAsync(userId, Guid.NewGuid());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.Inactive");
    }
}
