using System.Globalization;

namespace SocialPublishing.Domain.Publications;

/// <summary>
/// Formata a chave de idempotência lógica entre post e instante agendado.
/// </summary>
public static class PublicationJobKey
{
    /// <summary>
    /// Monta a chave estável <c>{postId:D}:{scheduledAtUtc:O}</c> em cultura invariante.
    /// </summary>
    /// <param name="postId">Identificador do post orgânico.</param>
    /// <param name="scheduledAtUtc">Horário do slot em UTC.</param>
    /// <returns>Chave única do job na outbox.</returns>
    public static string Build(Guid postId, DateTime scheduledAtUtc)
    {
        var normalized = scheduledAtUtc.Kind == DateTimeKind.Utc
            ? scheduledAtUtc
            : DateTime.SpecifyKind(scheduledAtUtc, DateTimeKind.Utc);

        return string.Create(
            CultureInfo.InvariantCulture,
            $"{postId:D}:{normalized:O}");
    }
}
