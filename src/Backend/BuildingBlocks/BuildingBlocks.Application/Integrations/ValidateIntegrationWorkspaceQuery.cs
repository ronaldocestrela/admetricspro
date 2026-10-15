using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Tenants;

namespace BuildingBlocks.Application.Integrations;

/// <summary>Contrato inter-módulos para autorizar operações de integração no workspace atual.</summary>
/// <param name="WorkspaceId">Workspace solicitado.</param>
/// <param name="Manage">Indica administração de credenciais ou vínculos.</param>
/// <param name="EditCampaigns">Indica edição publicitária, sem conceder administração de credenciais.</param>
/// <param name="Permission">Permissão específica do consumidor de dados, quando necessária.</param>
/// <param name="IncludeInactive">Permite administrar cadastro de workspace pausado, sem liberar suas integrações.</param>
public sealed record ValidateIntegrationWorkspaceQuery(Guid WorkspaceId, bool Manage = true, bool EditCampaigns = false,
    TenantPermission? Permission = null, bool IncludeInactive = false) : IQuery<bool>;
