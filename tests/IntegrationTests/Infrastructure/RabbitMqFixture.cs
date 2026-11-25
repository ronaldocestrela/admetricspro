using Testcontainers.RabbitMq;

namespace IntegrationTests.Infrastructure;

/// <summary>
/// RabbitMQ container fixture for integration tests (Fase 3).
/// </summary>
public sealed class RabbitMqFixture : IAsyncLifetime
{
    private readonly RabbitMqContainer _container = new RabbitMqBuilder("rabbitmq:4")
        .WithUsername("guest")
        .WithPassword("guest")
        .WithCleanUp(true)
        .Build();

    /// <summary>
    /// Obtém o hostname do broker.
    /// </summary>
    public string HostName => _container.Hostname;

    /// <summary>
    /// Obtém a porta AMQP.
    /// </summary>
    public int AmqpPort => _container.GetMappedPublicPort(5672);

    /// <summary>
    /// Usuário padrão do container de teste.
    /// </summary>
    public string Username => "guest";

    /// <summary>
    /// Senha padrão do container de teste.
    /// </summary>
    public string Password => "guest";

    /// <inheritdoc />
    public Task InitializeAsync() => _container.StartAsync();

    /// <inheritdoc />
    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}

/// <summary>
/// Coleção que combina SQL Server e RabbitMQ.
/// </summary>
[CollectionDefinition(Name)]
public sealed class MessagingSqlRabbitCollection
    : ICollectionFixture<SqlServerFixture>, ICollectionFixture<RabbitMqFixture>
{
    /// <summary>
    /// Nome da coleção xUnit.
    /// </summary>
    public const string Name = "MessagingSqlRabbitCollection";
}