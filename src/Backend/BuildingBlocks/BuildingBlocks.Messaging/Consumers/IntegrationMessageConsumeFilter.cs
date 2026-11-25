using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Messaging.Contracts;
using MassTransit;

namespace BuildingBlocks.Messaging.Consumers;

/// <summary>
/// Rejeita mensagens com envelope inválido antes de invocar consumidores MediatR.
/// </summary>
/// <typeparam name="T">Tipo da mensagem de transporte.</typeparam>
public sealed class IntegrationMessageConsumeFilter<T> : IFilter<ConsumeContext<T>>
    where T : class
{
    /// <inheritdoc />
    public void Probe(ProbeContext context) => context.CreateFilterScope("integration-envelope-guard");

    /// <inheritdoc />
    public async Task Send(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
    {
        if (context.Message is IntegrationEventTransportMessage transport)
        {
            var envelopeResult = IntegrationEventEnvelopeSerializer.Deserialize(transport.Json);
            if (envelopeResult.IsFailure)
            {
                throw new IntegrationMessageRejectedException(MessagingErrors.PayloadInvalid.Code);
            }

            var envelope = envelopeResult.Value;
            if (envelope.Scope == IntegrationEventScope.Operational && !envelope.TenantId.HasValue)
            {
                throw new IntegrationMessageRejectedException(MessagingErrors.TenantRequired.Code);
            }
        }

        await next.Send(context);
    }
}

/// <summary>
/// Exceção usada para mover mensagem inválida ao dead-letter após retries.
/// </summary>
public sealed class IntegrationMessageRejectedException : Exception
{
    /// <summary>
    /// Inicializa com código de erro de negócio.
    /// </summary>
    /// <param name="errorCode">Código estável.</param>
    public IntegrationMessageRejectedException(string errorCode)
        : base(errorCode)
    {
        ErrorCode = errorCode;
    }

    /// <summary>
    /// Obtém o código de erro.
    /// </summary>
    public string ErrorCode { get; }
}