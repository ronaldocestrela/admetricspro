using BuildingBlocks.Application.Messaging;

namespace Master.Application.Tenants.Commands.ConfigureTenantCustomDomain;

/// <summary>
/// Comando para configurar ou alterar o domínio CNAME personalizado de um inquilino no catálogo MasterDb.
/// </summary>
/// <param name="TenantId">Identificador único do inquilino.</param>
/// <param name="CustomDomain">Domínio personalizado CNAME (ex: relatorios.agencia.com.br).</param>
public sealed record ConfigureTenantCustomDomainCommand(
    Guid TenantId,
    string CustomDomain) : ICommand;
