using BuildingBlocks.Application.Messaging;

namespace Tenants.Application.Squads.Commands.RemoveSquadMember;

/// <summary>
/// Comando para desvincular um colaborador de um squad.
/// </summary>
/// <param name="SquadId">Identificador do squad.</param>
/// <param name="UserId">Identificador do colaborador.</param>
public sealed record RemoveSquadMemberCommand(Guid SquadId, Guid UserId) : ICommand;
