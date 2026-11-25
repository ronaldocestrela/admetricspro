using BuildingBlocks.Application.Messaging.Persistence;
using BuildingBlocks.Infrastructure.Persistence;
using BuildingBlocks.Infrastructure.Persistence.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Infrastructure.DependencyInjection;

/// <summary>
/// Registro de persistência transacional de mensageria por escopo de <see cref="DbContext"/>.
/// </summary>
public static class MessagingPersistenceServiceCollectionExtensions
{
    /// <summary>
    /// Registra stores de outbox/inbox para o contexto operacional do tenant (resolvido por escopo).
    /// </summary>
    /// <param name="services">Coleção de serviços.</param>
    /// <returns>Coleção para encadeamento.</returns>
    public static IServiceCollection AddTenantMessagingStores(this IServiceCollection services)
    {
        services.AddScoped<IIntegrationOutboxStore, TenantAwareIntegrationOutboxStore>();
        services.AddScoped<IIntegrationOutboxWriter, TenantAwareIntegrationOutboxWriter>();
        services.AddScoped<IIntegrationInboxStore, TenantAwareIntegrationInboxStore>();
        services.AddScoped<IUserInitiatedJobStore, TenantAwareUserInitiatedJobStore>();
        services.AddScoped<ITenantUserInitiatedJobCommitter, TenantUserInitiatedJobCommitter>();
        return services;
    }

    /// <summary>
    /// Registra stores de outbox/inbox e lease no contexto do catálogo Master.
    /// </summary>
    /// <typeparam name="TMasterContext">Tipo do DbContext do Master (deve expor sets de mensageria).</typeparam>
    /// <param name="services">Coleção de serviços.</param>
    /// <returns>Coleção para encadeamento.</returns>
    public static IServiceCollection AddMasterMessagingStores<TMasterContext>(this IServiceCollection services)
        where TMasterContext : DbContext
    {
        services.AddScoped<MasterOutboxDispatchStore>(sp =>
            new MasterOutboxDispatchStore(new DbIntegrationOutboxStore(sp.GetRequiredService<TMasterContext>())));
        services.AddScoped<ICatalogIntegrationOutboxWriter>(sp =>
            new DbIntegrationOutboxWriter(sp.GetRequiredService<TMasterContext>()));
        services.AddScoped<ICatalogIntegrationOutboxStore>(sp =>
            new DbCatalogIntegrationOutboxStore(sp.GetRequiredService<TMasterContext>()));
        services.AddScoped<IIntegrationOutboxWriter>(sp =>
            new DbIntegrationOutboxWriter(sp.GetRequiredService<TMasterContext>()));
        services.AddScoped<MasterIntegrationInboxStore>(sp =>
            new MasterIntegrationInboxStore(new DbIntegrationInboxStore(sp.GetRequiredService<TMasterContext>())));
        services.AddScoped<IMessagingSchedulerLeaseStore>(sp =>
            new DbMessagingSchedulerLeaseStore(sp.GetRequiredService<TMasterContext>()));
        return services;
    }
}