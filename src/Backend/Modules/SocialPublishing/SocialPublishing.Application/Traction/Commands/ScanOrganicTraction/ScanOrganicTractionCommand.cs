using BuildingBlocks.Application.Messaging;

namespace SocialPublishing.Application.Traction.Commands.ScanOrganicTraction;

/// <summary>
/// Comando interno que varre posts publicados, detecta tração e persiste sugestões abertas.
/// </summary>
public sealed record ScanOrganicTractionCommand : ICommand<ScanOrganicTractionSummary>;

/// <summary>
/// Resumo da varredura de tração orgânica em um tenant.
/// </summary>
/// <param name="SuggestionsCreated">Quantidade de sugestões persistidas e notificadas.</param>
/// <param name="InsightsFailures">Leituras de insights que falharam e foram ignoradas.</param>
public sealed record ScanOrganicTractionSummary(int SuggestionsCreated, int InsightsFailures);
