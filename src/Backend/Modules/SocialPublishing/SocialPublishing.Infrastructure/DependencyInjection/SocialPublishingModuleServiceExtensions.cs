using System.Reflection;
using BuildingBlocks.Application.DependencyInjection;
using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Application.Options;
using Amazon.Runtime;
using Amazon.S3;
using SocialPublishing.Infrastructure.Ai;
using SocialPublishing.Infrastructure.Media;
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

        services.TryAddSingleton(TimeProvider.System);

        services.AddScoped<SocialPublishingDbContextFactory>();
        services.AddScoped<ITenantDbContextFactory<SocialPublishingDbContext>>(sp =>
            sp.GetRequiredService<SocialPublishingDbContextFactory>());
        services.AddScoped<SocialPublishingDbContextAccessor>();
        services.AddScoped<IOrganicConnectionRepository, OrganicConnectionRepository>();
        services.AddScoped<IOrganicPostRepository, OrganicPostRepository>();
        services.AddScoped<IOrganicMediaAssetRepository, OrganicMediaAssetRepository>();
        services.AddScoped<IBrandVoiceRepository, BrandVoiceRepository>();
        services.AddScoped<IEditorialPlanGenerationRepository, EditorialPlanGenerationRepository>();
        services.AddScoped<IPublicationJobRepository, PublicationJobRepository>();
        services.AddScoped<ISocialPublishingUnitOfWork, SocialPublishingUnitOfWork>();
        services.AddScoped<ISocialPublishingSchemaMigrator, SocialPublishingSchemaMigrator>();

        ConfigureOrganicOAuth(services, configuration);
        ConfigureOrganicMedia(services, configuration);
        ConfigureSocialAi(services, configuration);

        return services;
    }

    private static void ConfigureSocialAi(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SocialAiOptions>(options =>
        {
            configuration.GetSection(SocialAiOptions.SectionName).Bind(options);
            options.ApiKey = configuration["SOCIAL_AI_API_KEY"] ?? options.ApiKey;
            options.BaseUrl = configuration["SOCIAL_AI_BASE_URL"] ?? options.BaseUrl;
            options.Model = configuration["SOCIAL_AI_MODEL"] ?? options.Model;
        });

        services.AddHttpClient<ISocialCreativeGenerator, HttpSocialCreativeGenerator>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<SocialAiOptions>>().Value;
            if (!string.IsNullOrWhiteSpace(options.BaseUrl))
            {
                client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            }

            if (options.TimeoutSeconds > 0)
            {
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            }
        }).RemoveAllLoggers();
    }

    private static void ConfigureOrganicMedia(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OrganicMediaOptions>(configuration.GetSection(OrganicMediaOptions.SectionName));

        services.AddSingleton<IOrganicMediaMetadataReader, OrganicMediaHeaderInspector>();

        var provider = configuration[$"{OrganicMediaOptions.SectionName}:Provider"] ?? "Local";
        if (string.Equals(provider, "S3", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IAmazonS3>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<OrganicMediaOptions>>().Value;
                var config = new AmazonS3Config();
                if (!string.IsNullOrWhiteSpace(options.ServiceUrl))
                {
                    config.ServiceURL = options.ServiceUrl;
                    config.ForcePathStyle = true;
                }

                var accessKey = configuration["AWS_ACCESS_KEY_ID"];
                var secretKey = configuration["AWS_SECRET_ACCESS_KEY"];
                if (!string.IsNullOrWhiteSpace(accessKey) && !string.IsNullOrWhiteSpace(secretKey))
                {
                    return new AmazonS3Client(new BasicAWSCredentials(accessKey, secretKey), config);
                }

                return new AmazonS3Client(config);
            });
            services.AddSingleton<IOrganicMediaStore, S3OrganicMediaStore>();
        }
        else
        {
            services.AddSingleton<IOrganicMediaStore, LocalOrganicMediaStore>();
        }
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
