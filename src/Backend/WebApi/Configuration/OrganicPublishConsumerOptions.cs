namespace WebApi.Configuration;

/// <summary>
/// Opções do consumidor MassTransit de publicação orgânica por job.
/// </summary>
public sealed class OrganicPublishConsumerOptions
{
    /// <summary>
    /// Seção de configuração.
    /// </summary>
    public const string SectionName = "Messaging:Consumers:OrganicPublish";

    /// <summary>
    /// Prefetch do consumidor (1 a 4).
    /// </summary>
    public ushort Prefetch { get; set; } = 1;
}