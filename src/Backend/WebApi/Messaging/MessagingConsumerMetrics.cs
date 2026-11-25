using BuildingBlocks.Application.Messaging;

namespace WebApi.Messaging;

/// <summary>
/// Contadores OpenTelemetry de consumo de mensagens de integração.
/// </summary>
public static class MessagingConsumerMetrics
{
    private static readonly System.Diagnostics.Metrics.Meter Meter = new("AdMetricsPro.Messaging");
    private static readonly System.Diagnostics.Metrics.Counter<long> Acknowledged =
        Meter.CreateCounter<long>("admetrics.messaging.acknowledged");
    private static readonly System.Diagnostics.Metrics.Counter<long> Rescheduled =
        Meter.CreateCounter<long>("admetrics.messaging.rescheduled");
    private static readonly System.Diagnostics.Metrics.Counter<long> DeadLetter =
        Meter.CreateCounter<long>("admetrics.messaging.dead_letter");

    /// <summary>
    /// Registra a disposição aplicada após processamento do consumidor.
    /// </summary>
    /// <param name="action">Ação decidida pela política de ack.</param>
    /// <param name="messageType">Tipo versionado opcional para dimensionalização.</param>
    public static void RecordDisposition(IntegrationMessageDispositionAction action, string? messageType = null)
    {
        switch (action)
        {
            case IntegrationMessageDispositionAction.Acknowledge:
                if (messageType is null)
                {
                    Acknowledged.Add(1);
                }
                else
                {
                    Acknowledged.Add(1, new KeyValuePair<string, object?>("message-type", messageType));
                }

                break;
            case IntegrationMessageDispositionAction.Reschedule:
                if (messageType is null)
                {
                    Rescheduled.Add(1);
                }
                else
                {
                    Rescheduled.Add(1, new KeyValuePair<string, object?>("message-type", messageType));
                }

                break;
            default:
                break;
        }
    }

    /// <summary>
    /// Incrementa contador de dead-letter explícito na outbox.
    /// </summary>
    /// <param name="messageType">Tipo versionado opcional.</param>
    public static void RecordDeadLetter(string? messageType = null)
    {
        if (messageType is null)
        {
            DeadLetter.Add(1);
        }
        else
        {
            DeadLetter.Add(1, new KeyValuePair<string, object?>("message-type", messageType));
        }
    }
}