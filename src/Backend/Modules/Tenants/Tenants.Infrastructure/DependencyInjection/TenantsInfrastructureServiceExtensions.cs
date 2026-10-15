using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tenants.Application.Auth.Services;
using Tenants.Infrastructure.Auth;
using Microsoft.AspNetCore.Http;

namespace Tenants.Infrastructure.DependencyInjection;

/// <summary>
/// Métodos de extensão de injeção de dependência para registro dos serviços e configurações do módulo Tenants.Infrastructure.
/// </summary>
public static class TenantsInfrastructureServiceExtensions
{
    /// <summary>
    /// Registra os serviços de infraestrutura de inquilinos, incluindo autenticação e gerador de JWT.
    /// </summary>
    /// <param name="services">A coleção de serviços da aplicação.</param>
    /// <param name="configuration">Instância de configuração para binding de opções.</param>
    /// <returns>A coleção de serviços para encadeamento fluente.</returns>
    public static IServiceCollection AddTenantsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<TenantJwtOptions>(options =>
        {
            configuration.GetSection(TenantJwtOptions.SectionName).Bind(options);
        });

        services.AddAuthentication("Tenant")
            .AddJwtBearer("Tenant", options =>
            {
                var jwt = new TenantJwtOptions();
                configuration.GetSection(TenantJwtOptions.SectionName).Bind(jwt);
                // Sem configuração não há token válido; não usar uma chave padrão conhecida.
                var key = string.IsNullOrWhiteSpace(jwt.SecretKey)
                    ? System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)
                    : System.Text.Encoding.UTF8.GetBytes(jwt.SecretKey);
                options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(key),
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    ValidAlgorithms = [Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256],
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
                options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
                {
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();
                        context.Response.StatusCode = 401;
                        await context.Response.WriteAsJsonAsync(BuildingBlocks.Domain.Primitives.Result.Failure(
                            BuildingBlocks.Domain.Primitives.Error.Unauthorized("Auth.Unauthorized", "Autenticação válida necessária.")), context.HttpContext.RequestAborted);
                    }
                };
            });
        services.AddAuthorization();

        services.AddScoped<ITenantAuthService, TenantAuthService>();
        services.AddScoped<ITenantTokenService, TenantJwtTokenService>();
        services.AddScoped<Tenants.Application.Workspaces.Repositories.IWorkspaceRepository, Tenants.Infrastructure.Workspaces.WorkspaceRepository>();
        services.AddScoped<Tenants.Application.Squads.Repositories.ISquadRepository, Tenants.Infrastructure.Squads.SquadRepository>();
        services.AddScoped<Tenants.Application.Users.Repositories.ITenantUserRepository, Tenants.Infrastructure.Users.TenantUserRepository>();
        services.AddScoped<Tenants.Application.Squads.Services.IUserPortfolioService, Tenants.Infrastructure.Squads.UserPortfolioService>();
        services.AddScoped<Tenants.Application.Integrations.Repositories.IConnectedAdAccountRepository, Tenants.Infrastructure.Integrations.ConnectedAdAccountRepository>();
        services.AddScoped<Tenants.Application.Audit.Repositories.ITenantAuditLogRepository, Tenants.Infrastructure.Audit.TenantAuditLogRepository>();
        services.AddScoped<Tenants.Application.Rbac.Services.IPermissionEvaluator, Tenants.Infrastructure.Rbac.TenantPermissionEvaluator>();
        services.AddScoped<Tenants.Application.Branding.Repositories.ITenantBrandingRepository, Tenants.Infrastructure.Branding.TenantBrandingRepository>();
        services.AddScoped<Tenants.Application.Persistence.ITenantUnitOfWork, Tenants.Infrastructure.Persistence.TenantUnitOfWork>();

        return services;
    }
}
