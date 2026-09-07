using BuildingBlocks.Application.Messaging;

namespace Tenants.Application.Squads.Commands.UnassignSquadWorkspace;

/// <summary>
/// Comando para desassociar um cliente/workspace da carteira de atendimento do squad.
/// </summary>
/// <param name="SquadId">Identificador do squad.</param>
/// <param name="WorkspaceId">Identificador do workspace.</param>
public sealed record UnassignSquadWorkspaceCommand(Guid SquadId, Guid WorkspaceId) : ICommand;
