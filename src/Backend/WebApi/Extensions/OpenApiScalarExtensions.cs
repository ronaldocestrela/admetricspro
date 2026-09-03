using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Scalar.AspNetCore;

namespace WebApi.Extensions;

/// <summary>
/// Métodos de extensão para configuração do OpenAPI nativo e interface Scalar UI.
/// </summary>
public static class OpenApiScalarExtensions
{
    /// <summary>
    /// Registra e configura a geração nativa de contratos OpenAPI v1.
    /// </summary>
    /// <param name="services">Coleção de serviços da aplicação.</param>
    /// <returns>A coleção de serviços configurada.</returns>
    public static IServiceCollection AddOpenApiDocumentation(this IServiceCollection services)
    {
        services.AddOpenApi("v1", options =>
        {
            options.AddDocumentTransformer((document, context, cancellationToken) =>
            {
                document.Info = new()
                {
                    Title = "AdMetricsPro API",
                    Version = "v1",
                    Description = "SaaS de Gestão Unificada de Tráfego Pago (Meta Ads, Google Ads, Bing Ads e TikTok Ads)."
                };
                return Task.CompletedTask;
            });
        });

        return services;
    }

    /// <summary>
    /// Mapeia os endpoints de OpenAPI e Scalar UI nos ambientes de desenvolvimento e homologação.
    /// </summary>
    /// <param name="app">Instância da aplicação Web.</param>
    /// <returns>A aplicação configurada.</returns>
    public static WebApplication UseOpenApiAndScalar(this WebApplication app)
    {
        if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Staging"))
        {
            app.MapOpenApi();
            app.MapScalarApiReference("/scalar/v1", options =>
            {
                options.WithTitle("AdMetricsPro API - Scalar Reference");
            });
        }

        return app;
    }
}
