namespace WebApp.State;

/// <summary>
/// Abstração para armazenamento seguro de dados no navegador do cliente (local/session storage),
/// provendo tolerância a falhas durante o ciclo de pré-renderização estática do Blazor Server.
/// </summary>
public interface IBrowserStorageService
{
    /// <summary>
    /// Armazena um valor tipado associado a uma chave de identificação.
    /// </summary>
    /// <typeparam name="T">Tipo do dado a ser persistido.</typeparam>
    /// <param name="key">Chave única de armazenamento.</param>
    /// <param name="value">Instância do objeto a persistir.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação assíncrona.</param>
    ValueTask SetItemAsync<T>(string key, T value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Recupera um valor tipado a partir de sua chave. Retorna padrão (null) se não existir ou se JSInterop estiver indisponível.
    /// </summary>
    /// <typeparam name="T">Tipo do dado recuperado.</typeparam>
    /// <param name="key">Chave única de consulta.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação assíncrona.</param>
    /// <returns>Objeto desserializado ou valor padrão.</returns>
    ValueTask<T?> GetItemAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove um item associado à chave do armazenamento local.
    /// </summary>
    /// <param name="key">Chave do item a ser removido.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação assíncrona.</param>
    ValueTask RemoveItemAsync(string key, CancellationToken cancellationToken = default);
}
