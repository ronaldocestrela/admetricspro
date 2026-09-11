using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using FluentAssertions;
using NSubstitute;
using Tenants.Application.Squads.Queries.GetUserAccessibleWorkspaces;
using Tenants.Application.Squads.Services;
using Tenants.Application.Workspaces.DTOs;
using Tenants.Application.Workspaces.Repositories;
using Xunit;

namespace UnitTests.Backend.Tenants;

/// <summary>
/// Testes unitários para <see cref="GetUserAccessibleWorkspacesQueryHandler"/>.
/// </summary>
public sealed class GetUserAccessibleWorkspacesQueryHandlerTests
{
    private readonly IUserPortfolioService _portfolioService = Substitute.For<IUserPortfolioService>();
    private readonly IWorkspaceRepository _workspaceRepository = Substitute.For<IWorkspaceRepository>();
    private readonly GetUserAccessibleWorkspacesQueryHandler _handler;

    /// <summary>
    /// Inicializa uma nova instância dos testes de <see cref="GetUserAccessibleWorkspacesQueryHandler"/>.
    /// </summary>
    public GetUserAccessibleWorkspacesQueryHandlerTests()
    {
        _handler = new GetUserAccessibleWorkspacesQueryHandler(_portfolioService, _workspaceRepository);
    }

    /// <summary>
    /// Valida que quando o serviço de portfólio falha (ex: usuário inexistente), o handler propaga a falha.
    /// </summary>
    [Fact]
    public async Task Handle_QuandoPortfolioServiceFalha_DevePropagarFalha()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var query = new GetUserAccessibleWorkspacesQuery(userId);
        var erro = Error.NotFound("User.NotFound", "Usuário não localizado.");

        _portfolioService.GetAccessibleWorkspaceIdsAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<Guid>>.Failure(erro));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.NotFound");
        await _workspaceRepository.DidNotReceive().GetAllAsync(Arg.Any<bool?>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Valida que quando o usuário não possui nenhum workspace acessível, retorna lista vazia.
    /// </summary>
    [Fact]
    public async Task Handle_QuandoNenhumWorkspaceAcessivel_DeveRetornarListaVaziaSemConsultarRepo()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var query = new GetUserAccessibleWorkspacesQuery(userId);

        _portfolioService.GetAccessibleWorkspaceIdsAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<Guid>>.Success(Array.Empty<Guid>()));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
        await _workspaceRepository.DidNotReceive().GetAllAsync(Arg.Any<bool?>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Valida que retorna apenas os workspaces cujos IDs foram autorizados pelo serviço de portfólio.
    /// </summary>
    [Fact]
    public async Task Handle_QuandoPossuiWorkspacesAcessiveis_DeveRetornarSomenteOsAutorizados()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var authorizedWsId = Guid.NewGuid();
        var unauthorizedWsId = Guid.NewGuid();

        var query = new GetUserAccessibleWorkspacesQuery(userId);

        var ws1 = Workspace.Create(authorizedWsId, "Cliente Autorizado", "123.456.789-09", 5000m, "Varejo").Value;
        var ws2 = Workspace.Create(unauthorizedWsId, "Cliente Não Autorizado", "987.654.321-00", 3000m, "Educação").Value;

        _portfolioService.GetAccessibleWorkspaceIdsAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<Guid>>.Success(new List<Guid> { authorizedWsId }));

        _workspaceRepository.GetAllAsync(cancellationToken: Arg.Any<CancellationToken>())
            .Returns(new List<Workspace> { ws1, ws2 });

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value.First().Id.Should().Be(authorizedWsId);
        result.Value.First().Name.Should().Be("Cliente Autorizado");
    }
}
