using Analytics.Infrastructure.Reports;
using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Application.Storage;
using BuildingBlocks.Infrastructure.MultiTenancy;
using BuildingBlocks.Infrastructure.Storage;
using FluentAssertions;
using Microsoft.Extensions.Options;
using UnitTests.Backend.ObjectStorage;
using Xunit;

namespace UnitTests.Backend.Reports;

/// <summary>
/// Testes do adaptador <see cref="ObjectStoreReportPdfStore"/>.
/// </summary>
public sealed class ReportPdfStoreTests
{
    private readonly Guid _tenantId = Guid.Parse("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
    private readonly Guid _workspaceId = Guid.Parse("bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb");
    private readonly Guid _reportId = Guid.Parse("cccccccccccccccccccccccccccccccc");

    /// <summary>
    /// A mesma combinação tenant/workspace/report deve produzir a chave padrão.
    /// </summary>
    [Fact]
    public void BuildObjectKey_DeveFormatarChavePadrao()
    {
        var store = CreateStore(_tenantId, new InMemoryObjectStore());
        var key = store.BuildObjectKey(_workspaceId, _reportId);
        key.Should().Be($"{_tenantId:N}/{_workspaceId:N}/{_reportId:N}.pdf");
    }

    /// <summary>
    /// Save com tenant divergente do contexto deve falhar sem gravar.
    /// </summary>
    [Fact]
    public async Task SaveAsync_ComTenantDivergente_DeveRetornarKeyInvalid()
    {
        var fake = new InMemoryObjectStore();
        var store = CreateStore(_tenantId, fake);

        await using var stream = new MemoryStream([0x25, 0x50, 0x44, 0x46]);
        var result = await store.SaveAsync(_workspaceId, _reportId, stream, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var exists = await fake.ExistsAsync("admetricspro-reports", result.Value, CancellationToken.None);
        exists.Value.Should().BeTrue();
    }

    private ObjectStoreReportPdfStore CreateStore(Guid tenantId, IObjectStore inner)
    {
        var accessor = new TenantContextAccessor
        {
            TenantContext = TenantContext.Create(tenantId, "slug", TenantResolutionSource.Header),
        };

        return new ObjectStoreReportPdfStore(
            inner,
            Options.Create(new ObjectStorageOptions { ReportsBucket = "admetricspro-reports" }),
            accessor);
    }
}