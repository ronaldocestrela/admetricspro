using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using WebApp.State;
using Xunit;

namespace UnitTests.Frontend.State;

/// <summary>
/// Testes unitários para o provedor de contexto de cliente/workspace (<see cref="WorkspaceContextStateProvider"/>).
/// Valida seleção ativa, persistência em storage, restauração pós-reload (F5) e notificações de eventos.
/// </summary>
public sealed class WorkspaceContextStateProviderTests
{
    private readonly IBrowserStorageService _storage = Substitute.For<IBrowserStorageService>();

    /// <summary>
    /// Valida que ao instanciar, nenhum workspace está selecionado por padrão.
    /// </summary>
    [Fact]
    public void InitialState_ShouldHaveNoWorkspaceSelected()
    {
        // Arrange & Act
        var sut = new WorkspaceContextStateProvider(_storage);

        // Assert
        sut.CurrentWorkspaceId.Should().BeNull();
        sut.CurrentWorkspaceName.Should().BeNull();
        sut.HasWorkspaceSelected.Should().BeFalse();
    }

    /// <summary>
    /// Valida que ao definir um workspace ativo, o estado é atualizado, o evento é disparado e os dados são persistidos no storage.
    /// </summary>
    [Fact]
    public async Task SetActiveWorkspaceAsync_ShouldUpdateState_NotifyEvent_AndPersistToStorage()
    {
        // Arrange
        var sut = new WorkspaceContextStateProvider(_storage);
        var eventFired = false;
        sut.OnWorkspaceChanged += () => eventFired = true;

        var workspaceId = Guid.NewGuid();
        const string workspaceName = "Loja Virtual Alpha";

        // Act
        await sut.SetActiveWorkspaceAsync(workspaceId, workspaceName);

        // Assert
        sut.CurrentWorkspaceId.Should().Be(workspaceId);
        sut.CurrentWorkspaceName.Should().Be(workspaceName);
        sut.HasWorkspaceSelected.Should().BeTrue();
        eventFired.Should().BeTrue();

        await _storage.Received(1).SetItemAsync(
            "admetricspro_active_workspace",
            Arg.Is<ActiveWorkspaceStoredModel>(m => m.WorkspaceId == workspaceId && m.WorkspaceName == workspaceName),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Valida que ao definir null ou Guid.Empty, a seleção é limpa e o item é removido do storage.
    /// </summary>
    [Fact]
    public async Task SetActiveWorkspaceAsync_WithNull_ShouldClearStateAndRemoveFromStorage()
    {
        // Arrange
        var sut = new WorkspaceContextStateProvider(_storage);
        var initialId = Guid.NewGuid();
        await sut.SetActiveWorkspaceAsync(initialId, "Workspace Inicial");

        var eventFired = false;
        sut.OnWorkspaceChanged += () => eventFired = true;

        // Act
        await sut.SetActiveWorkspaceAsync(null, null);

        // Assert
        sut.CurrentWorkspaceId.Should().BeNull();
        sut.CurrentWorkspaceName.Should().BeNull();
        sut.HasWorkspaceSelected.Should().BeFalse();
        eventFired.Should().BeTrue();

        await _storage.Received(1).RemoveItemAsync("admetricspro_active_workspace", Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Valida restauração bem-sucedida a partir de dados encontrados no storage do navegador (simulando pós-F5).
    /// </summary>
    [Fact]
    public async Task RestoreActiveWorkspaceAsync_WhenStoredValueExists_ShouldRestoreState()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        const string workspaceName = "Cliente Beta E-commerce";
        var stored = new ActiveWorkspaceStoredModel(workspaceId, workspaceName);

        _storage.GetItemAsync<ActiveWorkspaceStoredModel>("admetricspro_active_workspace", Arg.Any<CancellationToken>())
            .Returns(new ValueTask<ActiveWorkspaceStoredModel?>(stored));

        var sut = new WorkspaceContextStateProvider(_storage);
        var eventFired = false;
        sut.OnWorkspaceChanged += () => eventFired = true;

        // Act
        var restoredId = await sut.RestoreActiveWorkspaceAsync();

        // Assert
        restoredId.Should().Be(workspaceId);
        sut.CurrentWorkspaceId.Should().Be(workspaceId);
        sut.CurrentWorkspaceName.Should().Be(workspaceName);
        sut.HasWorkspaceSelected.Should().BeTrue();
        eventFired.Should().BeTrue();
    }

    /// <summary>
    /// Valida que quando o storage está vazio ou lança exceção (ex: pré-renderização), o estado permanece nulo sem quebrar o fluxo.
    /// </summary>
    [Fact]
    public async Task RestoreActiveWorkspaceAsync_WhenStorageThrowsOrEmpty_ShouldGracefullyRemainNull()
    {
        // Arrange
        _storage.GetItemAsync<ActiveWorkspaceStoredModel>("admetricspro_active_workspace", Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("JavaScript interop not available during prerendering."));

        var sut = new WorkspaceContextStateProvider(_storage);

        // Act
        var restoredId = await sut.RestoreActiveWorkspaceAsync();

        // Assert
        restoredId.Should().BeNull();
        sut.CurrentWorkspaceId.Should().BeNull();
        sut.HasWorkspaceSelected.Should().BeFalse();
    }

    /// <summary>
    /// Valida que Clear() remove a seleção ativa e dispara notificação para re-renderização das telas.
    /// </summary>
    [Fact]
    public async Task Clear_ShouldResetSelectionAndNotifyEvent()
    {
        // Arrange
        var sut = new WorkspaceContextStateProvider(_storage);
        await sut.SetActiveWorkspaceAsync(Guid.NewGuid(), "Cliente Teste");

        var eventFired = false;
        sut.OnWorkspaceChanged += () => eventFired = true;

        // Act
        sut.Clear();

        // Assert
        sut.CurrentWorkspaceId.Should().BeNull();
        sut.CurrentWorkspaceName.Should().BeNull();
        sut.HasWorkspaceSelected.Should().BeFalse();
        eventFired.Should().BeTrue();
    }
}
