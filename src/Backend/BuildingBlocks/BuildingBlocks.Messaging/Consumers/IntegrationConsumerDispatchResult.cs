using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;

namespace BuildingBlocks.Messaging.Consumers;

/// <summary>
/// Resultado do despacho MediatR no consumidor, incluindo a política de ack.
/// </summary>
public sealed record IntegrationConsumerDispatchResult
{
    private IntegrationConsumerDispatchResult(Result result, IntegrationResultDisposition disposition)
    {
        Result = result;
        Disposition = disposition;
    }

    /// <summary>
    /// Obtém o resultado do comando MediatR.
    /// </summary>
    public Result Result { get; }

    /// <summary>
    /// Obtém a disposição recomendada para a mensagem AMQP.
    /// </summary>
    public IntegrationResultDisposition Disposition { get; }

    /// <summary>
    /// Cria o resultado combinado.
    /// </summary>
    /// <param name="result">Resultado do comando.</param>
    /// <param name="disposition">Política de ack.</param>
    /// <returns>Instância imutável.</returns>
    public static IntegrationConsumerDispatchResult Create(Result result, IntegrationResultDisposition disposition) =>
        new(result, disposition);
}