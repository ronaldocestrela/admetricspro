using BuildingBlocks.Application.Security;
using BuildingBlocks.Infrastructure.Security;
using FluentAssertions;
using Master.Application.DependencyInjection;
using Master.Infrastructure.Extensions;
using Master.Infrastructure.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Services;
using WebApp.State;
using Xunit;

namespace UnitTests.Frontend.Startup;

/// <summary>
/// Testes unitários para validar a integridade e resolução do contêiner de injeção de dependências do Blazor WebApp.
/// Garante que nenhuma regressão de serviços faltando (como IImpersonationContext) cause falha no startup.
/// </summary>
public sealed class WebAppDependencyInjectionTests
{
    /// <summary>
    /// Valida que a árvore de injeção de dependências do WebApp constrói sem lançar exceções com ValidateOnBuild ativo.
    /// </summary>
    [Fact]
    public void BuildServiceProvider_WithValidateOnBuild_ShouldResolveAllRequiredServices()
    {
        // Arrange
        var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions());
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "ConnectionStrings:MasterDb", "Server=dummy;Database=MasterDb;Trusted_Connection=True;TrustServerCertificate=True;" },
            { "Api:BaseUrl", "https://localhost:7001" },
            { "ImpersonationJwt:SecretKey", "123456789012345678901234567890123456789012345678" }
        };
        builder.Configuration.AddInMemoryCollection(inMemorySettings);

        builder.Services.AddRazorComponents().AddInteractiveServerComponents();
        builder.Services.AddMasterCatalog("Server=dummy;Database=MasterDb;Trusted_Connection=True;TrustServerCertificate=True;");
        builder.Services.AddMasterApplication();
        builder.Services.AddSecurityServices();
        builder.Services.Configure<ImpersonationJwtOptions>(options =>
        {
            builder.Configuration.GetSection(ImpersonationJwtOptions.SectionName).Bind(options);
        });

        builder.Services.AddScoped<ITenantStateProvider, TenantStateProvider>();
        builder.Services.AddScoped<ITenantDirectoryService, TenantDirectoryService>();
        builder.Services.AddScoped<IPlanManagementService, PlanManagementService>();
        builder.Services.AddScoped<IApiHealthClientService, ApiHealthClientService>();
        builder.Services.AddScoped<IFeatureFlagClientService, FeatureFlagClientService>();
        builder.Services.AddScoped<IImpersonationStateProvider, ImpersonationStateProvider>();
        builder.Services.AddHttpClient<IImpersonationClientService, ImpersonationClientService>(client =>
        {
            client.BaseAddress = new Uri("https://localhost:7001");
        });

        // Act
        var act = () => builder.Services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });

        // Assert
        act.Should().NotThrow("todas as dependências registradas no WebApp devem ser resolvidas com sucesso");
    }

    /// <summary>
    /// Valida que serviços críticos de segurança e contexto de impersonação são resolvidos dentro do escopo.
    /// </summary>
    [Fact]
    public void BuildServiceProvider_ShouldResolveSecurityAndImpersonationContext()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMasterCatalog("Server=dummy;Database=MasterDb;Trusted_Connection=True;TrustServerCertificate=True;");
        services.AddMasterApplication();
        services.AddSecurityServices();
        services.AddOptions();
        services.Configure<ImpersonationJwtOptions>(_ => { });

        var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });

        using var scope = provider.CreateScope();

        // Act & Assert
        scope.ServiceProvider.GetRequiredService<IImpersonationContextAccessor>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IImpersonationContext>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IBillingDataMasker>().Should().NotBeNull();
    }
}
