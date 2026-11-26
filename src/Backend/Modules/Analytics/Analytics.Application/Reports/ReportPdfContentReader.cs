using Analytics.Domain.Reports;
using BuildingBlocks.Application.Storage;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Reports;

namespace Analytics.Application.Reports;

/// <summary>
/// Resolve bytes do PDF priorizando object storage e caindo na coluna legada.
/// </summary>
public sealed class ReportPdfContentReader
{
    private readonly IReportPdfStore _pdfStore;

    /// <summary>
    /// Inicializa o leitor de PDF de relatório.
    /// </summary>
    /// <param name="pdfStore">Porta de object storage do módulo.</param>
    public ReportPdfContentReader(IReportPdfStore pdfStore)
    {
        _pdfStore = pdfStore ?? throw new ArgumentNullException(nameof(pdfStore));
    }

    /// <summary>
    /// Carrega o PDF do relatório conforme regra de migração (objeto primeiro).
    /// </summary>
    /// <param name="report">Relatório gerado.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Bytes do PDF ou falha de negócio.</returns>
    public async Task<Result<byte[]>> ReadPdfBytesAsync(GeneratedReport report, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(report);

        if (!string.IsNullOrWhiteSpace(report.PdfObjectKey))
        {
            var streamResult = await _pdfStore.OpenReadAsync(report.PdfObjectKey, cancellationToken);
            if (streamResult.IsFailure)
            {
                return Result<byte[]>.Failure(streamResult.Error);
            }

            await using var stream = streamResult.Value;
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer, cancellationToken);
            return Result<byte[]>.Success(buffer.ToArray());
        }

        if (report.PdfContent is { Length: > 0 })
        {
            return Result<byte[]>.Success(report.PdfContent);
        }

        return Result<byte[]>.Failure(Error.NotFound("Report.PdfNotFound", "Arquivo PDF deste relatório não foi gerado."));
    }
}