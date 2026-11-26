using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Infrastructure.MultiTenancy;
using TenantResolutionSource = BuildingBlocks.Application.MultiTenancy.TenantResolutionSource;
using FluentAssertions;
using NSubstitute;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Application.Brand.Commands.UploadBrandVoiceLogo;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Brand;
using Xunit;

namespace UnitTests.Backend.SocialPublishing.Application;

/// <summary>
/// Testes do comando <see cref="UploadBrandVoiceLogoCommandHandler"/>.
/// </summary>
public sealed class UploadBrandVoiceLogoCommandTests
{
    private static readonly byte[] MinimalPng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _tenantId = Guid.NewGuid();

    /// <summary>
    /// Sem perfil de marca deve retornar BrandVoiceNotFound.
    /// </summary>
    [Fact]
    public async Task Handle_SemBrandVoice_DeveRetornarNotFound()
    {
        var repository = Substitute.For<IBrandVoiceRepository>();
        repository.GetByWorkspaceAsync(_workspaceId, Arg.Any<CancellationToken>())
            .Returns((BrandVoiceProfile?)null);

        var handler = CreateHandler(repository, Substitute.For<IBrandLogoStore>(), Substitute.For<ISocialPublishingUnitOfWork>());
        await using var stream = new MemoryStream(MinimalPng);

        var result = await handler.Handle(
            new UploadBrandVoiceLogoCommand(_workspaceId, "logo.png", "image/png", stream),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.BrandVoiceNotFound.Code);
    }

    /// <summary>
    /// Upload válido persiste metadados e retorna HasLogo true.
    /// </summary>
    [Fact]
    public async Task Handle_ComPngValido_DeveAnexarLogo()
    {
        var profile = BrandVoiceProfile.Create(
            _workspaceId,
            "Tom",
            "Persona",
            ["https://example.com"],
            [],
            EmojiPolicy.Sparingly,
            15,
            25,
            25,
            25,
            25).Value;

        var repository = Substitute.For<IBrandVoiceRepository>();
        repository.GetByWorkspaceAsync(_workspaceId, Arg.Any<CancellationToken>())
            .Returns(profile);

        var logoStore = Substitute.For<IBrandLogoStore>();
        logoStore.SaveAsync(_tenantId, _workspaceId, Arg.Any<Stream>(), "image/png", Arg.Any<CancellationToken>())
            .Returns(BuildingBlocks.Domain.Primitives.Result<string>.Success("organic-media/key/logo"));

        var unitOfWork = Substitute.For<ISocialPublishingUnitOfWork>();
        var handler = CreateHandler(repository, logoStore, unitOfWork);

        await using var stream = new MemoryStream(MinimalPng);
        var result = await handler.Handle(
            new UploadBrandVoiceLogoCommand(_workspaceId, "logo.png", "image/png", stream),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.HasLogo.Should().BeTrue();
        result.Value.LogoContentType.Should().Be("image/png");
        await repository.Received(1).UpdateAsync(profile, Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    private UploadBrandVoiceLogoCommandHandler CreateHandler(
        IBrandVoiceRepository repository,
        IBrandLogoStore logoStore,
        ISocialPublishingUnitOfWork unitOfWork)
    {
        var accessor = new TenantContextAccessor
        {
            TenantContext = TenantContext.Create(_tenantId, "tenant", TenantResolutionSource.Header),
        };

        return new UploadBrandVoiceLogoCommandHandler(repository, logoStore, unitOfWork, accessor);
    }
}