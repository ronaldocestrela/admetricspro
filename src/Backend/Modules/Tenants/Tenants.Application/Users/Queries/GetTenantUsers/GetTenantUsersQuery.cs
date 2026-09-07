using BuildingBlocks.Application.Messaging;
using Tenants.Application.Users.DTOs;

namespace Tenants.Application.Users.Queries.GetTenantUsers;

/// <summary>
/// Consulta para listar os colaboradores cadastrados no inquilino ativo com filtro opcional por status ativo.
/// </summary>
/// <param name="ActiveOnly">Filtro opcional para retornar apenas colaboradores ativos.</param>
public sealed record GetTenantUsersQuery(bool? ActiveOnly = null) : IQuery<IReadOnlyList<TenantUserDto>>;
