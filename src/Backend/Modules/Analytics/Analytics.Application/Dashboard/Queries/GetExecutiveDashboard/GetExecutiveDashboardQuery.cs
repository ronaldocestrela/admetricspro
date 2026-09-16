using Analytics.Application.Dashboard.DTOs;
using BuildingBlocks.Application.Messaging;

namespace Analytics.Application.Dashboard.Queries.GetExecutiveDashboard;

/// <summary>
/// Consulta analítica para obtenção do resumo executivo consolidado do dashboard unificado cross-network,
/// com suporte a comparação temporal automática com período anterior, séries temporais e segmentação.
/// </summary>
/// <param name="WorkspaceId">Identificador opcional do workspace para filtragem contextual.</param>
/// <param name="StartDateUtc">Data inicial UTC do período corrente (padrão: D-7).</param>
/// <param name="EndDateUtc">Data final UTC do período corrente (padrão: agora).</param>
/// <param name="Platform">Plataforma opcional de anúncios (Meta, Google, TikTok, Bing ou nulo para consolidado).</param>
/// <param name="Device">Dispositivo opcional de acesso (Mobile, Desktop, Tablet ou nulo para consolidado).</param>
/// <param name="Currency">Moeda de referência da visualização (padrão: BRL).</param>
public sealed record GetExecutiveDashboardQuery(
    Guid? WorkspaceId = null,
    DateTime? StartDateUtc = null,
    DateTime? EndDateUtc = null,
    string? Platform = null,
    string? Device = null,
    string? Currency = null) : IQuery<ExecutiveDashboardDto>;
