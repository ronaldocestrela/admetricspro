using BuildingBlocks.Domain.Tenants;
using FluentAssertions;
using NSubstitute;
using Tenants.Application.Branding;
using UnitTests.Backend.ObjectStorage;

namespace UnitTests.Backend.Tenants;

/// <summary>
/// Garante que atualização só com URL externa não grava objeto.
/// </summary>
public sealed class UpdateTenantBrandingAssetSyncTests
{
    private readonly InMemoryObjectStore _objectStore = new();

    /// <summary>
    /// URL externa sem upload não chama PutAsync.
    /// </summary>
    [Fact]
    public async Task SyncAsync_ExternalUrlOnly_DoesNotPutObject()
    {
        var branding = TenantBranding.Create(
            Guid.NewGuid(),
            "#2563EB",
            "#0F172A",
            "https://cdn.example.com/logo.png").Value;

        var tenantId = Guid.NewGuid();
        var resolver = Substitute.For<ITenantBrandPublicUrlResolver>();
        resolver.IsResolvedApiUrl(Arg.Any<Guid>(), Arg.Any<TenantBrandAssetSlot>(), Arg.Any<string?>(), Arg.Any<string?>())
            .Returns(false);

        var store = Substitute.For<ITenantBrandAssetStore>();

        var result = await TenantBrandingAssetUrlSynchronizer.SyncAsync(
            branding,
            tenantId,
            "https://cdn.other.com/new.png",
            null,
            null,
            resolver,
            store,
            "https://api.test",
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await store.DidNotReceive().SaveAsync(
            Arg.Any<TenantBrandAssetSlot>(),
            Arg.Any<Stream>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
        branding.LightLogoObjectKey.Should().BeNull();
    }
}