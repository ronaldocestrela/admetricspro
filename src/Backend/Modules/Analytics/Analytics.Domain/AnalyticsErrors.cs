using BuildingBlocks.Domain.Primitives;

namespace Analytics.Domain;

/// <summary>
/// Erros tipados do módulo Analytics para controle de fluxo sem exceções.
/// </summary>
public static class AnalyticsErrors
{
    /// <summary>
    /// Notificação de tração orgânica sem fingerprint utilizável para o Creative Hub.
    /// </summary>
    public static Error OrganicFingerprintMissing { get; } =
        Error.Validation(
            "Analytics.OrganicFingerprintMissing",
            "A notificação de tração orgânica não inclui fingerprint de mídia; o ativo não foi registrado no Creative Hub.");
}
