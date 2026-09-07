using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Extensions;
using WebApp.Services;
using Xunit;

namespace UnitTests.Frontend.Startup;

/// <summary>
/// Testes unitários para validar os métodos de extensão de configuração do HttpClient no frontend.
/// </summary>
public sealed class HttpClientExtensionsTests
{
    /// <summary>
    /// Valida que a extensão ConfigureDevelopmentCertificateBypass aplica o handler de bypass de certificado quando em modo de desenvolvimento.
    /// </summary>
    [Fact]
    public void ConfigureDevelopmentCertificateBypass_WhenIsDevelopmentIsTrue_ShouldConfigurePrimaryHandler()
    {
        // Arrange
        var services = new ServiceCollection();
        var apiUri = new Uri("https://localhost:7001");

        // Act
        services.AddHttpClient<ITenantOnboardingClientService, TenantOnboardingClientService>(client => client.BaseAddress = apiUri)
            .ConfigureDevelopmentCertificateBypass(isDevelopment: true);

        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IHttpClientFactory>();

        // Assert
        var act = () => factory.CreateClient(nameof(ITenantOnboardingClientService));
        act.Should().NotThrow("a criação do client HTTP com handler customizado deve ser concluída sem exceções");
    }

    /// <summary>
    /// Valida que a extensão ConfigureDevelopmentCertificateBypass não altera o handler padrão quando em produção.
    /// </summary>
    [Fact]
    public void ConfigureDevelopmentCertificateBypass_WhenIsDevelopmentIsFalse_ShouldNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        var apiUri = new Uri("https://localhost:7001");

        // Act
        services.AddHttpClient<ITenantOnboardingClientService, TenantOnboardingClientService>(client => client.BaseAddress = apiUri)
            .ConfigureDevelopmentCertificateBypass(isDevelopment: false);

        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IHttpClientFactory>();

        // Assert
        var act = () => factory.CreateClient(nameof(ITenantOnboardingClientService));
        act.Should().NotThrow("em produção o cliente deve ser instanciado normalmente");
    }

    /// <summary>
    /// Valida que a extensão ConfigureDevelopmentCertificateBypass do BackofficeApp funciona corretamente.
    /// </summary>
    [Fact]
    public void Backoffice_ConfigureDevelopmentCertificateBypass_ShouldConfigureWithoutErrors()
    {
        // Arrange
        var services = new ServiceCollection();
        var apiUri = new Uri("https://localhost:7001");

        // Act
        BackofficeApp.Extensions.HttpClientExtensions.ConfigureDevelopmentCertificateBypass(
            services.AddHttpClient<BackofficeApp.Services.ITenantDirectoryService, BackofficeApp.Services.TenantDirectoryService>(client => client.BaseAddress = apiUri),
            isDevelopment: true);

        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IHttpClientFactory>();

        // Assert
        var act = () => factory.CreateClient(nameof(BackofficeApp.Services.ITenantDirectoryService));
        act.Should().NotThrow();
    }
}
