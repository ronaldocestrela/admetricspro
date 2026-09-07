using BuildingBlocks.Domain.Tenants;
using FluentAssertions;
using NSubstitute;
using Tenants.Application.Squads.Queries.GetSquads;
using Tenants.Application.Squads.Repositories;
using Xunit;

namespace UnitTests.Backend.Tenants;

/// <summary>
/// Testes unitários para <see cref="GetSquadsQueryHandler"/>.
/// </summary>
public sealed class GetSquadsQueryHandlerTests
{
    private readonly ISquadRepository _squadRepository = Substitute.For<ISquadRepository>();
    private readonly GetSquadsQueryHandler _handler;

    /// <summary>
    /// Construtor dos testes.
    /// </summary>
    public GetSquadsQueryHandlerTests()
    {
        _handler = new GetSquadsQueryHandler(_squadRepository);
    }

    /// <summary>
    /// Valida que a consulta lista corretamente os squads com contadores de membros e workspaces.
    /// </summary>
    [Fact]
    public async Task Handle_DeveRetornarListaDeSquadsComContadores()
    {
        // Arrange
        var squad1 = Squad.Create(Guid.NewGuid(), "Squad Alpha", "Desc Alpha").Value;
        squad1.AddMember(Guid.NewGuid());
        squad1.AssignWorkspace(Guid.NewGuid());
        squad1.AssignWorkspace(Guid.NewGuid());

        var squad2 = Squad.Create(Guid.NewGuid(), "Squad Beta", null).Value;

        _squadRepository.GetAllAsync(null, Arg.Any<CancellationToken>())
            .Returns(new List<Squad> { squad1, squad2 });

        var query = new GetSquadsQuery(null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);

        var first = result.Value.First(s => s.Name == "Squad Alpha");
        first.MemberCount.Should().Be(1);
        first.WorkspaceCount.Should().Be(2);

        var second = result.Value.First(s => s.Name == "Squad Beta");
        second.MemberCount.Should().Be(0);
        second.WorkspaceCount.Should().Be(0);
    }
}
