using BuildingBlocks.Application.Messaging;
using Tenants.Application.Squads.DTOs;

namespace Tenants.Application.Squads.Queries.GetSquads;

/// <summary>
/// Consulta para listar squads da agência com filtro opcional por status ativo.
/// </summary>
/// <param name="ActiveOnly">Filtro opcional para listar apenas squads ativos.</param>
public sealed record GetSquadsQuery(bool? ActiveOnly = null) : IQuery<IReadOnlyList<SquadSummaryDto>>;
