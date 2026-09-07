using BuildingBlocks.Domain.Tenants;
using FluentAssertions;
using NSubstitute;
using Tenants.Application.Workspaces.Queries.GetWorkspaceById;
using Tenants.Application.Workspaces.Queries.GetWorkspaces;
using Tenants.Application.Workspaces.Repositories;
using Xunit;

namespace UnitTests.Backend.Tenants;

/// <summary>
/// Testes unitários para consultas de workspaces (<see cref="GetWorkspacesQueryHandler"/> e <see cref="GetWorkspaceByIdQueryHandler"/>).
/// </summary>
public sealed class GetWorkspacesQueryHandlerTests
{
    private readonly IWorkspaceRepository _workspaceRepository = Substitute.For<IWorkspaceRepository>();
    private const string ValidCpf = "123.456.789-09";

    /// <summary>
    /// Valida que a consulta de listagem projeta as entidades para DTOs corretamente.
    /// </summary>
    [Fact]
    public async Task Handle_GetWorkspaces_DeveRetornarListaDeDtos()
    {
        // Arrange
        var w1 = Workspace.Create(Guid.NewGuid(), "Cliente 1", ValidCpf, 1000m, "Varejo").Value;
        var w2 = Workspace.Create(Guid.NewGuid(), "Cliente 2", ValidCpf, 2000m, "SaaS").Value;
        _workspaceRepository.GetAllAsync(null, Arg.Any<CancellationToken>())
            .Returns(new List<Workspace> { w1, w2 });

        var handler = new GetWorkspacesQueryHandler(_workspaceRepository);
        var query = new GetWorkspacesQuery(null);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value[0].Name.Should().Be("Cliente 1");
        result.Value[1].Name.Should().Be("Cliente 2");
    }

    /// <summary>
    /// Valida que ao buscar workspace por ID existente, retorna o DTO.
    /// </summary>
    [Fact]
    public async Task Handle_GetWorkspaceById_QuandoExiste_DeveRetornarDto()
    {
        // Arrange
        var id = Guid.NewGuid();
        var workspace = Workspace.Create(id, "Cliente 1", ValidCpf, 1000m, "Varejo").Value;
        _workspaceRepository.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(workspace);

        var handler = new GetWorkspaceByIdQueryHandler(_workspaceRepository);
        var query = new GetWorkspaceByIdQuery(id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(id);
        result.Value.Name.Should().Be("Cliente 1");
    }

    /// <summary>
    /// Valida que ao buscar workspace por ID inexistente, retorna erro NotFound.
    /// </summary>
    [Fact]
    public async Task Handle_GetWorkspaceById_QuandoNaoExiste_DeveRetornarNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        _workspaceRepository.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns((Workspace?)null);

        var handler = new GetWorkspaceByIdQueryHandler(_workspaceRepository);
        var query = new GetWorkspaceByIdQuery(id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Workspace.NotFound");
    }
}
