using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Messaging.Transport;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Messaging.Configuration;
using BuildingBlocks.Messaging.Transport;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Messaging.DependencyInjection;

/// <summary>
/// Registro do transporte RabbitMQ + MassTransit na borda de infraestrutura.
/// </summary>
public static class AdMetricsMessagingExtensions
{
    /// <summary>
    /// Adiciona MassTransit e publicador quando <c>Messaging:Enabled</c> é verdadeiro.
    /// </summary>
    /// <param name="services">Coleção de serviços.</param>
    /// <param name="configuration">Configuração do host.</param>
    /// <param name="registrationMode">Modo de registro conforme o papel do host.</param>
    /// <param name="configureConsumers">Callback opcional para consumidores de laboratório/teste.</param>
    /// <param name="configureRabbit">Callback opcional para ajustes finos do host RabbitMQ.</param>
    /// <returns>Coleção para encadeamento.</returns>
    public static IServiceCollection AddAdMetricsMessaging(
        this IServiceCollection services,
        IConfiguration configuration,
        AdMetricsMessagingRegistrationMode registrationMode,
        Action<IRegistrationConfigurator>? configureConsumers = null,
        Action<IBusRegistrationContext, IRabbitMqBusFactoryConfigurator>? configureRabbit = null)
    {
        if (registrationMode is AdMetricsMessagingRegistrationMode.None)
        {
            services.AddSingleton<IIntegrationEventPublisher, NoOpIntegrationEventPublisher>();
            return services;
        }

        var rabbit = configuration.GetSection(RabbitMqConnectionOptions.SectionName).Get<RabbitMqConnectionOptions>()
            ?? new RabbitMqConnectionOptions();

        var registerConsumers = registrationMode is AdMetricsMessagingRegistrationMode.ConsumersOnly
            or AdMetricsMessagingRegistrationMode.Full;
        var registerPublisher = registrationMode is AdMetricsMessagingRegistrationMode.PublisherOnly
            or AdMetricsMessagingRegistrationMode.Full;

        services.AddMassTransit(bus =>
        {
            if (registerConsumers)
            {
                configureConsumers?.Invoke(bus);
            }

            if (registerConsumers)
            {
                bus.AddConfigureEndpointsCallback((_, _, cfg) =>
                {
                    cfg.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(2)));
                    cfg.PrefetchCount = 4;
                });
            }

            bus.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(rabbit.Host, (ushort)rabbit.Port, rabbit.VirtualHost, h =>
                {
                    h.Username(rabbit.Username);
                    h.Password(rabbit.Password);
                });

                cfg.Message<Contracts.IntegrationEventTransportMessage>(m =>
                {
                    m.SetEntityName("admetricspro.integration");
                });

                cfg.Publish<Contracts.IntegrationEventTransportMessage>(p =>
                {
                    p.ExchangeType = "topic";
                });

                cfg.Send<Contracts.IntegrationEventTransportMessage>(s =>
                {
                    s.UseRoutingKeyFormatter(ctx => ctx.Message.MessageType);
                });

                if (registerConsumers && configureRabbit is not null)
                {
                    configureRabbit(context, cfg);
                }
            });
        });

        if (registerPublisher)
        {
            services.AddScoped<IIntegrationEventPublisher, IntegrationEventPublisher>();
        }
        else
        {
            services.AddSingleton<IIntegrationEventPublisher, NoOpIntegrationEventPublisher>();
        }

        return services;
    }
}

/// <summary>
/// Publicador nulo quando mensageria está desligada.
/// </summary>
internal sealed class NoOpIntegrationEventPublisher : IIntegrationEventPublisher
{
    /// <inheritdoc />
    public Task<Result> PublishAsync(
        BuildingBlocks.Domain.Messaging.IntegrationOutboxMessage message,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Result.Failure(MessagingErrors.Disabled));
}