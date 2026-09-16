namespace WebApp.State;

/// <summary>
/// Provedor de estado contextual do Cliente/Workspace ativo no circuito Blazor Server.
/// Mantém o workspace em foco sincronizado e persistido entre atualizações de tela (F5) e navegação.
/// </summary>
public interface IWorkspaceContextStateProvider
{
    /// <summary>
    /// Identificador único do Workspace/Cliente atualmente selecionado, ou nulo para o consolidado ("Todos").
    /// </summary>
    Guid? CurrentWorkspaceId { get; }

    /// <summary>
    /// Nome amigável do Workspace/Cliente selecionado, ou nulo para o consolidado.
    /// </summary>
    string? CurrentWorkspaceName { get; }

    /// <summary>
    /// Indica se há um workspace específico selecionado no momento.
    /// </summary>
    bool HasWorkspaceSelected { get; }

    /// <summary>
    /// Evento notificado quando o workspace ativo é alterado na sessão.
    /// </summary>
    event Action? OnWorkspaceChanged;

    /// <summary>
    /// Define e persiste o Workspace ativo para a sessão do usuário.
    /// </summary>
    /// <param name="workspaceId">Identificador do workspace ou nulo para consolidado.</param>
    /// <param name="workspaceName">Nome de exibição do workspace ou nulo.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    Task SetActiveWorkspaceAsync(Guid? workspaceId, string? workspaceName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restaura o Workspace ativo persistido no armazenamento local do navegador.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Identificador do workspace restaurado ou nulo.</returns>
    Task<Guid?> RestoreActiveWorkspaceAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Limpa o workspace selecionado, redefinindo para a visão consolidada.
    /// </summary>
    void Clear();
}
