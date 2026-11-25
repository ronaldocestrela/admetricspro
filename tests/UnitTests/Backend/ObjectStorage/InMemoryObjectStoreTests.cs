using System.Reflection;
using BuildingBlocks.Application.Storage;
using BuildingBlocks.Domain.Primitives;
using FluentAssertions;

namespace UnitTests.Backend.ObjectStorage;

/// <summary>
/// Comportamento do fake em memória de <see cref="IObjectStore"/> (sem rede).
/// </summary>
public sealed class InMemoryObjectStoreTests
{
    [Fact]
    public void IObjectStore_MustNotExposeAmazonTypes()
    {
        var interfaceType = typeof(IObjectStore);
        var methods = interfaceType.GetMethods();

        foreach (var method in methods)
        {
            AssertNoAmazonType(method.ReturnType, $"{interfaceType.Name}.{method.Name} return");
            foreach (var param in method.GetParameters())
            {
                AssertNoAmazonType(param.ParameterType, $"{interfaceType.Name}.{method.Name} param {param.Name}");
            }
        }
    }

    [Fact]
    public async Task PutAsync_ThenOpenRead_ReturnsSameBytes()
    {
        var store = new InMemoryObjectStore();
        var bucket = "admetricspro-organic";
        var key = "organic-media/durable/test";
        var payload = new byte[] { 1, 2, 3, 4 };
        await using var input = new MemoryStream(payload);

        var put = await store.PutAsync(bucket, key, input, "application/octet-stream");
        put.IsSuccess.Should().BeTrue();

        var read = await store.OpenReadAsync(bucket, key);
        read.IsSuccess.Should().BeTrue();
        await using var stream = read.Value;
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        ms.ToArray().Should().Equal(payload);
    }

    [Fact]
    public async Task PutAsync_SecondPut_ReplacesContentAndContentType()
    {
        var store = new InMemoryObjectStore();
        var bucket = "b";
        var key = "k";

        await store.PutAsync(bucket, key, new MemoryStream([1]), "text/plain");
        await store.PutAsync(bucket, key, new MemoryStream([9, 9]), "image/png");

        var read = await store.OpenReadAsync(bucket, key);
        using var ms = new MemoryStream();
        await read.Value.CopyToAsync(ms);
        ms.ToArray().Should().Equal([9, 9]);

        var exists = await store.ExistsAsync(bucket, key);
        exists.IsSuccess.Should().BeTrue();
        exists.Value.Should().BeTrue();
    }

    [Fact]
    public async Task OpenReadAsync_WhenMissing_ReturnsObjectNotFound()
    {
        var store = new InMemoryObjectStore();

        var read = await store.OpenReadAsync("bucket", "missing");

        read.IsFailure.Should().BeTrue();
        read.Error.Should().Be(StorageErrors.ObjectNotFound);
    }

    [Fact]
    public async Task DeleteAsync_WhenMissing_IsSuccess()
    {
        var store = new InMemoryObjectStore();

        var delete = await store.DeleteAsync("bucket", "never");

        delete.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_ThenOpenRead_ReturnsObjectNotFound()
    {
        var store = new InMemoryObjectStore();
        const string bucket = "b";
        const string key = "k";

        await store.PutAsync(bucket, key, new MemoryStream([1]), "application/octet-stream");
        (await store.DeleteAsync(bucket, key)).IsSuccess.Should().BeTrue();
        (await store.DeleteAsync(bucket, key)).IsSuccess.Should().BeTrue();

        var read = await store.OpenReadAsync(bucket, key);
        read.IsFailure.Should().BeTrue();
        read.Error.Code.Should().Be("Storage.ObjectNotFound");
    }

    [Fact]
    public async Task ExistsAsync_ReturnsFalseWithoutObjectNotFound()
    {
        var store = new InMemoryObjectStore();

        var exists = await store.ExistsAsync("bucket", "nope");

        exists.IsSuccess.Should().BeTrue();
        exists.Value.Should().BeFalse();
    }

    [Fact]
    public async Task SameKey_DifferentBuckets_DoNotCollide()
    {
        var store = new InMemoryObjectStore();
        const string key = "shared-key";

        await store.PutAsync("bucket-a", key, new MemoryStream([1]), "text/plain");
        await store.PutAsync("bucket-b", key, new MemoryStream([2, 2]), "text/plain");

        var readA = await store.OpenReadAsync("bucket-a", key);
        using var msA = new MemoryStream();
        await readA.Value.CopyToAsync(msA);
        msA.ToArray().Should().Equal([1]);

        var readB = await store.OpenReadAsync("bucket-b", key);
        using var msB = new MemoryStream();
        await readB.Value.CopyToAsync(msB);
        msB.ToArray().Should().Equal([2, 2]);
    }

    private static void AssertNoAmazonType(Type type, string context)
    {
        if (type == typeof(void))
        {
            return;
        }

        var toCheck = type;
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>))
        {
            toCheck = type.GetGenericArguments()[0];
        }

        if (toCheck.IsGenericType && toCheck.GetGenericTypeDefinition() == typeof(Result<>))
        {
            toCheck = toCheck.GetGenericArguments()[0];
        }

        if (toCheck == typeof(Result) || toCheck == typeof(Stream) || toCheck.IsPrimitive || toCheck == typeof(string))
        {
            return;
        }

        var fullName = toCheck.FullName ?? toCheck.Name;
        fullName.Should().NotContain("Amazon", context);
    }
}