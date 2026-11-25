namespace WebApi.Configuration;

/// <summary>
/// Opções do consumidor de e-mails transacionais do catálogo Master.
/// </summary>
public sealed class CatalogEmailConsumerOptions
{
    /// <summary>
    /// Seção <c>Messaging:Consumers:CatalogEmail</c>.
    /// </summary>
    public const string SectionName = "Messaging:Consumers:CatalogEmail";

    /// <summary>
    /// Tentativas máximas de consumo (reagendamentos na mesma linha de outbox) antes de dead-letter.
    /// </summary>
    public int MaxAttempts { get; set; } = 5;
}