using BuildingBlocks.Application.Storage;
using BuildingBlocks.Domain.Primitives;
using FluentAssertions;

namespace UnitTests.Backend.ObjectStorage;

/// <summary>
/// Valida a regra pura de chave de objeto por tenant (Fase 2 MinIO).
/// </summary>
public sealed class ObjectKeyTests
{
    private static readonly Guid TenantId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
    private static readonly Guid OtherTenantId = Guid.Parse("11111111-2222-3333-4444-555555555555");
    private static readonly Guid WorkspaceId = Guid.Parse("ffffffff-1111-2222-3333-444444444444");
    private static readonly Guid AssetId = Guid.Parse("99999999-8888-7777-6666-555555555555");

    private static string TenantN => TenantId.ToString("N");
    private static string OtherTenantN => OtherTenantId.ToString("N");
    private static string WorkspaceN => WorkspaceId.ToString("N");
    private static string AssetN => AssetId.ToString("N");

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WhenKeyMissing_ReturnsKeyInvalid(string? key)
    {
        var result = ObjectKey.Create(key, TenantId);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(StorageErrors.KeyInvalid);
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Theory]
    [InlineData("tenant\\key")]
    [InlineData(@"organic-media\durable\x")]
    public void Create_WhenKeyContainsBackslash_ReturnsKeyInvalid(string key)
    {
        var result = ObjectKey.Create(key, TenantId);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Storage.KeyInvalid");
    }

    [Fact]
    public void Create_WhenKeyContainsDotDot_ReturnsKeyInvalid()
    {
        ObjectKey.Create("../secret", TenantId).Error.Code.Should().Be("Storage.KeyInvalid");
        ObjectKey.Create($"{TenantN}/..", TenantId).Error.Code.Should().Be("Storage.KeyInvalid");
        ObjectKey.Create($"organic-media/durable/../{WorkspaceN}/{AssetN}", TenantId).Error.Code.Should()
            .Be("Storage.KeyInvalid");
    }

    [Fact]
    public void Create_WhenKeyHasEmptySegment_ReturnsKeyInvalid()
    {
        ObjectKey.Create("/only-leading", TenantId).Error.Code.Should().Be("Storage.KeyInvalid");
        ObjectKey.Create("trailing/", TenantId).Error.Code.Should().Be("Storage.KeyInvalid");
        ObjectKey.Create($"{TenantN}//rest", TenantId).Error.Code.Should().Be("Storage.KeyInvalid");
    }

    [Fact]
    public void Create_WhenOrganicDurableKeyMatchesTenant_Succeeds()
    {
        var key = $"organic-media/durable/{TenantN}/{WorkspaceN}/{AssetN}";

        var result = ObjectKey.Create(key, TenantId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(key);
    }

    [Fact]
    public void Create_WhenOrganicStagingKeyMatchesTenant_Succeeds()
    {
        var key = $"organic-media/staging/{TenantN}/{WorkspaceN}/{AssetN}";

        var result = ObjectKey.Create(key, TenantId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(key);
    }

    [Fact]
    public void Create_WhenOrganicKeyUsesOtherTenant_ReturnsKeyInvalid()
    {
        var key = $"organic-media/durable/{OtherTenantN}/{WorkspaceN}/{AssetN}";

        var result = ObjectKey.Create(key, TenantId);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Storage.KeyInvalid");
    }

    [Fact]
    public void Create_WhenPrefixKeyMatchesTenant_Succeeds()
    {
        var key = $"{TenantN}/light";

        var result = ObjectKey.Create(key, TenantId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(key);
    }

    [Fact]
    public void Create_WhenReportStyleKeyMatchesTenant_Succeeds()
    {
        var reportId = Guid.NewGuid();
        var key = $"{TenantN}/{WorkspaceN}/{reportId:N}.pdf";

        var result = ObjectKey.Create(key, TenantId);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_WhenKeyStartsWithOtherTenant_ReturnsKeyInvalid()
    {
        var key = $"{OtherTenantN}/light";

        var result = ObjectKey.Create(key, TenantId);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Storage.KeyInvalid");
    }

    [Fact]
    public void Create_WhenTenantOnlySegment_ReturnsKeyInvalid()
    {
        var result = ObjectKey.Create(TenantN, TenantId);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Storage.KeyInvalid");
    }

    [Fact]
    public void StorageErrors_UseStableCodesAndSemanticTypes()
    {
        StorageErrors.KeyInvalid.Code.Should().Be("Storage.KeyInvalid");
        StorageErrors.KeyInvalid.Type.Should().Be(ErrorType.Validation);
        StorageErrors.ObjectNotFound.Type.Should().Be(ErrorType.NotFound);
        StorageErrors.WriteFailed.Code.Should().Be("Storage.WriteFailed");
        StorageErrors.Disabled.Code.Should().Be("Storage.Disabled");
    }
}