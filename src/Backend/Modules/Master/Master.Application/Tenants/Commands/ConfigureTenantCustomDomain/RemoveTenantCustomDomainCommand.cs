using BuildingBlocks.Application.Messaging;

namespace Master.Application.Tenants.Commands.ConfigureTenantCustomDomain;

/// <summary>
/// Comando para remoção ou cancelamento do apontamento CNAME de um inquilino no catálogo MasterDb.
/// </summary>
/// <param name="TenantId">Identificador do inquilino.</param>
public sealed record RemoveTenantCustomDomainCommand(Guid TenantId) : ICommand;
