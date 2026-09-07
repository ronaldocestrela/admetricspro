using Master.Domain.Integrations;

namespace Master.Application.FeatureFlags.DTOs;

/// <summary>
/// Resumo do status operacional de congelamento do motor de automações cross-network.
/// </summary>
/// <param name="IsFrozen">Se verdadeiro, a execução de automações está bloqueada.</param>
/// <param name="Platform">Plataforma consultada (ou nulo para status global).</param>
/// <param name="ActiveKillSwitchKey">Chave do Kill Switch que causou o congelamento, se houver.</param>
/// <param name="CheckedAtUtc">Timestamp da verificação em UTC.</param>
public sealed record AutomationEngineStatusDto(
    bool IsFrozen,
    AdPlatform? Platform,
    string? ActiveKillSwitchKey,
    DateTime CheckedAtUtc);
