using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WebApi.Background;
using Xunit;

namespace AcceptanceTests.Composition;

/// <summary>
/// Registro condicional do job one-shot de migração do volume orgânico (Fase 5).
/// </summary>
public sealed class OrganicMediaVolumeMigrationCompositionTests
{
    /// <summary>
    /// O hosted service de migração só existe com MinIO e flag ativa.
    /// </summary>
    /// <param name="provider">ObjectStorage:Provider.</param>
    /// <param name="migrate">ObjectStorage:MigrateLocalRoot.</param>
    /// <param name="expect">Espera registro do serviço.</param>
    [Theory]
    [InlineData("Local", false, false)]
    [InlineData("MinIO", false, false)]
    [InlineData("MinIO", true, true)]
    public void MigrateLocalRoot_RegistersHostedServiceOnlyWhenMinIoAndFlag(string provider, bool migrate, bool expect)
    {
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ObjectStorage:Provider", provider);
            builder.UseSetting("ObjectStorage:MigrateLocalRoot", migrate.ToString());
            builder.UseSetting("Messaging:Enabled", "false");
        });

        HasHostedService<OrganicMediaVolumeMigrationHostedService>(factory.Services).Should().Be(expect);
    }

    private static bool HasHostedService<T>(IServiceProvider services)
        where T : class, IHostedService
    {
        return services.GetServices<IHostedService>().Any(h => h is T);
    }
}