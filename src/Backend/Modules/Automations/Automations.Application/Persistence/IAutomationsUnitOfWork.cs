using BuildingBlocks.Application.Persistence;

namespace Automations.Application.Persistence;

/// <summary>
/// Contrato da unidade de trabalho para confirmação atômica de mutações no banco do inquilino do módulo Automations.
/// </summary>
public interface IAutomationsUnitOfWork : IUnitOfWork
{
}
