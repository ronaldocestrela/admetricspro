namespace WebApp.State;

/// <summary>
/// Implementação concreta do provedor de estado de Tenant com ciclo de vida Scoped por circuito SignalR.
/// </summary>
public class TenantStateProvider : ITenantStateProvider
{
    private TenantState _currentTenant;
    private bool _isInitialized;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="TenantStateProvider"/> com o estado institucional padrão.
    /// </summary>
    public TenantStateProvider()
    {
        _currentTenant = TenantState.Default;
        _isInitialized = true;
    }

    /// <inheritdoc />
    public TenantState CurrentTenant => _currentTenant;

    /// <inheritdoc />
    public bool IsInitialized => _isInitialized;

    /// <inheritdoc />
    public event Action? OnTenantChanged;

    /// <inheritdoc />
    public void SetTenant(TenantState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        _currentTenant = state;
        _isInitialized = true;
        NotifyStateChanged();
    }

    /// <inheritdoc />
    public Task InitializeAsync(TenantState? initialState = null, CancellationToken cancellationToken = default)
    {
        if (initialState is not null)
        {
            _currentTenant = initialState;
        }

        _isInitialized = true;
        NotifyStateChanged();
        return Task.CompletedTask;
    }

    private void NotifyStateChanged()
    {
        OnTenantChanged?.Invoke();
    }
}
