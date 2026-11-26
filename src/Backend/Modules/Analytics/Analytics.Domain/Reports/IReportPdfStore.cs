using BuildingBlocks.Domain.Primitives;

namespace Analytics.Domain.Reports;

/// <summary>
/// Porta de armazenamento de PDFs de relatório no bucket de relatórios (metadado no SQL).
/// </summary>
public interface IReportPdfStore
{
    /// <summary>
    /// Monta a chave determinística do PDF para o relatório informado.
    /// </summary>
    /// <param name="workspaceId">Workspace avaliado.</param>
    /// <param name="reportId">Identificador do relatório gerado.</param>
    /// <returns>Chave relativa no bucket de relatórios.</returns>
    string BuildObjectKey(Guid workspaceId, Guid reportId);

    /// <summary>
    /// Grava o conteúdo PDF no bucket de relatórios.
    /// </summary>
    /// <param name="workspaceId">Workspace do relatório.</param>
    /// <param name="reportId">Identificador do relatório.</param>
    /// <param name="content">Stream de leitura do PDF.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Chave gravada ou falha de storage.</returns>
    Task<Result<string>> SaveAsync(
        Guid workspaceId,
        Guid reportId,
        Stream content,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Abre leitura do PDF pela chave persistida.
    /// </summary>
    /// <param name="objectKey">Chave no bucket.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Fluxo legível ou falha.</returns>
    Task<Result<Stream>> OpenReadAsync(string objectKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove o objeto PDF.
    /// </summary>
    /// <param name="objectKey">Chave no bucket.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Sucesso ou falha de storage.</returns>
    Task<Result> DeleteAsync(string objectKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém o tamanho em bytes do objeto, se existir.
    /// </summary>
    /// <param name="objectKey">Chave no bucket.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Tamanho ou ausência.</returns>
    Task<Result<long?>> GetContentLengthAsync(string objectKey, CancellationToken cancellationToken = default);
}