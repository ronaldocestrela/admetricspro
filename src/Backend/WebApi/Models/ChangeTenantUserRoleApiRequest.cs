using BuildingBlocks.Domain.Tenants;

namespace WebApi.Models;

/// <summary>
/// Modelo de requisição HTTP para alteração do papel funcional de um colaborador no inquilino.
/// </summary>
/// <param name="NewRole">Novo papel a ser atribuído ao colaborador.</param>
public sealed record ChangeTenantUserRoleApiRequest(TenantRole NewRole);
