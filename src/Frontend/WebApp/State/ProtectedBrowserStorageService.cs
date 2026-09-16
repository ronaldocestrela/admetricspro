using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace WebApp.State;

/// <summary>
/// Implementação de armazenamento no navegador baseada em <see cref="ProtectedLocalStorage"/> do Blazor Server,
/// provendo criptografia em repouso dos dados da sessão e tolerância a falhas durante pré-renderização estática.
/// </summary>
public sealed class ProtectedBrowserStorageService : IBrowserStorageService
{
    private readonly ProtectedLocalStorage _protectedLocalStorage;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="ProtectedBrowserStorageService"/>.
    /// </summary>
    /// <param name="protectedLocalStorage">Provedor seguro de armazenamento local do ASP.NET Core Blazor.</param>
    public ProtectedBrowserStorageService(ProtectedLocalStorage protectedLocalStorage)
    {
        _protectedLocalStorage = protectedLocalStorage ?? throw new ArgumentNullException(nameof(protectedLocalStorage));
    }

    /// <inheritdoc />
    public async ValueTask SetItemAsync<T>(string key, T value, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        try
        {
            await _protectedLocalStorage.SetAsync(key, value!);
        }
        catch
        {
            // Silencioso se invocado durante pré-renderização ou se JSInterop estiver desconectado
        }
    }

    /// <inheritdoc />
    public async ValueTask<T?> GetItemAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        try
        {
            var result = await _protectedLocalStorage.GetAsync<T>(key);
            if (result.Success)
            {
                return result.Value;
            }
        }
        catch
        {
            // Retorna padrão graciosamente em falhas de JSInterop ou criptografia
        }

        return default;
    }

    /// <inheritdoc />
    public async ValueTask RemoveItemAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        try
        {
            await _protectedLocalStorage.DeleteAsync(key);
        }
        catch
        {
            // Silencioso se invocado durante desconexão
        }
    }
}
