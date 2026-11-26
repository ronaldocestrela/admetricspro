using Analytics.Application.Reports;
using Analytics.Domain.Reports;
using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Application.Storage;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Infrastructure.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Analytics.Infrastructure.Reports;

/// <summary>
/// Copia <c>PdfContent</c> legado e arquivos locais para o bucket de relatórios.
/// </summary>
public sealed class ReportPdfCopier
{
    private const string PdfContentType = "application/pdf";

    private readonly IReportRepository _repository;
    private readonly IReportPdfStore _pdfStore;
    private readonly IObjectStore _objectStore;
    private readonly ObjectStorageOptions _objectStorageOptions;
    private readonly ITenantContextAccessor _tenantContextAccessor;
    private readonly ILogger<ReportPdfCopier> _logger;

    /// <summary>
    /// Inicializa o copiador de PDFs de relatório.
    /// </summary>
    public ReportPdfCopier(
        IReportRepository repository,
        IReportPdfStore pdfStore,
        IObjectStore objectStore,
        IOptions<ObjectStorageOptions> objectStorageOptions,
        ITenantContextAccessor tenantContextAccessor,
        ILogger<ReportPdfCopier> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _pdfStore = pdfStore ?? throw new ArgumentNullException(nameof(pdfStore));
        _objectStore = objectStore ?? throw new ArgumentNullException(nameof(objectStore));
        _objectStorageOptions = objectStorageOptions?.Value ?? throw new ArgumentNullException(nameof(objectStorageOptions));
        _tenantContextAccessor = tenantContextAccessor ?? throw new ArgumentNullException(nameof(tenantContextAccessor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Copia bytes do SQL e arquivos locais pendentes para o bucket de relatórios.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Contagens da execução.</returns>
    public async Task<Result<ReportPdfMigrationCopyReport>> CopyTenantReportsAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContextAccessor.TenantContext?.TenantId;
        if (tenantId is null || tenantId == Guid.Empty)
        {
            return Result<ReportPdfMigrationCopyReport>.Failure(StorageErrors.KeyInvalid);
        }

        var copied = 0;
        var skipped = 0;
        var failed = 0;

        var withBytes = await _repository.ListGeneratedReportsWithPdfBytesAsync(cancellationToken);
        foreach (var report in withBytes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var key = string.IsNullOrWhiteSpace(report.PdfObjectKey)
                ? ReportPdfObjectKeyFormatter.Build(tenantId.Value, report.WorkspaceId, report.Id)
                : report.PdfObjectKey!;

            var outcome = await PutIfNeededAsync(key, report.PdfContent!, cancellationToken);
            switch (outcome)
            {
                case CopyOutcome.Copied:
                    report.AssignPdfObjectKey(key);
                    await _repository.UpdateGeneratedReportAsync(report, cancellationToken);
                    copied++;
                    _logger.LogInformation("PDF de relatório copiado. ReportId={ReportId}, Key={Key}", report.Id, key);
                    break;
                case CopyOutcome.Skipped:
                    if (string.IsNullOrWhiteSpace(report.PdfObjectKey))
                    {
                        report.AssignPdfObjectKey(key);
                        await _repository.UpdateGeneratedReportAsync(report, cancellationToken);
                    }

                    skipped++;
                    break;
                case CopyOutcome.Failed:
                    failed++;
                    break;
            }
        }

        var withKey = await _repository.ListGeneratedReportsWithPdfObjectKeyAsync(cancellationToken);
        foreach (var report in withKey)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var key = report.PdfObjectKey!;
            var remoteLength = await _pdfStore.GetContentLengthAsync(key, cancellationToken);
            if (remoteLength.IsSuccess && remoteLength.Value.HasValue)
            {
                continue;
            }

            var localPath = MapLocalPath(key);
            if (!File.Exists(localPath))
            {
                continue;
            }

            await using var file = File.OpenRead(localPath);
            var put = await _objectStore.PutAsync(
                _objectStorageOptions.ReportsBucket,
                key,
                file,
                PdfContentType,
                cancellationToken);

            if (put.IsFailure)
            {
                failed++;
                _logger.LogWarning("Falha ao enviar PDF local. ReportId={ReportId}, Key={Key}, Code={Code}", report.Id, key, put.Error.Code);
            }
            else
            {
                copied++;
                _logger.LogInformation("PDF local enviado ao bucket. ReportId={ReportId}, Key={Key}", report.Id, key);
            }
        }

        return Result<ReportPdfMigrationCopyReport>.Success(new ReportPdfMigrationCopyReport(copied, skipped, failed));
    }

    private async Task<CopyOutcome> PutIfNeededAsync(string key, byte[] content, CancellationToken cancellationToken)
    {
        var lengthResult = await _pdfStore.GetContentLengthAsync(key, cancellationToken);
        if (lengthResult.IsSuccess && lengthResult.Value == content.LongLength)
        {
            return CopyOutcome.Skipped;
        }

        await using var stream = new MemoryStream(content);
        var put = await _objectStore.PutAsync(
            _objectStorageOptions.ReportsBucket,
            key,
            stream,
            PdfContentType,
            cancellationToken);

        return put.IsFailure ? CopyOutcome.Failed : CopyOutcome.Copied;
    }

    private string MapLocalPath(string key) =>
        Path.Combine(_objectStorageOptions.LocalRoot, key.Replace('/', Path.DirectorySeparatorChar));

    private enum CopyOutcome
    {
        Copied,
        Skipped,
        Failed,
    }
}