namespace SocialPublishing.Application.Messaging;

/// <summary>
/// Tipos versionados de integração do módulo SocialPublishing publicados no exchange <c>admetricspro.integration</c>.
/// </summary>
public static class SocialPublishingIntegrationMessageTypes
{
    /// <summary>
    /// Disparo assíncrono de um job de publicação orgânica devido e já reivindicado.
    /// </summary>
    public const string PublishOrganicSlot = "social.publish-organic-slot.v1";
}