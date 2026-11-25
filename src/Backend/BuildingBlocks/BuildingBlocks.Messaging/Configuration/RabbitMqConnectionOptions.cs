namespace BuildingBlocks.Messaging.Configuration;

/// <summary>
/// Parâmetros de conexão AMQP para MassTransit.
/// </summary>
public sealed class RabbitMqConnectionOptions
{
    /// <summary>
    /// Nome da seção <c>RabbitMQ</c>.
    /// </summary>
    public const string SectionName = "RabbitMQ";

    /// <summary>
    /// Hostname do broker.
    /// </summary>
    public string Host { get; set; } = "localhost";

    /// <summary>
    /// Porta AMQP.
    /// </summary>
    public int Port { get; set; } = 5672;

    /// <summary>
    /// Virtual host.
    /// </summary>
    public string VirtualHost { get; set; } = "admetricspro";

    /// <summary>
    /// Usuário AMQP.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Senha AMQP.
    /// </summary>
    public string Password { get; set; } = string.Empty;
}