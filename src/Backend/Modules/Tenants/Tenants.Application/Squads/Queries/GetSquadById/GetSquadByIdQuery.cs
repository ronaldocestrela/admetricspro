using BuildingBlocks.Application.Messaging;
using Tenants.Application.Squads.DTOs;

namespace Tenants.Application.Squads.Queries.GetSquadById;

/// <summary>
/// Consulta para obter os dados detalhados de um squad específico.
/// </summary>
/// <param name="SquadId">Identificador único do squad.</param>
public sealed record GetSquadByIdQuery(Guid SquadId) : IQuery<SquadDetailsDto>;
