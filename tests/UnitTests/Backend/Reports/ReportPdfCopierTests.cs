using Analytics.Infrastructure.Reports;
using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Application.Storage;
using BuildingBlocks.Domain.Reports;
using BuildingBlocks.Infrastructure.MultiTenancy;
using BuildingBlocks.Infrastructure.Storage;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using UnitTests.Backend.ObjectStorage;
using Xunit;
using Analytics.Domain.Reports;

namespace UnitTests.Backend.Reports;

/// <summary>
/// Testes do copiador idempotente de PDFs legados.
/// </summary>
public sealed class ReportPdfCopierTests
{
    private readonly Guid _tenantId = Guid.Parse("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
    private readonly Guid _workspaceId = Guid.Parse("bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb");

    /// <summary>
    /// Copia dois relatórios, ignora um já existente com mesmo tamanho e preenche chave sem apagar coluna.
    /// </summary>
    [Fact]
    public async Task CopyTenantReportsAsync_DeveCopiarIgnorarEPreservarPdfContent()
    {
        var store = new InMemoryObjectStore();
        var pdfStore = CreatePdfStore(_tenantId, store);
        var repository = Substitute.For<IReportRepository>();

        var report1Id = Guid.NewGuid();
        var report2Id = Guid.NewGuid();
        var bytes1 = new byte[] { 1, 2, 3 };
        var bytes2 = new byte[] { 4, 5, 6, 7 };

        var report1 = GeneratedReport.Create(
            report1Id,
            _workspaceId,
            null,
            "R1",
            DateTime.UtcNow.AddDays(-7),
            DateTime.UtcNow,
            null,
            bytes1,
            "{}",
            DateTime.UtcNow).Value;

        var report2 = GeneratedReport.Create(
            report2Id,
            _workspaceId,
            null,
            "R2",
            DateTime.UtcNow.AddDays(-7),
            DateTime.UtcNow,
            null,
            bytes2,
            "{}",
            DateTime.UtcNow).Value;

        repository.ListGeneratedReportsWithPdfBytesAsync(Arg.Any<CancellationToken>())
            .Returns(new List<GeneratedReport> { report1, report2 });

        repository.ListGeneratedReportsWithPdfObjectKeyAsync(Arg.Any<CancellationToken>())
            .Returns(Array.Empty<GeneratedReport>());

        var key2 = $"{_tenantId:N}/{_workspaceId:N}/{report2Id:N}.pdf";
        await store.PutAsync("admetricspro-reports", key2, new MemoryStream(bytes2), "application/pdf", CancellationToken.None);

        var copier = new ReportPdfCopier(
            repository,
            pdfStore,
            store,
            Options.Create(new ObjectStorageOptions { ReportsBucket = "admetricspro-reports", LocalRoot = Path.GetTempPath() }),
            new TenantContextAccessor { TenantContext = TenantContext.Create(_tenantId, "slug", TenantResolutionSource.Header) },
            NullLogger<ReportPdfCopier>.Instance);

        var result = await copier.CopyTenantReportsAsync(CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Copied.Should().Be(1);
        result.Value.Skipped.Should().Be(1);

        report1.PdfContent.Should().NotBeNull();
        report1.PdfObjectKey.Should().NotBeNullOrWhiteSpace();
        report2.PdfObjectKey.Should().Be(key2);

        await repository.Received(2).UpdateGeneratedReportAsync(Arg.Any<GeneratedReport>(), Arg.Any<CancellationToken>());
    }

    private ObjectStoreReportPdfStore CreatePdfStore(Guid tenantId, IObjectStore inner) =>
        new(
            inner,
            Options.Create(new ObjectStorageOptions { ReportsBucket = "admetricspro-reports" }),
            new TenantContextAccessor { TenantContext = TenantContext.Create(tenantId, "slug", TenantResolutionSource.Header) });
}