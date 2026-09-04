namespace BackofficeApp.State;

/// <summary>
/// Provedor de estado scoped para monitoramento de sessões ativas de Shadow Mode no circuito Blazor.
/// </summary>
public interface IImpersonationStateProvider
{
    /// <summary>
    /// Obtém o estado atual da sessão de impersonation.
    /// </summary>
    ImpersonationSessionState CurrentSession { get; }

    /// <summary>
    /// Evento disparado sempre que a sessão de impersonation é iniciada, atualizada ou encerrada.
    /// </summary>
    event Action? OnSessionChanged;

    /// <summary>
    /// Define e ativa uma nova sessão de impersonation no circuito.
    /// </summary>
    /// <param name="state">Novo estado da sessão.</param>
    void SetSession(ImpersonationSessionState state);

    /// <summary>
    /// Limpa o estado da sessão, retornando ao modo padrão inativo.
    /// </summary>
    void ClearSession();
}
