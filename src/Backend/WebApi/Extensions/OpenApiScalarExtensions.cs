using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

namespace WebApi.Extensions;

/// <summary>
/// Métodos de extensão para configuração do OpenAPI nativo e interface Scalar UI.
/// </summary>
public static class OpenApiScalarExtensions
{
    /// <summary>
    /// Registra e configura a geração nativa de contratos OpenAPI v1 com suporte a segurança corporativa Bearer JWT.
    /// </summary>
    /// <param name="services">Coleção de serviços da aplicação.</param>
    /// <returns>A coleção de serviços configurada.</returns>
    public static IServiceCollection AddOpenApiDocumentation(this IServiceCollection services)
    {
        services.AddOpenApi("v1", options =>
        {
            options.AddDocumentTransformer((document, context, cancellationToken) =>
            {
                document.Info = new OpenApiInfo
                {
                    Title = "AdMetricsPro API",
                    Version = "v1",
                    Description = "SaaS de Gestão Unificada de Tráfego Pago (Meta Ads, Google Ads, Bing Ads e TikTok Ads) - Painel Administrativo do MasterDb e Operações de Tenants.",
                    Contact = new OpenApiContact
                    {
                        Name = "AdMetricsPro Suporte de Engenharia",
                        Email = "support@admetricspro.internal"
                    },
                    License = new OpenApiLicense
                    {
                        Name = "Proprietário",
                        Url = new Uri("https://admetricspro.internal/terms")
                    }
                };

                var bearerScheme = new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Description = "Autenticação corporativa JWT Bearer. Insira o token no formato: Bearer {seu_token}",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT"
                };

                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
                document.Components.SecuritySchemes["Bearer"] = bearerScheme;

                var securityRequirement = new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("Bearer", document)] = []
                };

                document.Security = [securityRequirement];

                return Task.CompletedTask;
            });
        });

        return services;
    }

    /// <summary>
    /// Mapeia os endpoints de OpenAPI e Scalar UI nos ambientes de desenvolvimento e homologação com suporte a autenticação corporativa.
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
                options.WithTitle("AdMetricsPro API - Scalar Reference")
                       .WithTheme(ScalarTheme.Moon)
                       .AddPreferredSecuritySchemes("Bearer");
            });
        }

        return app;
    }
}
