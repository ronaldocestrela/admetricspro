using BuildingBlocks.Domain.Primitives;

namespace BuildingBlocks.Application.Messaging;

/// <summary>
/// Papel operacional do processo WebApi para mensageria durável (Fase 10).
/// </summary>
public enum MessagingHostRole
{
    /// <summary>
    /// HTTP, gravação de outbox e migrações; sem broker nem laços temporais.
    /// </summary>
    Api,

    /// <summary>
    /// Dispatcher e laços temporais; publica no broker sem consumidores.
    /// </summary>
    Scheduler,

    /// <summary>
    /// Somente consumidores MassTransit; sem laços.
    /// </summary>
    Consumer,

    /// <summary>
    /// Instância única: HTTP, laços, dispatcher e consumidores.
    /// </summary>
    All,
}

/// <summary>
/// Converte valores de configuração em <see cref="MessagingHostRole"/>.
/// </summary>
public static class MessagingHostRoleParser
{
    /// <summary>
    /// Interpreta o texto de <c>Messaging:HostRole</c>.
    /// </summary>
    /// <param name="value">Valor da configuração (padrão <c>All</c> quando vazio).</param>
    /// <returns>Papel válido ou falha de validação.</returns>
    public static Result<MessagingHostRole> Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result<MessagingHostRole>.Success(MessagingHostRole.All);
        }

        return value.Trim() switch
        {
            "Api" => Result<MessagingHostRole>.Success(MessagingHostRole.Api),
            "Scheduler" => Result<MessagingHostRole>.Success(MessagingHostRole.Scheduler),
            "Consumer" => Result<MessagingHostRole>.Success(MessagingHostRole.Consumer),
            "All" => Result<MessagingHostRole>.Success(MessagingHostRole.All),
            _ => Result<MessagingHostRole>.Failure(
                Error.Validation(
                    "Messaging.InvalidHostRole",
                    "Messaging:HostRole deve ser Api, Scheduler, Consumer ou All.")),
        };
    }
}