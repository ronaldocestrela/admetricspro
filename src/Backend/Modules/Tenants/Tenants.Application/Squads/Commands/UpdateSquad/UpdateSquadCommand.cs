using BuildingBlocks.Application.Messaging;

namespace Tenants.Application.Squads.Commands.UpdateSquad;

/// <summary>
/// Comando para atualizar os dados cadastrais (nome e descrição) de um squad existente.
/// </summary>
/// <param name="Id">Identificador único do squad.</param>
/// <param name="Name">Novo nome do squad.</param>
/// <param name="Description">Nova descrição ou notas de escopo.</param>
public sealed record UpdateSquadCommand(Guid Id, string Name, string? Description) : ICommand;
