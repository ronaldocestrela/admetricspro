using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Tenants.Application.Branding;
using Tenants.Application.Branding.Commands.UploadTenantBrandAsset;
using Tenants.Application.Branding.Repositories;
using Tenants.Application.Persistence;
using Tenants.Infrastructure.Branding;
using BuildingBlocks.Infrastructure.Storage;
using UnitTests.Backend.ObjectStorage;

namespace UnitTests.Backend.Tenants;

/// <summary>
/// Testes de <see cref="UploadTenantBrandAssetCommandHandler"/>.
/// </summary>
public sealed class UploadTenantBrandAssetCommandHandlerTests
{
    private readonly ITenantBrandingRepository _brandingRepository = Substitute.For<ITenantBrandingRepository>();
    private readonly ITenantUnitOfWork _unitOfWork = Substitute.For<ITenantUnitOfWork>();
    private readonly ITenantContextAccessor _tenantContextAccessor = Substitute.For<ITenantContextAccessor>();
    private readonly InMemoryObjectStore _objectStore = new();
    private readonly Guid _tenantId = Guid.NewGuid();

    /// <summary>
    /// Upload válido grava chave canônica e URL da API.
    /// </summary>
    [Fact]
    public async Task Handle_ValidPng_ShouldPersistObjectKeyAndApiUrl()
    {
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.IsResolved.Returns(true);
        tenantContext.TenantId.Returns(_tenantId);
        _tenantContextAccessor.TenantContext.Returns(tenantContext);

        var branding = TenantBranding.Create(Guid.NewGuid(), "#2563EB", "#0F172A").Value;
        _brandingRepository.GetAsync(Arg.Any<CancellationToken>()).Returns(branding);

        var store = CreateBrandStore();
        var urlResolver = new TenantBrandPublicUrlResolver(
            Options.Create(new ObjectStorageOptions { PublicBaseUrl = "https://api.test" }));

        var handler = new UploadTenantBrandAssetCommandHandler(
            _brandingRepository,
            store,
            urlResolver,
            _unitOfWork,
            _tenantContextAccessor);

        var png = CreateMinimalPng();
        var command = new UploadTenantBrandAssetCommand(
            TenantBrandAssetSlot.Light,
            "logo.png",
            "image/png",
            new MemoryStream(png),
            "https://api.test");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.LightLogoUrl.Should().Be($"https://api.test/api/v1/public/branding/{_tenantId:N}/light");
        branding.LightLogoObjectKey.Should().Be($"{_tenantId:N}/light");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Arquivo acima de 2 MB falha com validação.
    /// </summary>
    [Fact]
    public async Task Handle_WhenFileTooLarge_ShouldFailValidation()
    {
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.IsResolved.Returns(true);
        tenantContext.TenantId.Returns(_tenantId);
        _tenantContextAccessor.TenantContext.Returns(tenantContext);

        var handler = new UploadTenantBrandAssetCommandHandler(
            _brandingRepository,
            CreateBrandStore(),
            new TenantBrandPublicUrlResolver(Options.Create(new ObjectStorageOptions { PublicBaseUrl = "https://api.test" })),
            _unitOfWork,
            _tenantContextAccessor);

        var oversize = new byte[TenantBrandingAssetLimits.MaxUploadBytes + 1];
        var command = new UploadTenantBrandAssetCommand(
            TenantBrandAssetSlot.Light,
            "big.png",
            "image/png",
            new MemoryStream(oversize),
            "https://api.test");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("TenantBranding.AssetTooLarge");
        _objectStore.Keys.Should().BeEmpty();
    }

    private ObjectStoreTenantBrandAssetStore CreateBrandStore() =>
        new(
            _objectStore,
            Options.Create(new ObjectStorageOptions { BrandingBucket = "admetricspro-branding", LocalRoot = Path.GetTempPath() }),
            _tenantContextAccessor,
            Substitute.For<Microsoft.Extensions.Logging.ILogger<ObjectStoreTenantBrandAssetStore>>());

    private static byte[] CreateMinimalPng() =>
    [
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
        0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
        0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
        0x08, 0x06, 0x00, 0x00, 0x00, 0x1F, 0x15, 0xC4,
        0x89, 0x00, 0x00, 0x00, 0x0A, 0x49, 0x44, 0x41,
        0x54, 0x78, 0x9C, 0x63, 0x00, 0x01, 0x00, 0x00,
        0x05, 0x00, 0x01, 0x0D, 0x0A, 0x2D, 0xB4, 0x00,
        0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44, 0xAE,
        0x42, 0x60, 0x82
    ];
}