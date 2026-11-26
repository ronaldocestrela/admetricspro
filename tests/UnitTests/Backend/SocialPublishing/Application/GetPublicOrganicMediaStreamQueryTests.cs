using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Infrastructure.MultiTenancy;
using BuildingBlocks.Infrastructure.Storage;
using FluentAssertions;
using Microsoft.Extensions.Options;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Application.Media.Queries.GetPublicOrganicMediaStream;
using SocialPublishing.Application.Options;
using SocialPublishing.Domain;
using SocialPublishing.Infrastructure.Media;
using Xunit;

namespace UnitTests.Backend.SocialPublishing.Application;

/// <summary>
/// Testes do handler de stream público de mídia orgânica.
/// </summary>
public sealed class GetPublicOrganicMediaStreamQueryTests
{
    private const string SigningKey = "organic-media-publish-signing-key-32-chars!";
    private readonly Guid _tenantId = Guid.Parse("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");

    /// <summary>
    /// Token válido abre o stream com content type do payload.
    /// </summary>
    [Fact]
    public async Task Handle_TokenValido_RetornaStreamComContentType()
    {
        var key = $"organic-media/durable/{_tenantId:N}/{Guid.NewGuid():N}/{Guid.NewGuid():N}";
        var tokenService = CreateTokenService();
        var token = tokenService.IssueToken(_tenantId, key, "image/png", DateTime.UtcNow).Value;
        var store = new FakeOrganicMediaStore(key, [1, 2, 3]);
        var handler = CreateHandler(tokenService, store, tenantId: null);

        var result = await handler.Handle(new GetPublicOrganicMediaStreamQuery(token), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ContentType.Should().Be("image/png");
        using var reader = new MemoryStream();
        await result.Value.Content.CopyToAsync(reader);
        reader.ToArray().Should().Equal(1, 2, 3);
    }

    /// <summary>
    /// Token expirado retorna erro opaco.
    /// </summary>
    [Fact]
    public async Task Handle_TokenExpirado_RetornaNotFound()
    {
        var key = $"organic-media/durable/{_tenantId:N}/{Guid.NewGuid():N}/{Guid.NewGuid():N}";
        var issuedAt = new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);
        var tokenService = CreateTokenService();
        var token = tokenService.IssueToken(_tenantId, key, "image/png", issuedAt).Value;
        var handler = CreateHandler(
            tokenService,
            new FakeOrganicMediaStore(key, [1]),
            tenantId: null,
            utcNow: issuedAt.AddHours(3));

        var result = await handler.Handle(new GetPublicOrganicMediaStreamQuery(token), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.PublicMediaNotFound.Code);
    }

    /// <summary>
    /// Tenant do contexto divergente do token retorna 404 lógico.
    /// </summary>
    [Fact]
    public async Task Handle_TenantDivergente_RetornaNotFound()
    {
        var otherTenant = Guid.NewGuid();
        var key = $"organic-media/durable/{_tenantId:N}/{Guid.NewGuid():N}/{Guid.NewGuid():N}";
        var tokenService = CreateTokenService();
        var token = tokenService.IssueToken(_tenantId, key, "image/png", DateTime.UtcNow).Value;
        var handler = CreateHandler(tokenService, new FakeOrganicMediaStore(key, [1]), tenantId: otherTenant);

        var result = await handler.Handle(new GetPublicOrganicMediaStreamQuery(token), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.PublicMediaNotFound.Code);
    }

    private OrganicMediaPublishTokenService CreateTokenService() =>
        new(
            Options.Create(new ObjectStorageOptions
            {
                PublishUrlSigningKey = SigningKey,
                PublishUrlTtlMinutes = 120,
            }),
            Options.Create(new OrganicMediaOptions()));

    private GetPublicOrganicMediaStreamQueryHandler CreateHandler(
        IOrganicMediaPublishTokenService tokenService,
        IOrganicMediaStore store,
        Guid? tenantId,
        DateTime? utcNow = null)
    {
        var accessor = new TenantContextAccessor();
        if (tenantId.HasValue)
        {
            accessor.TenantContext = TenantContext.Create(tenantId.Value, "slug", TenantResolutionSource.Header);
        }
        else
        {
            accessor.TenantContext = TenantContext.Empty;
        }

        return new GetPublicOrganicMediaStreamQueryHandler(
            tokenService,
            store,
            accessor,
            utcNow.HasValue ? new FixedTimeProvider(utcNow.Value) : TimeProvider.System);
    }

    private sealed class FakeOrganicMediaStore(string key, byte[] content) : IOrganicMediaStore
    {
        public Task<BuildingBlocks.Domain.Primitives.Result<string>> SaveAsync(
            Guid tenantId,
            Guid workspaceId,
            Guid assetId,
            Stream stream,
            string contentType,
            OrganicMediaStorageTier tier,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(BuildingBlocks.Domain.Primitives.Result<string>.Success(key));

        public Task<BuildingBlocks.Domain.Primitives.Result<Stream>> OpenReadAsync(
            string objectKey,
            CancellationToken cancellationToken = default)
        {
            if (objectKey != key)
            {
                return Task.FromResult(BuildingBlocks.Domain.Primitives.Result<Stream>.Failure(
                    SocialErrors.PublicMediaNotFound));
            }

            return Task.FromResult(BuildingBlocks.Domain.Primitives.Result<Stream>.Success(
                new MemoryStream(content, writable: false)));
        }

        public Task<BuildingBlocks.Domain.Primitives.Result> DeleteAsync(
            string objectKey,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(BuildingBlocks.Domain.Primitives.Result.Success());
    }

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}