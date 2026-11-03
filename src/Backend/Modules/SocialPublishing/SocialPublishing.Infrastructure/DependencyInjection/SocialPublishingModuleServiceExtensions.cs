using System.Reflection;
using BuildingBlocks.Application.DependencyInjection;
using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Application.Options;
using SocialPublishing.Infrastructure.OAuth;
using SocialPublishing.Infrastructure.Persistence;
using SocialPublishing.Infrastructure.Persistence.Repositories;
using SocialPublishing.Infrastructure.Security;

namespace SocialPublishing.Infrastructure.DependencyInjection;

/// <summary>
/// Registro de composição do bounded context SocialPublishing na raiz WebApi.
/// </summary>
public static class SocialPublishingModuleServiceExtensions
{
    /// <summary>
    /// Registra MediatR, validadores FluentValidation e behaviors do módulo SocialPublishing.
    /// </summary>
    /// <param name="services">Coleção de serviços da aplicação.</param>
    /// <param name="configuration">Configuração da aplicação (reservada para opções futuras do módulo).</param>
    /// <returns>A coleção de serviços para encadeamento fluente.</returns>
    public static IServiceCollection AddSocialPublishingModule(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddMessaging(Assembly.Load(new AssemblyName("SocialPublishing.Application")));

        services.AddScoped<SocialPublishingDbContextFactory>();
        services.AddScoped<ITenantDbContextFactory<SocialPublishingDbContext>>(sp =>
            sp.GetRequiredService<SocialPublishingDbContextFactory>());
        services.AddScoped<SocialPublishingDbContextAccessor>();
        services.AddScoped<IOrganicConnectionRepository, OrganicConnectionRepository>();
        services.AddScoped<IOrganicPostRepository, OrganicPostRepository>();
        services.AddScoped<IBrandVoiceRepository, BrandVoiceRepository>();
        services.AddScoped<ISocialPublishingUnitOfWork, SocialPublishingUnitOfWork>();
        services.AddScoped<ISocialPublishingSchemaMigrator, SocialPublishingSchemaMigrator>();

        ConfigureOrganicOAuth(services, configuration);

        return services;
    }

    private static void ConfigureOrganicOAuth(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OrganicOAuthOptions>(options =>
        {
            configuration.GetSection(OrganicOAuthOptions.SectionName).Bind(options);
            options.Meta.AppId = configuration["META_APP_ID"] ?? options.Meta.AppId;
            options.Meta.AppSecret = configuration["META_APP_SECRET"] ?? options.Meta.AppSecret;
            options.Meta.GraphApiVersion = configuration["META_GRAPH_API_VERSION"] ?? options.Meta.GraphApiVersion;
        });

        services.AddOptions<OrganicOAuthOptions>()
            .Validate(
                o => !string.IsNullOrWhiteSpace(o.StateSigningKey) &&
                     o.StateSigningKey.Length >= 32 &&
                     !o.StateSigningKey.StartsWith("default", StringComparison.OrdinalIgnoreCase),
                "Configure SocialPublishing:OAuth:StateSigningKey com pelo menos 32 caracteres aleatórios.")
            .ValidateOnStart();

        services.AddSingleton<IOrganicOAuthStateService>(sp =>
        {
            var key = sp.GetRequiredService<IOptions<OrganicOAuthOptions>>().Value.StateSigningKey;
            return new OrganicOAuthStateService(key);
        });

        services.AddHttpClient<MetaInstagramOrganicAdapter>().RemoveAllLoggers();
        services.AddHttpClient<MetaFacebookPageOrganicAdapter>().RemoveAllLoggers();
        services.AddHttpClient<TikTokContentPostingAdapter>().RemoveAllLoggers();

        services.AddTransient<IOrganicPlatformAdapter>(sp => sp.GetRequiredService<MetaInstagramOrganicAdapter>());
        services.AddTransient<IOrganicPlatformAdapter>(sp => sp.GetRequiredService<MetaFacebookPageOrganicAdapter>());
        services.AddTransient<IOrganicPlatformAdapter>(sp => sp.GetRequiredService<TikTokContentPostingAdapter>());

        services.AddScoped<IOrganicTokenProtector, OrganicTokenProtector>();
    }
}
