using Microsoft.Extensions.Diagnostics.HealthChecks;
using RabbitMQ.Client;
using WebApi.Configuration;

namespace WebApi.Extensions;

/// <summary>
/// Registra o health check nomeado <c>rabbitmq</c> quando a mensageria está habilitada.
/// </summary>
public static class RabbitMqHealthCheckExtensions
{
    private const string HealthCheckName = "rabbitmq";
    private static readonly TimeSpan HealthCheckTimeout = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Adiciona o probe AMQP ao pipeline de health checks somente se <see cref="MessagingOptions.Enabled"/> for <see langword="true"/>.
    /// </summary>
    /// <param name="builder">Builder retornado por <c>AddHealthChecks()</c>.</param>
    /// <param name="configuration">Configuração da aplicação.</param>
    /// <returns>O mesmo <paramref name="builder"/> para encadeamento.</returns>
    public static IHealthChecksBuilder AddRabbitMqHealthCheck(
        this IHealthChecksBuilder builder,
        IConfiguration configuration)
    {
        var messaging = configuration.GetSection(MessagingOptions.SectionName).Get<MessagingOptions>() ?? new MessagingOptions();
        if (!messaging.Enabled)
        {
            return builder;
        }

        var rabbit = configuration.GetSection(RabbitMqOptions.SectionName).Get<RabbitMqOptions>() ?? new RabbitMqOptions();
        var amqpUri = BuildAmqpUri(rabbit);

        return builder.AddRabbitMQ(
            _ => CreateConnectionAsync(amqpUri).GetAwaiter().GetResult(),
            name: HealthCheckName,
            failureStatus: HealthStatus.Unhealthy,
            timeout: HealthCheckTimeout);
    }

    /// <summary>
    /// Monta a URI AMQP a partir das opções tipadas.
    /// </summary>
    /// <param name="options">Credenciais e destino do broker.</param>
    /// <returns>URI no formato <c>amqp://</c>.</returns>
    internal static Uri BuildAmqpUri(RabbitMqOptions options)
    {
        var user = Uri.EscapeDataString(options.Username);
        var pass = Uri.EscapeDataString(options.Password);
        var vhost = options.VirtualHost.TrimStart('/');
        var vhostSegment = string.IsNullOrEmpty(vhost)
            ? "%2F"
            : Uri.EscapeDataString(vhost);

        var uriText = $"amqp://{user}:{pass}@{options.Host}:{options.Port}/{vhostSegment}";
        return new Uri(uriText);
    }

    private static async Task<IConnection> CreateConnectionAsync(Uri amqpUri)
    {
        var factory = new ConnectionFactory { Uri = amqpUri };
        return await factory.CreateConnectionAsync().ConfigureAwait(false);
    }
}