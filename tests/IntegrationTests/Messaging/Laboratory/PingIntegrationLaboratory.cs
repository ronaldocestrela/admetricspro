using System.Text.Json;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Messaging.Persistence;
using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Domain.Messaging;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Infrastructure.MultiTenancy;
using BuildingBlocks.Messaging.Consumers;
using BuildingBlocks.Messaging.Contracts;
using MassTransit;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationTests.Messaging.Laboratory;

/// <summary>
/// Contrato de laboratório <c>messaging.ping.v1</c>.
/// </summary>
public sealed class PingIntegrationMessageConsumer(IServiceScopeFactory scopeFactory) : IConsumer<IntegrationEventTransportMessage>
{
    /// <summary>
    /// Nome estável do consumidor para inbox.
    /// </summary>
    public const string ConsumerName = "messaging.ping";

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<IntegrationEventTransportMessage> context)
    {
        if (context.Message.MessageType != "messaging.ping.v1")
        {
            return;
        }

        var envelopeResult = IntegrationEventEnvelopeSerializer.Deserialize(context.Message.Json);
        if (envelopeResult.IsFailure)
        {
            throw new IntegrationMessageRejectedException(envelopeResult.Error.Code);
        }

        var envelope = envelopeResult.Value;
        using var scope = scopeFactory.CreateScope();

        if (envelope.Scope == IntegrationEventScope.Operational)
        {
            if (!envelope.TenantId.HasValue)
            {
                throw new IntegrationMessageRejectedException(MessagingErrors.TenantRequired.Code);
            }

            scope.ServiceProvider.GetRequiredService<ITenantContextAccessor>().TenantContext =
                TenantContext.Create(envelope.TenantId, subdomain: null, TenantResolutionSource.None);
        }

        var dispatcher = new IntegrationConsumerDispatcher(
            scope.ServiceProvider.GetRequiredService<ISender>(),
            scope.ServiceProvider.GetRequiredService<IIntegrationInboxStore>(),
            scope.ServiceProvider.GetRequiredService<ITenantContextAccessor>(),
            ConsumerName);

        var result = await dispatcher.DispatchAsync(
            context.Message,
            env => new PingIntegrationCommand(
                env.EventId,
                env.Payload.GetProperty("pingId").GetGuid(),
                DateTime.UtcNow),
            context.CancellationToken);

        if (result.IsFailure && result.Error.Code is "Messaging.PayloadInvalid" or "Messaging.TenantRequired")
        {
            throw new IntegrationMessageRejectedException(result.Error.Code);
        }
    }
}

/// <summary>
/// Comando MediatR de laboratório.
/// </summary>
/// <param name="EventId">Identificador do evento.</param>
/// <param name="PingId">Valor de correlação no payload.</param>
/// <param name="ProcessedAtUtc">Instante de processamento.</param>
public sealed record PingIntegrationCommand(Guid EventId, Guid PingId, DateTime ProcessedAtUtc) : IRequest<Result>;

/// <summary>
/// Handler que registra efeito colateral em store compartilhado.
/// </summary>
public sealed class PingIntegrationCommandHandler(IPingIntegrationEffectStore effectStore)
    : IRequestHandler<PingIntegrationCommand, Result>
{
    /// <inheritdoc />
    public Task<Result> Handle(PingIntegrationCommand request, CancellationToken cancellationToken)
    {
        effectStore.Record(request.EventId);
        return Task.FromResult(Result.Success());
    }
}

/// <summary>
/// Store de efeito colateral para asserts de idempotência.
/// </summary>
public interface IPingIntegrationEffectStore
{
    /// <summary>
    /// Registra processamento do evento.
    /// </summary>
    /// <param name="eventId">Identificador do evento.</param>
    void Record(Guid eventId);

    /// <summary>
    /// Conta processamentos.
    /// </summary>
    /// <param name="eventId">Identificador do evento.</param>
    /// <returns>Contagem.</returns>
    int GetCount(Guid eventId);
}

/// <summary>
/// Implementação em memória do store de efeito ping.
/// </summary>
public sealed class InMemoryPingIntegrationEffectStore : IPingIntegrationEffectStore
{
    private readonly Dictionary<Guid, int> _counts = new();

    /// <inheritdoc />
    public void Record(Guid eventId)
    {
        lock (_counts)
        {
            _counts.TryGetValue(eventId, out var count);
            _counts[eventId] = count + 1;
        }
    }

    /// <inheritdoc />
    public int GetCount(Guid eventId)
    {
        lock (_counts)
        {
            return _counts.TryGetValue(eventId, out var count) ? count : 0;
        }
    }
}

/// <summary>
/// Utilitários para montar envelope e linha de outbox de ping.
/// </summary>
public static class PingOutboxFactory
{
    /// <summary>
    /// Cria JSON de envelope operacional de ping.
    /// </summary>
    /// <param name="eventId">EventId.</param>
    /// <param name="tenantId">Tenant operacional.</param>
    /// <param name="pingId">Valor no payload.</param>
    /// <returns>JSON serializado.</returns>
    public static string BuildEnvelopeJson(Guid eventId, Guid tenantId, Guid pingId)
    {
        using var payload = JsonDocument.Parse($$"""{"pingId":"{{pingId}}"}""");
        var envelope = IntegrationEventEnvelope.Create(
            eventId,
            "messaging.ping.v1",
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            IntegrationEventScope.Operational,
            tenantId,
            payload.RootElement.Clone()).Value;

        return IntegrationEventEnvelopeSerializer.Serialize(envelope);
    }

    /// <summary>
    /// Cria linha de outbox pendente a partir do envelope.
    /// </summary>
    /// <param name="eventId">EventId.</param>
    /// <param name="tenantId">Tenant.</param>
    /// <param name="pingId">Ping.</param>
    /// <returns>Linha de outbox.</returns>
    public static IntegrationOutboxMessage CreateOutboxLine(Guid eventId, Guid tenantId, Guid pingId) =>
        IntegrationOutboxMessage.Create(
            Guid.NewGuid(),
            eventId,
            "messaging.ping.v1",
            BuildEnvelopeJson(eventId, tenantId, pingId),
            tenantId,
            DateTime.UtcNow).Value;
}