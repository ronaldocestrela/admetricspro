using BuildingBlocks.Application.Messaging;

namespace Tenants.Application.Squads.Commands.AddSquadMember;

/// <summary>
/// Comando para adicionar um colaborador ao time/squad.
/// </summary>
/// <param name="SquadId">Identificador do squad.</param>
/// <param name="UserId">Identificador do colaborador (<see cref="BuildingBlocks.Domain.Tenants.TenantUser"/>).</param>
public sealed record AddSquadMemberCommand(Guid SquadId, Guid UserId) : ICommand;
