namespace WebApi.Configuration;

/// <summary>
/// Parâmetros de conexão AMQP com o broker RabbitMQ (somente infraestrutura; sem publicação de negócio na Fase 1).
/// </summary>
public sealed class RabbitMqOptions
{
    /// <summary>
    /// Nome da seção em <c>IConfiguration</c> (variável de ambiente <c>RabbitMQ__*</c>).
    /// </summary>
    public const string SectionName = "RabbitMQ";

    /// <summary>
    /// Hostname do broker (nome do serviço Docker ou <c>localhost</c> no host).
    /// </summary>
    public string Host { get; set; } = "localhost";

    /// <summary>
    /// Porta AMQP padrão.
    /// </summary>
    public int Port { get; set; } = 5672;

    /// <summary>
    /// Virtual host lógico (ex.: <c>admetricspro</c>).
    /// </summary>
    public string VirtualHost { get; set; } = "admetricspro";

    /// <summary>
    /// Usuário AMQP dedicado (não usar <c>guest</c> fora do loopback).
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Senha AMQP; fornecida apenas por ambiente.
    /// </summary>
    public string Password { get; set; } = string.Empty;
}