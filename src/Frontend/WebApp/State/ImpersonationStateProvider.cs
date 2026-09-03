namespace WebApp.State;

/// <summary>
/// Implementação concreta do provedor de estado de impersonation com ciclo de vida Scoped.
/// </summary>
public sealed class ImpersonationStateProvider : IImpersonationStateProvider
{
    private ImpersonationSessionState _currentSession = ImpersonationSessionState.Inactive;

    /// <inheritdoc />
    public ImpersonationSessionState CurrentSession => _currentSession;

    /// <inheritdoc />
    public event Action? OnSessionChanged;

    /// <inheritdoc />
    public void SetSession(ImpersonationSessionState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        _currentSession = state;
        OnSessionChanged?.Invoke();
    }

    /// <inheritdoc />
    public void ClearSession()
    {
        _currentSession = ImpersonationSessionState.Inactive;
        OnSessionChanged?.Invoke();
    }
}
