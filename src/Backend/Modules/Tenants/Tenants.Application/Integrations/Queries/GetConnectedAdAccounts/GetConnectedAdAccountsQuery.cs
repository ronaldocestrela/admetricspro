using BuildingBlocks.Application.Messaging;
using Tenants.Application.Integrations.DTOs;

namespace Tenants.Application.Integrations.Queries.GetConnectedAdAccounts;

/// <summary>
/// Consulta para listar as contas de anúncios conectadas no inquilino ativo, com filtro opcional por workspace.
/// </summary>
/// <param name="WorkspaceId">Identificador opcional de workspace para filtrar as contas.</param>
public sealed record GetConnectedAdAccountsQuery(Guid? WorkspaceId = null) : IQuery<IReadOnlyList<ConnectedAdAccountDto>>;
