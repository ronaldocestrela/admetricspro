using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using FluentAssertions;
using Xunit;

namespace UnitTests.Backend.Tenants;

/// <summary>
/// Testes unitários para o agregado de domínio <see cref="Squad"/> e suas entidades filhas <see cref="SquadMember"/> e <see cref="SquadWorkspace"/>.
/// </summary>
public sealed class SquadTests
{
    /// <summary>
    /// Valida que a criação com dados válidos inicializa o squad no estado ativo e com coleções vazias.
    /// </summary>
    [Fact]
    public void Create_ComDadosValidos_DeveCriarSquadComSucesso()
    {
        // Arrange
        var id = Guid.NewGuid();
        const string name = "Squad E-commerce Performance";
        const string description = "Gestão de tráfego para grandes contas de e-commerce";

        // Act
        var result = Squad.Create(id, name, description);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Id.Should().Be(id);
        result.Value.Name.Should().Be(name);
        result.Value.Description.Should().Be(description);
        result.Value.IsActive.Should().BeTrue();
        result.Value.Members.Should().BeEmpty();
        result.Value.Workspaces.Should().BeEmpty();
        result.Value.CreatedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        result.Value.UpdatedAtUtc.Should().BeNull();
    }

    /// <summary>
    /// Valida que a criação sem ID prévio gera automaticamente um GUID válido.
    /// </summary>
    [Fact]
    public void Create_ComIdVazio_DeveGerarNovoGuid()
    {
        // Act
        var result = Squad.Create(Guid.Empty, "Squad Inbound", null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().NotBeEmpty();
    }

    /// <summary>
    /// Valida que a criação com nome nulo ou em branco falha com erro de validação.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ComNomeInvalido_DeveRetornarErroDeValidacao(string? invalidName)
    {
        // Act
        var result = Squad.Create(Guid.NewGuid(), invalidName!, "Desc");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Squad.EmptyName");
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    /// <summary>
    /// Valida que nomes maiores que 100 caracteres são rejeitados.
    /// </summary>
    [Fact]
    public void Create_ComNomeExcedendo100Caracteres_DeveRetornarErroDeValidacao()
    {
        // Arrange
        var longName = new string('A', 101);

        // Act
        var result = Squad.Create(Guid.NewGuid(), longName, null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Squad.NameTooLong");
    }

    /// <summary>
    /// Valida que descrições com mais de 500 caracteres são rejeitadas.
    /// </summary>
    [Fact]
    public void Create_ComDescricaoExcedendo500Caracteres_DeveRetornarErroDeValidacao()
    {
        // Arrange
        var longDescription = new string('D', 501);

        // Act
        var result = Squad.Create(Guid.NewGuid(), "Squad Alpha", longDescription);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Squad.DescriptionTooLong");
    }

    /// <summary>
    /// Valida a atualização correta dos dados descritivos do squad.
    /// </summary>
    [Fact]
    public void UpdateDetails_ComDadosValidos_DeveAtualizarPropriedadesETimestamp()
    {
        // Arrange
        var squad = Squad.Create(Guid.NewGuid(), "Squad Inicial", "Desc inicial").Value;

        // Act
        var result = squad.UpdateDetails("Squad Atualizado", "Nova desc");

        // Assert
        result.IsSuccess.Should().BeTrue();
        squad.Name.Should().Be("Squad Atualizado");
        squad.Description.Should().Be("Nova desc");
        squad.UpdatedAtUtc.Should().NotBeNull();
    }

    /// <summary>
    /// Valida a alternância de status operacional do squad.
    /// </summary>
    [Fact]
    public void ToggleStatus_DeveModificarStatusETimestamp()
    {
        // Arrange
        var squad = Squad.Create(Guid.NewGuid(), "Squad Teste", null).Value;

        // Act
        squad.ToggleStatus(false);

        // Assert
        squad.IsActive.Should().BeFalse();
        squad.UpdatedAtUtc.Should().NotBeNull();

        squad.ToggleStatus(true);
        squad.IsActive.Should().BeTrue();
    }

    /// <summary>
    /// Valida a adição de membros e bloqueio contra duplicatas na mesma equipe.
    /// </summary>
    [Fact]
    public void AddMember_DeveAdicionarMembroEBloquearDuplicatas()
    {
        // Arrange
        var squad = Squad.Create(Guid.NewGuid(), "Squad Alpha", null).Value;
        var userId = Guid.NewGuid();

        // Act - 1ª adição
        var result1 = squad.AddMember(userId);

        // Assert
        result1.IsSuccess.Should().BeTrue();
        squad.Members.Should().HaveCount(1);
        squad.Members.First().UserId.Should().Be(userId);
        squad.Members.First().SquadId.Should().Be(squad.Id);

        // Act - 2ª adição (duplicata)
        var result2 = squad.AddMember(userId);

        // Assert duplicata
        result2.IsFailure.Should().BeTrue();
        result2.Error.Code.Should().Be("Squad.MemberAlreadyExists");
        result2.Error.Type.Should().Be(ErrorType.Conflict);
        squad.Members.Should().HaveCount(1);
    }

    /// <summary>
    /// Valida a remoção de membros do squad.
    /// </summary>
    [Fact]
    public void RemoveMember_DeveRemoverMembroOuRetornarNotFound()
    {
        // Arrange
        var squad = Squad.Create(Guid.NewGuid(), "Squad Alpha", null).Value;
        var userId = Guid.NewGuid();
        squad.AddMember(userId);

        // Act - Remoção de membro existente
        var removeResult = squad.RemoveMember(userId);

        // Assert
        removeResult.IsSuccess.Should().BeTrue();
        squad.Members.Should().BeEmpty();

        // Act - Remoção de membro não presente
        var removeInexistente = squad.RemoveMember(userId);
        removeInexistente.IsFailure.Should().BeTrue();
        removeInexistente.Error.Code.Should().Be("Squad.MemberNotFound");
        removeInexistente.Error.Type.Should().Be(ErrorType.NotFound);
    }

    /// <summary>
    /// Valida a associação de workspaces à carteira do squad e bloqueio contra duplicatas.
    /// </summary>
    [Fact]
    public void AssignWorkspace_DeveAssociarWorkspaceEBloquearDuplicatas()
    {
        // Arrange
        var squad = Squad.Create(Guid.NewGuid(), "Squad Alpha", null).Value;
        var workspaceId = Guid.NewGuid();

        // Act - 1ª alocação
        var result1 = squad.AssignWorkspace(workspaceId);

        // Assert
        result1.IsSuccess.Should().BeTrue();
        squad.Workspaces.Should().HaveCount(1);
        squad.Workspaces.First().WorkspaceId.Should().Be(workspaceId);

        // Act - 2ª alocação (duplicata)
        var result2 = squad.AssignWorkspace(workspaceId);

        // Assert duplicata
        result2.IsFailure.Should().BeTrue();
        result2.Error.Code.Should().Be("Squad.WorkspaceAlreadyAssigned");
        result2.Error.Type.Should().Be(ErrorType.Conflict);
        squad.Workspaces.Should().HaveCount(1);
    }

    /// <summary>
    /// Valida a desassociação de workspaces da carteira do squad.
    /// </summary>
    [Fact]
    public void UnassignWorkspace_DeveDesassociarOuRetornarNotFound()
    {
        // Arrange
        var squad = Squad.Create(Guid.NewGuid(), "Squad Alpha", null).Value;
        var workspaceId = Guid.NewGuid();
        squad.AssignWorkspace(workspaceId);

        // Act - Desassociação existente
        var unassignResult = squad.UnassignWorkspace(workspaceId);

        // Assert
        unassignResult.IsSuccess.Should().BeTrue();
        squad.Workspaces.Should().BeEmpty();

        // Act - Desassociação de workspace não alocado
        var unassignInexistente = squad.UnassignWorkspace(workspaceId);
        unassignInexistente.IsFailure.Should().BeTrue();
        unassignInexistente.Error.Code.Should().Be("Squad.WorkspaceNotAssigned");
        unassignInexistente.Error.Type.Should().Be(ErrorType.NotFound);
    }
}
