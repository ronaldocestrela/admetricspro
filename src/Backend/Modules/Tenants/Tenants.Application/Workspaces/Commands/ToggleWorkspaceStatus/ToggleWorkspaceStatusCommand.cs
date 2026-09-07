using BuildingBlocks.Application.Messaging;

namespace Tenants.Application.Workspaces.Commands.ToggleWorkspaceStatus;

/// <summary>
/// Comando para alternar o status operacional de um workspace entre ativo e inativo.
/// </summary>
/// <param name="Id">Identificador único do workspace.</param>
public sealed record ToggleWorkspaceStatusCommand(Guid Id) : ICommand;
