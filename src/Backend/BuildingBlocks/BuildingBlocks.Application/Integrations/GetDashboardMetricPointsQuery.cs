using BuildingBlocks.Application.Messaging;

namespace BuildingBlocks.Application.Integrations;

/// <summary>Consulta inter-módulos das métricas diárias persistidas na carteira autorizada.</summary>
/// <param name="WorkspaceId">Workspace ou nulo para carteira acessível.</param>
/// <param name="StartDateUtc">Primeiro dia incluído.</param>
/// <param name="EndDateUtc">Último dia incluído.</param>
public sealed record GetDashboardMetricPointsQuery(Guid? WorkspaceId, DateTime StartDateUtc, DateTime EndDateUtc)
    : IQuery<IReadOnlyList<DashboardMetricPoint>>;

/// <summary>Ponto diário exportado sem compartilhar persistência entre módulos.</summary>
/// <param name="Date">Dia da métrica.</param>
/// <param name="Platform">Plataforma de origem.</param>
/// <param name="Currency">Moeda original.</param>
/// <param name="Spend">Investimento importado.</param>
/// <param name="Revenue">Valor de conversões importado.</param>
/// <param name="Impressions">Impressões importadas.</param>
/// <param name="Clicks">Cliques importados.</param>
/// <param name="Conversions">Conversões importadas.</param>
public sealed record DashboardMetricPoint(DateTime Date, string Platform, string Currency, decimal Spend,
    decimal Revenue, long Impressions, long Clicks, decimal Conversions);

/// <summary>Resolve workspaces ativos visíveis ao usuário do tenant corrente.</summary>
/// <param name="WorkspaceId">Workspace específico ou nulo para carteira.</param>
public sealed record GetAnalyticsWorkspaceIdsQuery(Guid? WorkspaceId) : IQuery<IReadOnlyList<Guid>>;
