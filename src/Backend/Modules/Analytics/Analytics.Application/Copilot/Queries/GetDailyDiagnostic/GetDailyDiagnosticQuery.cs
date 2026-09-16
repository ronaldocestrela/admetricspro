using Analytics.Application.Copilot.DTOs;
using BuildingBlocks.Application.Messaging;

namespace Analytics.Application.Copilot.Queries.GetDailyDiagnostic;

/// <summary>
/// Consulta para obter o relatório de diagnóstico diário sintetizado pelo Copiloto de IA para um workspace.
/// </summary>
/// <param name="WorkspaceId">Identificador único do workspace.</param>
/// <param name="Date">Data de referência do diagnóstico (opcional, padrão hoje UTC).</param>
public sealed record GetDailyDiagnosticQuery(
    Guid WorkspaceId,
    DateTime? Date = null) : IQuery<DailyDiagnosticReportDto>;
