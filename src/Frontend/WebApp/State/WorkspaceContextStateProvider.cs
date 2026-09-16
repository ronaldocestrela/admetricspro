namespace WebApp.State;

/// <summary>
/// Modelo serializável para persistência do cliente/workspace ativo no armazenamento do navegador.
/// </summary>
/// <param name="WorkspaceId">Identificador único do workspace.</param>
/// <param name="WorkspaceName">Nome comercial do workspace/cliente.</param>
public record ActiveWorkspaceStoredModel(Guid WorkspaceId, string? WorkspaceName);

/// <summary>
/// Implementação concreta do provedor de estado de cliente/workspace ativo (<see cref="IWorkspaceContextStateProvider"/>).
/// Registrado com ciclo de vida Scoped por circuito SignalR, mantendo persistência sincronizada no navegador.
/// </summary>
public sealed class WorkspaceContextStateProvider : IWorkspaceContextStateProvider
{
    private const string StorageKey = "admetricspro_active_workspace";
    private readonly IBrowserStorageService _storage;

    private Guid? _currentWorkspaceId;
    private string? _currentWorkspaceName;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="WorkspaceContextStateProvider"/>.
    /// </summary>
    /// <param name="storage">Serviço de armazenamento no navegador.</param>
    public WorkspaceContextStateProvider(IBrowserStorageService storage)
    {
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
    }

    /// <inheritdoc />
    public Guid? CurrentWorkspaceId => _currentWorkspaceId;

    /// <inheritdoc />
    public string? CurrentWorkspaceName => _currentWorkspaceName;

    /// <inheritdoc />
    public bool HasWorkspaceSelected => _currentWorkspaceId.HasValue && _currentWorkspaceId.Value != Guid.Empty;

    /// <inheritdoc />
    public event Action? OnWorkspaceChanged;

    /// <inheritdoc />
    public async Task SetActiveWorkspaceAsync(Guid? workspaceId, string? workspaceName, CancellationToken cancellationToken = default)
    {
        if (!workspaceId.HasValue || workspaceId.Value == Guid.Empty)
        {
            _currentWorkspaceId = null;
            _currentWorkspaceName = null;

            try
            {
                await _storage.RemoveItemAsync(StorageKey, cancellationToken);
            }
            catch
            {
                // Silencioso em caso de restrições de JSInterop durante pré-render
            }
        }
        else
        {
            _currentWorkspaceId = workspaceId;
            _currentWorkspaceName = workspaceName;

            try
            {
                var model = new ActiveWorkspaceStoredModel(workspaceId.Value, workspaceName);
                await _storage.SetItemAsync(StorageKey, model, cancellationToken);
            }
            catch
            {
                // Silencioso em caso de restrições de JSInterop durante pré-render
            }
        }

        OnWorkspaceChanged?.Invoke();
    }

    /// <inheritdoc />
    public async Task<Guid?> RestoreActiveWorkspaceAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var stored = await _storage.GetItemAsync<ActiveWorkspaceStoredModel>(StorageKey, cancellationToken);
            if (stored is not null && stored.WorkspaceId != Guid.Empty)
            {
                _currentWorkspaceId = stored.WorkspaceId;
                _currentWorkspaceName = stored.WorkspaceName;
                OnWorkspaceChanged?.Invoke();
                return _currentWorkspaceId;
            }
        }
        catch
        {
            // Tratamento gracioso se o JSInterop não estiver disponível ou dados corrompidos
        }

        return null;
    }

    /// <inheritdoc />
    public void Clear()
    {
        _currentWorkspaceId = null;
        _currentWorkspaceName = null;
        OnWorkspaceChanged?.Invoke();
    }
}
