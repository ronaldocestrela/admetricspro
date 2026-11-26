using BuildingBlocks.Domain.Primitives;

namespace BuildingBlocks.Application.Storage;

/// <summary>
/// Porta de armazenamento de objetos por bucket e chave (implementações em Infrastructure).
/// </summary>
public interface IObjectStore
{
    /// <summary>
    /// Grava ou substitui o conteúdo de um objeto.
    /// </summary>
    /// <param name="bucket">Nome do bucket.</param>
    /// <param name="key">Chave do objeto no bucket.</param>
    /// <param name="content">Fluxo de leitura do conteúdo.</param>
    /// <param name="contentType">Tipo MIME.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Sucesso ou falha de storage.</returns>
    Task<Result> PutAsync(
        string bucket,
        string key,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Abre leitura do objeto.
    /// </summary>
    /// <param name="bucket">Nome do bucket.</param>
    /// <param name="key">Chave do objeto.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Fluxo legível ou falha (incluindo objeto ausente).</returns>
    Task<Result<Stream>> OpenReadAsync(
        string bucket,
        string key,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove o objeto se existir.
    /// </summary>
    /// <param name="bucket">Nome do bucket.</param>
    /// <param name="key">Chave do objeto.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Sucesso; exclusão de chave ausente é idempotente.</returns>
    Task<Result> DeleteAsync(
        string bucket,
        string key,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica existência do objeto.
    /// </summary>
    /// <param name="bucket">Nome do bucket.</param>
    /// <param name="key">Chave do objeto.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><c>true</c> ou <c>false</c> em sucesso.</returns>
    Task<Result<bool>> ExistsAsync(
        string bucket,
        string key,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém o tamanho em bytes do objeto, se existir.
    /// </summary>
    /// <param name="bucket">Nome do bucket.</param>
    /// <param name="key">Chave do objeto.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><see langword="null"/> quando ausente; caso contrário o tamanho em sucesso.</returns>
    Task<Result<long?>> GetContentLengthAsync(
        string bucket,
        string key,
        CancellationToken cancellationToken = default);
}