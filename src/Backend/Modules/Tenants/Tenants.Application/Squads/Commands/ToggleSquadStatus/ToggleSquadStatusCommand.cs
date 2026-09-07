using BuildingBlocks.Application.Messaging;

namespace Tenants.Application.Squads.Commands.ToggleSquadStatus;

/// <summary>
/// Comando para alternar o status operacional do squad (ativo ou inativo).
/// </summary>
/// <param name="Id">Identificador único do squad.</param>
/// <param name="IsActive">Novo status do squad.</param>
public sealed record ToggleSquadStatusCommand(Guid Id, bool IsActive) : ICommand;
