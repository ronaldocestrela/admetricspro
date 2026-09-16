using BuildingBlocks.Domain.Primitives;

namespace Analytics.Domain.Reports;

/// <summary>
/// Contrato do gerador de arquivos PDF executivos com identidade visual White-Label da agência.
/// </summary>
public interface IReportPdfGenerator
{
    /// <summary>
    /// Renderiza os dados do relatório em um documento binário PDF com cabeçalho, tabelas e rodapé da agência.
    /// </summary>
    /// <param name="model">Modelo de dados consolidado com métricas e metadados de branding.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    /// <returns>Bytes do arquivo PDF ou falha na renderização.</returns>
    Task<Result<byte[]>> GeneratePdfAsync(ReportRenderModel model, CancellationToken cancellationToken = default);
}
