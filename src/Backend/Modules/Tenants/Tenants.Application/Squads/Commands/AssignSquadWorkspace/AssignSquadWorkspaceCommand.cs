using BuildingBlocks.Application.Messaging;

namespace Tenants.Application.Squads.Commands.AssignSquadWorkspace;

/// <summary>
/// Comando para alocar um cliente/workspace à carteira de atendimento do squad.
/// </summary>
/// <param name="SquadId">Identificador do squad.</param>
/// <param name="WorkspaceId">Identificador do workspace (<see cref="BuildingBlocks.Domain.Tenants.Workspace"/>).</param>
public sealed record AssignSquadWorkspaceCommand(Guid SquadId, Guid WorkspaceId) : ICommand;
