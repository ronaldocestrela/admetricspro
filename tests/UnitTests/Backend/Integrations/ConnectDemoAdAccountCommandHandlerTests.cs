using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using NSubstitute;
using Tenants.Application.Integrations.Commands.ConnectDemoAdAccount;
using Tenants.Application.Integrations.Repositories;
using Tenants.Application.Persistence;
using Tenants.Application.Workspaces.Repositories;

namespace UnitTests.Backend.Integrations;

/// <summary>
/// Testes unitários para o comando de vinculação de conta de anúncios demonstrativa (<see cref="ConnectDemoAdAccountCommandHandler"/>).
/// </summary>
public sealed class ConnectDemoAdAccountCommandHandlerTests
{
    private readonly IWorkspaceRepository _workspaceRepository = Substitute.For<IWorkspaceRepository>();
    private readonly IConnectedAdAccountRepository _adAccountRepository = Substitute.For<IConnectedAdAccountRepository>();
    private readonly ITenantUnitOfWork _unitOfWork = Substitute.For<ITenantUnitOfWork>();

    private ConnectDemoAdAccountCommandHandler CreateHandler()
    {
        return new ConnectDemoAdAccountCommandHandler(
            _workspaceRepository,
            _adAccountRepository,
            _unitOfWork);
    }

    /// <summary>
    /// Valida que a vinculação de conta demo para um workspace existente é executada com sucesso.
    /// </summary>
    [Fact]
    public async Task Handle_WithValidWorkspace_ShouldCreateDemoAccountAndReturnId()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var workspace = Workspace.Create(workspaceId, "Workspace Teste", "12.345.678/0001-95", 1000m, "E-commerce").Value;

        _workspaceRepository.GetByIdAsync(workspaceId, Arg.Any<CancellationToken>())
            .Returns(workspace);

        var handler = CreateHandler();
        var command = new ConnectDemoAdAccountCommand(workspaceId, "MetaAds");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);
        await _adAccountRepository.Received(1).AddAsync(Arg.Is<ConnectedAdAccount>(a => a.IsDemo && a.WorkspaceId == workspaceId), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Valida que ao tentar vincular a um workspace inexistente, retorna erro NotFound.
    /// </summary>
    [Fact]
    public async Task Handle_WithNonExistentWorkspace_ShouldReturnNotFound()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        _workspaceRepository.GetByIdAsync(workspaceId, Arg.Any<CancellationToken>())
            .Returns((Workspace?)null);

        var handler = CreateHandler();
        var command = new ConnectDemoAdAccountCommand(workspaceId, "MetaAds");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Workspace.NotFound", result.Error.Code);
        await _adAccountRepository.DidNotReceive().AddAsync(Arg.Any<ConnectedAdAccount>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }
}
