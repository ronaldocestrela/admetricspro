using Integrations.Application.Options;
using Integrations.Application.Persistence;
using Integrations.Domain.OAuth;
using Integrations.Infrastructure.OAuth;
using Integrations.Infrastructure.OAuth.Adapters;
using Integrations.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Integrations.Infrastructure.DependencyInjection;

/// <summary>
/// Métodos de extensão para registro de adaptadores de rede, Token Vault e serviços do Integrations.Infrastructure.
/// </summary>
public static class IntegrationsInfrastructureServiceExtensions
{
    /// <summary>
    /// Registra os adaptadores OAuth2, clientes HTTP tipados, repositórios e serviços de criptografia do módulo.
    /// </summary>
    /// <param name="services">Coleção de serviços.</param>
    /// <param name="configuration">Configuração da aplicação.</param>
    /// <returns>A coleção de serviços para encadeamento fluente.</returns>
    public static IServiceCollection AddIntegrationsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Opções de rede e OAuth
        services.Configure<OAuthNetworkOptions>(options =>
        {
            configuration.GetSection(OAuthNetworkOptions.SectionName).Bind(options);
        });

        services.Configure<MetaAdsOAuthOptions>(options =>
        {
            configuration.GetSection($"{OAuthNetworkOptions.SectionName}:Meta").Bind(options);
        });

        services.Configure<GoogleAdsOAuthOptions>(options =>
        {
            configuration.GetSection($"{OAuthNetworkOptions.SectionName}:Google").Bind(options);
        });

        services.Configure<BingAdsOAuthOptions>(options =>
        {
            configuration.GetSection($"{OAuthNetworkOptions.SectionName}:Bing").Bind(options);
        });

        services.Configure<TikTokAdsOAuthOptions>(options =>
        {
            configuration.GetSection($"{OAuthNetworkOptions.SectionName}:TikTok").Bind(options);
        });

        // Serviço de estado anti-CSRF com fallback de chave se não configurada
        var signingKey = configuration[$"{OAuthNetworkOptions.SectionName}:StateSigningKey"]
            ?? "default-admetricspro-oauth-state-signing-key-32chars!";
        services.AddSingleton<IOAuthStateService>(_ => new OAuthStateService(signingKey));

        // Adaptadores HTTP tipados
        services.AddHttpClient<MetaAdsOAuthAdapter>();
        services.AddHttpClient<GoogleAdsOAuthAdapter>();
        services.AddHttpClient<BingAdsOAuthAdapter>();
        services.AddHttpClient<TikTokAdsOAuthAdapter>();

        // Registro de adaptadores na coleção de IOAuthAdapter
        services.AddScoped<IOAuthAdapter>(sp => sp.GetRequiredService<MetaAdsOAuthAdapter>());
        services.AddScoped<IOAuthAdapter>(sp => sp.GetRequiredService<GoogleAdsOAuthAdapter>());
        services.AddScoped<IOAuthAdapter>(sp => sp.GetRequiredService<BingAdsOAuthAdapter>());
        services.AddScoped<IOAuthAdapter>(sp => sp.GetRequiredService<TikTokAdsOAuthAdapter>());

        // Orquestrador de autenticação unificado
        services.AddScoped<IAdNetworkAuthService, AdNetworkAuthService>();

        // Serviço de criptografia simétrica AES-256 do Token Vault
        services.AddScoped<IOAuthEncryptionService, OAuthEncryptionService>();

        // Repositório do Token Vault e Unit of Work operacional
        services.AddScoped<IOAuthTokenVaultRepository, OAuthTokenVaultRepository>();
        services.AddScoped<IIntegrationsUnitOfWork, IntegrationsUnitOfWork>();

        // Mappers de hierarquia estrutural universal
        services.AddSingleton<Integrations.Domain.Campaigns.Mappers.IMetaAdsHierarchyMapper, Integrations.Infrastructure.Campaigns.Mappers.MetaAdsHierarchyMapper>();
        services.AddSingleton<Integrations.Domain.Campaigns.Mappers.IGoogleAdsHierarchyMapper, Integrations.Infrastructure.Campaigns.Mappers.GoogleAdsHierarchyMapper>();
        services.AddSingleton<Integrations.Domain.Campaigns.Mappers.ITikTokAdsHierarchyMapper, Integrations.Infrastructure.Campaigns.Mappers.TikTokAdsHierarchyMapper>();
        services.AddSingleton<Integrations.Domain.Campaigns.Mappers.IBingAdsHierarchyMapper, Integrations.Infrastructure.Campaigns.Mappers.BingAdsHierarchyMapper>();

        // Política de resiliência e controle de limite de taxa
        services.AddSingleton<Integrations.Domain.Campaigns.Resilience.IHierarchyRateLimitPolicy, Integrations.Infrastructure.Campaigns.Resilience.HierarchyRateLimitPolicy>();

        // Adaptadores de sincronização paginada
        services.AddHttpClient<Integrations.Infrastructure.Campaigns.Sync.MetaAdsHierarchySyncAdapter>();
        services.AddHttpClient<Integrations.Infrastructure.Campaigns.Sync.GoogleAdsHierarchySyncAdapter>();
        services.AddHttpClient<Integrations.Infrastructure.Campaigns.Sync.TikTokAdsHierarchySyncAdapter>();
        services.AddHttpClient<Integrations.Infrastructure.Campaigns.Sync.BingAdsHierarchySyncAdapter>();
        services.AddScoped<Integrations.Infrastructure.Campaigns.Sync.DemoHierarchySyncAdapter>();

        services.AddScoped<Integrations.Domain.Campaigns.Sync.ICampaignHierarchySyncAdapter>(sp => sp.GetRequiredService<Integrations.Infrastructure.Campaigns.Sync.DemoHierarchySyncAdapter>());
        services.AddScoped<Integrations.Domain.Campaigns.Sync.ICampaignHierarchySyncAdapter>(sp => sp.GetRequiredService<Integrations.Infrastructure.Campaigns.Sync.MetaAdsHierarchySyncAdapter>());
        services.AddScoped<Integrations.Domain.Campaigns.Sync.ICampaignHierarchySyncAdapter>(sp => sp.GetRequiredService<Integrations.Infrastructure.Campaigns.Sync.GoogleAdsHierarchySyncAdapter>());
        services.AddScoped<Integrations.Domain.Campaigns.Sync.ICampaignHierarchySyncAdapter>(sp => sp.GetRequiredService<Integrations.Infrastructure.Campaigns.Sync.TikTokAdsHierarchySyncAdapter>());
        services.AddScoped<Integrations.Domain.Campaigns.Sync.ICampaignHierarchySyncAdapter>(sp => sp.GetRequiredService<Integrations.Infrastructure.Campaigns.Sync.BingAdsHierarchySyncAdapter>());

        // Despachante central de sincronização
        services.AddScoped<Integrations.Domain.Campaigns.Sync.ICampaignHierarchySyncDispatcher, Integrations.Infrastructure.Campaigns.Sync.CampaignHierarchySyncDispatcher>();

        // Repositório de persistência da hierarquia de campanhas
        services.AddScoped<Integrations.Domain.Campaigns.ICampaignHierarchyRepository, Integrations.Infrastructure.Campaigns.Persistence.CampaignHierarchyRepository>();

        // Repositório de persistência de métricas analíticas de campanhas (idempotente)
        services.AddScoped<Integrations.Domain.Campaigns.ICampaignMetricsRepository, Integrations.Infrastructure.Campaigns.Persistence.CampaignMetricsRepository>();

        // Adaptadores de ingestão de métricas
        services.AddHttpClient<Integrations.Infrastructure.Campaigns.Sync.MetaAdsMetricsSyncAdapter>();
        services.AddHttpClient<Integrations.Infrastructure.Campaigns.Sync.GoogleAdsMetricsSyncAdapter>();
        services.AddHttpClient<Integrations.Infrastructure.Campaigns.Sync.TikTokAdsMetricsSyncAdapter>();
        services.AddHttpClient<Integrations.Infrastructure.Campaigns.Sync.BingAdsMetricsSyncAdapter>();
        services.AddScoped<Integrations.Infrastructure.Campaigns.Sync.DemoMetricsSyncAdapter>();

        services.AddScoped<Integrations.Domain.Campaigns.Sync.ICampaignMetricsSyncAdapter>(sp => sp.GetRequiredService<Integrations.Infrastructure.Campaigns.Sync.DemoMetricsSyncAdapter>());
        services.AddScoped<Integrations.Domain.Campaigns.Sync.ICampaignMetricsSyncAdapter>(sp => sp.GetRequiredService<Integrations.Infrastructure.Campaigns.Sync.MetaAdsMetricsSyncAdapter>());
        services.AddScoped<Integrations.Domain.Campaigns.Sync.ICampaignMetricsSyncAdapter>(sp => sp.GetRequiredService<Integrations.Infrastructure.Campaigns.Sync.GoogleAdsMetricsSyncAdapter>());
        services.AddScoped<Integrations.Domain.Campaigns.Sync.ICampaignMetricsSyncAdapter>(sp => sp.GetRequiredService<Integrations.Infrastructure.Campaigns.Sync.TikTokAdsMetricsSyncAdapter>());
        services.AddScoped<Integrations.Domain.Campaigns.Sync.ICampaignMetricsSyncAdapter>(sp => sp.GetRequiredService<Integrations.Infrastructure.Campaigns.Sync.BingAdsMetricsSyncAdapter>());

        // Despachante central de métricas
        services.AddScoped<Integrations.Domain.Campaigns.Sync.ICampaignMetricsSyncDispatcher, Integrations.Infrastructure.Campaigns.Sync.CampaignMetricsSyncDispatcher>();

        return services;
    }
}
