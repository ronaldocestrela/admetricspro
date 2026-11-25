using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using WebApi.Extensions;

namespace UnitTests.Backend.HealthChecks;

/// <summary>
/// Testes unitários do health check condicional do cluster MinIO (Fase 1 object storage).
/// </summary>
public sealed class MinioHealthCheckTests
{
    /// <summary>
    /// Com <c>ObjectStorage:Provider=Local</c>, o check <c>minio</c> não é registrado e o agregado permanece saudável.
    /// </summary>
    [Fact]
    public async Task WhenProviderLocal_ShouldNotRegisterMinioCheck_AndReportHealthy()
    {
        var services = CreateHealthCheckServices(new Dictionary<string, string?>
        {
            ["ObjectStorage:Provider"] = "Local",
        });

        await AssertHealthAsync(services, expectMinioEntry: false, expectedStatus: HealthStatus.Healthy);
    }

    /// <summary>
    /// Sem a chave <c>ObjectStorage:Provider</c>, o comportamento equivale a <c>Local</c> (deploy atual).
    /// </summary>
    [Fact]
    public async Task WhenProviderKeyAbsent_ShouldNotRegisterMinioCheck_AndReportHealthy()
    {
        var services = CreateHealthCheckServices(new Dictionary<string, string?>());

        await AssertHealthAsync(services, expectMinioEntry: false, expectedStatus: HealthStatus.Healthy);
    }

    /// <summary>
    /// Com <c>Provider=MinIO</c> e endpoint inacessível, o check nomeado <c>minio</c> fica <see cref="HealthStatus.Unhealthy"/>.
    /// </summary>
    [Fact]
    public async Task WhenProviderMinIoAndEndpointUnreachable_ShouldReportMinioUnhealthy()
    {
        var services = CreateHealthCheckServices(new Dictionary<string, string?>
        {
            ["ObjectStorage:Provider"] = "MinIO",
            ["ObjectStorage:ServiceUrl"] = "http://127.0.0.1:59900",
        });

        await AssertHealthAsync(services, expectMinioEntry: true, expectedStatus: HealthStatus.Unhealthy);
    }

    /// <summary>
    /// <c>Provider=minio</c> em minúsculas é aceito (comparação ordinal, case-insensitive).
    /// </summary>
    [Fact]
    public async Task WhenProviderMinIoCaseInsensitiveAndEndpointUnreachable_ShouldReportMinioUnhealthy()
    {
        var services = CreateHealthCheckServices(new Dictionary<string, string?>
        {
            ["ObjectStorage:Provider"] = "minio",
            ["ObjectStorage:ServiceUrl"] = "http://127.0.0.1:59900",
        });

        await AssertHealthAsync(services, expectMinioEntry: true, expectedStatus: HealthStatus.Unhealthy);
    }

    private static ServiceProvider CreateHealthCheckServices(IReadOnlyDictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHealthChecks()
            .AddMinioHealthCheck(configuration);

        return services.BuildServiceProvider();
    }

    private static async Task AssertHealthAsync(
        ServiceProvider provider,
        bool expectMinioEntry,
        HealthStatus expectedStatus)
    {
        await using (provider)
        {
            var healthCheckService = provider.GetRequiredService<HealthCheckService>();
            var report = await healthCheckService.CheckHealthAsync();

            report.Status.Should().Be(expectedStatus);

            report.Entries.ContainsKey("minio").Should().Be(expectMinioEntry);
            if (expectMinioEntry)
            {
                report.Entries["minio"].Status.Should().Be(HealthStatus.Unhealthy);
            }
        }
    }
}