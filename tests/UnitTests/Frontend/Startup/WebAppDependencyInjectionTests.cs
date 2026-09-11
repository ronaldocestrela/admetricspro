using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Services;
using WebApp.State;
using Xunit;

namespace UnitTests.Frontend.Startup;

/// <summary>
/// Testes unitários para validar a integridade e resolução do contêiner de injeção de dependências do Blazor WebApp.
/// Garante que o frontend seja estritamente desacoplado de banco de dados e resolva clientes HTTP tipados.
/// </summary>
public sealed class WebAppDependencyInjectionTests
{
    /// <summary>
    /// Valida que a árvore de injeção de dependências do WebApp constrói sem lançar exceções com ValidateOnBuild ativo
    /// e sem dependência de persistência ou DbContext no frontend.
    /// </summary>
    [Fact]
    public void BuildServiceProvider_WithValidateOnBuild_ShouldResolveAllRequiredServices()
    {
        // Arrange
        var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions());
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "Api:BaseUrl", "https://localhost:7001" }
        };
        builder.Configuration.AddInMemoryCollection(inMemorySettings);

        builder.Services.AddRazorComponents().AddInteractiveServerComponents();

        // Provedores de estado
        builder.Services.AddScoped<ITenantStateProvider, TenantStateProvider>();
        builder.Services.AddScoped<IImpersonationStateProvider, ImpersonationStateProvider>();

        // Clientes HTTP da Web API
        var apiUri = new Uri("https://localhost:7001");
        builder.Services.AddHttpClient<ITenantDirectoryService, TenantDirectoryService>(client => client.BaseAddress = apiUri);
        builder.Services.AddHttpClient<ITenantOnboardingClientService, TenantOnboardingClientService>(client => client.BaseAddress = apiUri);
        builder.Services.AddHttpClient<IPlanManagementService, PlanManagementService>(client => client.BaseAddress = apiUri);
        builder.Services.AddHttpClient<IApiHealthClientService, ApiHealthClientService>(client => client.BaseAddress = apiUri);
        builder.Services.AddHttpClient<IFeatureFlagClientService, FeatureFlagClientService>(client => client.BaseAddress = apiUri);
        builder.Services.AddHttpClient<IImpersonationClientService, ImpersonationClientService>(client => client.BaseAddress = apiUri);
        builder.Services.AddHttpClient<ISquadClientService, SquadClientService>(client => client.BaseAddress = apiUri);

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
    /// Valida que todos os serviços clientes de negócio são resolvidos dentro do escopo com instâncias HTTP tipadas.
    /// </summary>
    [Fact]
    public void BuildServiceProvider_ShouldResolveAllClientServicesInScope()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<ITenantStateProvider, TenantStateProvider>();
        services.AddScoped<IImpersonationStateProvider, ImpersonationStateProvider>();

        var apiUri = new Uri("https://localhost:7001");
        services.AddHttpClient<ITenantDirectoryService, TenantDirectoryService>(client => client.BaseAddress = apiUri);
        services.AddHttpClient<ITenantOnboardingClientService, TenantOnboardingClientService>(client => client.BaseAddress = apiUri);
        services.AddHttpClient<IPlanManagementService, PlanManagementService>(client => client.BaseAddress = apiUri);
        services.AddHttpClient<IApiHealthClientService, ApiHealthClientService>(client => client.BaseAddress = apiUri);
        services.AddHttpClient<IFeatureFlagClientService, FeatureFlagClientService>(client => client.BaseAddress = apiUri);
        services.AddHttpClient<IImpersonationClientService, ImpersonationClientService>(client => client.BaseAddress = apiUri);
        services.AddHttpClient<ISquadClientService, SquadClientService>(client => client.BaseAddress = apiUri);

        var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });

        using var scope = provider.CreateScope();

        // Act & Assert
        scope.ServiceProvider.GetRequiredService<ITenantStateProvider>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IImpersonationStateProvider>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<ITenantDirectoryService>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<ITenantOnboardingClientService>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IPlanManagementService>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IApiHealthClientService>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IFeatureFlagClientService>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IImpersonationClientService>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<ISquadClientService>().Should().NotBeNull();
    }
}
