using BuildingBlocks.Application.Persistence;

namespace Tenants.Application.Persistence;

/// <summary>
/// Contrato de unidade de trabalho responsável por coordenar a persistência atômica no banco de dados dedicado do inquilino.
/// </summary>
public interface ITenantUnitOfWork : IUnitOfWork
{
}
