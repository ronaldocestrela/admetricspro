using BuildingBlocks.Application.Messaging;
using Tenants.Application.Ftux.DTOs;

namespace Tenants.Application.Ftux.Queries.GetTenantFtuxStatus;

/// <summary>
/// Consulta responsável por diagnosticar o progresso do First-Time User Experience (FTUX) no inquilino ativo.
/// </summary>
public sealed record GetTenantFtuxStatusQuery : IQuery<TenantFtuxStatusDto>;
