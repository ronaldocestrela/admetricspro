using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;
using Tenants.Application.Integrations.DTOs;
using Tenants.Application.Integrations.Repositories;

namespace Tenants.Application.Integrations.Queries.GetConnectedAdAccounts;

/// <summary>
/// Manipulador da consulta <see cref="GetConnectedAdAccountsQuery"/> que retorna a lista de contas de anúncios vinculadas.
/// </summary>
public sealed class GetConnectedAdAccountsQueryHandler : IQueryHandler<GetConnectedAdAccountsQuery, IReadOnlyList<ConnectedAdAccountDto>>
{
    private readonly IConnectedAdAccountRepository _adAccountRepository;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="GetConnectedAdAccountsQueryHandler"/>.
    /// </summary>
    /// <param name="adAccountRepository">Repositório de contas conectadas.</param>
    public GetConnectedAdAccountsQueryHandler(IConnectedAdAccountRepository adAccountRepository)
    {
        _adAccountRepository = adAccountRepository ?? throw new ArgumentNullException(nameof(adAccountRepository));
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<ConnectedAdAccountDto>>> Handle(
        GetConnectedAdAccountsQuery query,
        CancellationToken cancellationToken)
    {
        var accounts = query.WorkspaceId.HasValue
            ? await _adAccountRepository.GetByWorkspaceIdAsync(query.WorkspaceId.Value, cancellationToken)
            : await _adAccountRepository.GetAllAsync(cancellationToken);

        var dtos = accounts.Select(a => new ConnectedAdAccountDto(
            a.Id,
            a.WorkspaceId,
            a.Platform,
            a.ExternalAccountId,
            a.AccountName,
            a.Currency,
            a.Status,
            a.IsDemo,
            a.CreatedAtUtc)).ToList();

        return Result<IReadOnlyList<ConnectedAdAccountDto>>.Success(dtos);
    }
}
