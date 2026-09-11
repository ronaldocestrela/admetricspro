using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using NSubstitute;
using Tenants.Application.Squads.Queries.ValidateUserWorkspaceAccess;
using Tenants.Application.Squads.Services;
using Xunit;

namespace UnitTests.Backend.Tenants;

/// <summary>
/// Testes unitários para <see cref="ValidateUserWorkspaceAccessQueryHandler"/>.
/// </summary>
public sealed class ValidateUserWorkspaceAccessQueryHandlerTests
{
    private readonly IUserPortfolioService _portfolioService = Substitute.For<IUserPortfolioService>();
    private readonly ValidateUserWorkspaceAccessQueryHandler _handler;

    /// <summary>
    /// Inicializa a suíte de testes de <see cref="ValidateUserWorkspaceAccessQueryHandler"/>.
    /// </summary>
    public ValidateUserWorkspaceAccessQueryHandlerTests()
    {
        _handler = new ValidateUserWorkspaceAccessQueryHandler(_portfolioService);
    }

    /// <summary>
    /// Valida que quando o usuário tem acesso ao workspace, o handler retorna sucesso com valor true.
    /// </summary>
    [Fact]
    public async Task Handle_QuandoUsuarioTemAcesso_DeveRetornarTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var query = new ValidateUserWorkspaceAccessQuery(userId, workspaceId);

        _portfolioService.HasAccessToWorkspaceAsync(userId, workspaceId, Arg.Any<CancellationToken>())
            .Returns(Result<bool>.Success(true));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    /// <summary>
    /// Valida que quando o usuário não tem acesso ao workspace, o handler retorna sucesso com valor false.
    /// </summary>
    [Fact]
    public async Task Handle_QuandoUsuarioNaoTemAcesso_DeveRetornarFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var query = new ValidateUserWorkspaceAccessQuery(userId, workspaceId);

        _portfolioService.HasAccessToWorkspaceAsync(userId, workspaceId, Arg.Any<CancellationToken>())
            .Returns(Result<bool>.Success(false));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    /// <summary>
    /// Valida que quando o serviço de portfólio falha (ex.: usuário inativo), o handler propaga o erro.
    /// </summary>
    [Fact]
    public async Task Handle_QuandoUsuarioInativo_DevePropagarErro()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var query = new ValidateUserWorkspaceAccessQuery(userId, workspaceId);
        var erro = Error.Validation("User.Inactive", "Colaborador inativo.");

        _portfolioService.HasAccessToWorkspaceAsync(userId, workspaceId, Arg.Any<CancellationToken>())
            .Returns(Result<bool>.Failure(erro));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.Inactive");
    }
}
