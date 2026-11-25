using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Messaging.Scheduling;
using BuildingBlocks.Infrastructure.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Infrastructure.DependencyInjection;

/// <summary>
/// Registro de liderança compartilhada para laços temporais de mensageria.
/// </summary>
public static class MessagingSchedulerLeadershipServiceCollectionExtensions
{
    /// <summary>
    /// Adiciona identificador de instância e <see cref="IMessagingSchedulerLeader"/>.
    /// </summary>
    /// <param name="services">Coleção de serviços.</param>
    /// <param name="configuration">Configuração do host.</param>
    /// <returns>Coleção para encadeamento.</returns>
    public static IServiceCollection AddMessagingSchedulerLeadership(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<MessagingHostInstanceId>();
        services.AddScoped<IMessagingSchedulerLeader, MessagingSchedulerLeader>();
        services.AddOptions<MessagingDispatchLeaseOptions>()
            .BindConfiguration(MessagingDispatchLeaseOptions.SectionName);
        return services;
    }
}