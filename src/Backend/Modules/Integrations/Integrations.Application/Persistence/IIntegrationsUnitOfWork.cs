using BuildingBlocks.Application.Persistence;

namespace Integrations.Application.Persistence;

/// <summary>
/// Contrato de Unit of Work para persistência atômica das operações do módulo de Integrações no banco do tenant.
/// </summary>
public interface IIntegrationsUnitOfWork : IUnitOfWork
{
}
