using BuildingBlocks.Application.Integrations;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using MediatR;
using Tenants.Application.Integrations.Repositories;
using Tenants.Application.Persistence;

namespace Tenants.Application.Integrations;

/// <summary>Persiste vínculos no módulo proprietário usando seu repositório existente.</summary>
/// <param name="accounts">Repositório existente de contas ou hierarquia.</param>
/// <param name="sender">Mediador de contratos e comandos.</param>
/// <param name="unitOfWork">Unidade de trabalho do módulo.</param>
public sealed class RegisterConnectedAdAccountsCommandHandler(IConnectedAdAccountRepository accounts, ISender sender, ITenantUnitOfWork unitOfWork)
    : ICommandHandler<RegisterConnectedAdAccountsCommand, IReadOnlyList<Guid>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<Guid>>> Handle(RegisterConnectedAdAccountsCommand request, CancellationToken cancellationToken)
    {
        var access = await sender.Send(new ValidateIntegrationWorkspaceQuery(request.WorkspaceId), cancellationToken);
        if (access.IsFailure) return Result<IReadOnlyList<Guid>>.Failure(access.Error);
        if (!access.Value) return Result<IReadOnlyList<Guid>>.Failure(Error.Forbidden("Meta.Forbidden", "Sem permissão para vincular contas."));
        var existing = await accounts.GetByWorkspaceIdAsync(request.WorkspaceId, cancellationToken);
        var ids = new List<Guid>();
        foreach (var item in request.Accounts.DistinctBy(a => a.ExternalId))
        {
            var account = existing.FirstOrDefault(a => !a.IsDemo && a.Platform == "MetaAds" && a.ExternalAccountId == item.ExternalId);
            var created = account is null;
            if (account is null)
            {
                var result = ConnectedAdAccount.Create(Guid.NewGuid(), request.WorkspaceId, "MetaAds", item.ExternalId, item.Name, item.Currency);
                if (result.IsFailure) return Result<IReadOnlyList<Guid>>.Failure(result.Error);
                account = result.Value;
            }
            var bind = account.BindAuthorization(request.ConnectionId, request.WorkspaceId, item.Name, item.Currency, item.TimeZone, item.AccountStatus, item.Tasks);
            if (bind.IsFailure) return Result<IReadOnlyList<Guid>>.Failure(bind.Error);
            if (created) await accounts.AddAsync(account, cancellationToken);
            else accounts.Update(account);
            ids.Add(account.Id);
        }
        await unitOfWork.CommitAsync(cancellationToken);
        return Result<IReadOnlyList<Guid>>.Success(ids);
    }
}

/// <summary>Desvincula somente contas Meta pertencentes ao workspace solicitado.</summary>
/// <param name="accounts">Repositório existente de contas ou hierarquia.</param>
/// <param name="sender">Mediador de contratos e comandos.</param>
/// <param name="unitOfWork">Unidade de trabalho do módulo.</param>
public sealed class DisconnectMetaAccountsCommandHandler(IConnectedAdAccountRepository accounts, ISender sender, ITenantUnitOfWork unitOfWork)
    : ICommandHandler<DisconnectMetaAccountsCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(DisconnectMetaAccountsCommand request, CancellationToken cancellationToken)
    {
        var access = await sender.Send(new ValidateIntegrationWorkspaceQuery(request.WorkspaceId), cancellationToken);
        if (access.IsFailure) return Result.Failure(access.Error);
        if (!access.Value) return Result.Failure(Error.Forbidden("Meta.Forbidden", "Sem permissão para desvincular contas."));
        var existing = await accounts.GetByWorkspaceIdAsync(request.WorkspaceId, cancellationToken);
        var targets = existing.Where(a => !a.IsDemo && a.Platform == "MetaAds" && (!request.AccountId.HasValue || a.Id == request.AccountId)).ToArray();
        if (request.AccountId.HasValue && targets.Length == 0)
            return Result.Failure(Error.NotFound("Meta.AccountNotFound", "Conta Meta não encontrada neste workspace."));
        foreach (var account in targets) { account.Disconnect(); accounts.Update(account); }
        await unitOfWork.CommitAsync(cancellationToken);
        return Result.Success();
    }
}
