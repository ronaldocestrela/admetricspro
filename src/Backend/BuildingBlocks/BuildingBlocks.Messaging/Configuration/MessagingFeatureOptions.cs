namespace BuildingBlocks.Messaging.Configuration;

/// <summary>
/// Feature flag de mensageria lida na borda MassTransit.
/// </summary>
public sealed class MessagingFeatureOptions
{
    /// <summary>
    /// Nome da seção <c>Messaging</c>.
    /// </summary>
    public const string SectionName = "Messaging";

    /// <summary>
    /// Indica se o transporte está habilitado.
    /// </summary>
    public bool Enabled { get; set; }
}