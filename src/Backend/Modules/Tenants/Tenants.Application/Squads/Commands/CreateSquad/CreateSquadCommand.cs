using BuildingBlocks.Application.Messaging;

namespace Tenants.Application.Squads.Commands.CreateSquad;

/// <summary>
/// Comando para criar um novo time/squad no ambiente operacional do inquilino.
/// </summary>
/// <param name="Name">Nome de identificação do squad.</param>
/// <param name="Description">Descrição ou escopo operacional opcional.</param>
public sealed record CreateSquadCommand(string Name, string? Description) : ICommand<Guid>;
