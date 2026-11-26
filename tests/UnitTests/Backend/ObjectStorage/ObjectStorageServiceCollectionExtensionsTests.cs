using BuildingBlocks.Application.Storage;
using BuildingBlocks.Infrastructure.MultiTenancy;
using BuildingBlocks.Infrastructure.Storage;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace UnitTests.Backend.ObjectStorage;

/// <summary>
/// Registro condicional de <see cref="IObjectStore"/> (Fase 3).
/// </summary>
public sealed class ObjectStorageServiceCollectionExtensionsTests
{
    /// <summary>
    /// Com provedor Local, registra apenas o store de disco.
    /// </summary>
    [Fact]
    public void AddObjectStorage_WhenProviderLocal_RegistersLocalObjectStore_WithoutAmazonS3()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ObjectStorage:Provider"] = "Local",
            })
            .Build();

        services.AddObjectStorage(configuration);

        services.Should().Contain(sd =>
            sd.ServiceType == typeof(IObjectStore) &&
            sd.ImplementationType == typeof(LocalObjectStore) &&
            sd.Lifetime == ServiceLifetime.Singleton);
        services.Should().NotContain(sd => sd.ServiceType.FullName == "Amazon.S3.IAmazonS3");
    }

    /// <summary>
    /// Sem chave de provedor, o padrão é Local.
    /// </summary>
    [Fact]
    public void AddObjectStorage_WhenProviderKeyAbsent_RegistersLocalObjectStore()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        services.AddObjectStorage(configuration);

        services.Should().Contain(sd =>
            sd.ServiceType == typeof(IObjectStore) &&
            sd.ImplementationType == typeof(LocalObjectStore));
        services.Should().NotContain(sd => sd.ServiceType.FullName == "Amazon.S3.IAmazonS3");
    }

    /// <summary>
    /// Provedor MinIO registra cliente S3 e implementação Minio.
    /// </summary>
    [Theory]
    [InlineData("MinIO")]
    [InlineData("minio")]
    public void AddObjectStorage_WhenProviderMinIo_RegistersMinioObjectStoreAndAmazonS3(string provider)
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ObjectStorage:Provider"] = provider,
                ["ObjectStorage:ServiceUrl"] = "http://127.0.0.1:9000",
                ["ObjectStorage:AccessKey"] = "test",
                ["ObjectStorage:SecretKey"] = "test",
                ["ObjectStorage:Region"] = "us-east-1",
            })
            .Build();

        services.AddLogging();
        services.AddMultiTenancy();
        services.AddObjectStorage(configuration);

        services.Should().Contain(sd =>
            sd.ServiceType == typeof(IObjectStore) &&
            sd.ImplementationType == typeof(MinioObjectStore) &&
            sd.Lifetime == ServiceLifetime.Singleton);
        services.Should().Contain(sd =>
            sd.ServiceType.FullName == "Amazon.S3.IAmazonS3" &&
            sd.Lifetime == ServiceLifetime.Singleton);
    }

    /// <summary>
    /// Provedor desconhecido falha na composição do DI.
    /// </summary>
    [Fact]
    public void AddObjectStorage_WhenProviderUnknown_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ObjectStorage:Provider"] = "Azure",
            })
            .Build();

        var act = () => services.AddObjectStorage(configuration);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ObjectStorage:Provider*");
    }
}