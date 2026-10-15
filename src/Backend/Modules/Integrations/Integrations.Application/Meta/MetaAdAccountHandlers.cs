using BuildingBlocks.Application.Integrations;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Integrations;
using BuildingBlocks.Domain.Primitives;
using Integrations.Application.Persistence;
using Integrations.Domain.Meta;
using Integrations.Domain.OAuth;
using MediatR;

namespace Integrations.Application.Meta;

/// <summary>Consulta contas acessíveis pela autorização Meta do workspace.</summary>
/// <param name="WorkspaceId">Workspace proprietário.</param>
public sealed record GetMetaAdAccountsQuery(Guid WorkspaceId) : IQuery<IReadOnlyList<MetaAdAccount>>;

/// <summary>Seleciona contas verificadas na Meta para o workspace.</summary>
/// <param name="WorkspaceId">Workspace proprietário.</param>
/// <param name="AccountIds">IDs externos canônicos das contas escolhidas.</param>
public sealed record SelectMetaAdAccountsCommand(Guid WorkspaceId, IReadOnlyList<string> AccountIds) : ICommand<IReadOnlyList<Guid>>;

/// <summary>Lista contas após validar usuário, workspace, credencial e permissão de leitura.</summary>
/// <param name="vaults">Repositório de credenciais contextualizadas.</param>
/// <param name="encryption">Proteção criptográfica das credenciais.</param>
/// <param name="api">Adaptador da operação oficial da Meta.</param>
/// <param name="sender">Mediador de contratos e comandos.</param>
/// <param name="unitOfWork">Unidade de trabalho do módulo.</param>
public sealed class GetMetaAdAccountsQueryHandler(IOAuthTokenVaultRepository vaults, IOAuthEncryptionService encryption,
    IMetaAdAccountsApi api, ISender sender, IIntegrationsUnitOfWork unitOfWork) : IQueryHandler<GetMetaAdAccountsQuery, IReadOnlyList<MetaAdAccount>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<MetaAdAccount>>> Handle(GetMetaAdAccountsQuery request, CancellationToken cancellationToken)
    {
        var vault = await MetaAuthorization.LoadAsync(request.WorkspaceId, vaults, sender, cancellationToken);
        if (vault.IsFailure) return Result<IReadOnlyList<MetaAdAccount>>.Failure(vault.Error);
        try
        {
            var result = await api.ListAsync(encryption.Decrypt(vault.Value.EncryptedAccessToken, OAuthTokenVault.EncryptionPurpose(vault.Value.WorkspaceId, vault.Value.Id, vault.Value.Platform)), cancellationToken);
            if (result.IsFailure && result.Error.Code == "Meta.TokenInvalid")
            {
                vault.Value.MarkExpired();
                vaults.Update(vault.Value);
                await unitOfWork.CommitAsync(cancellationToken);
            }
            return result;
        }
        catch (System.Security.Cryptography.CryptographicException)
        { return Result<IReadOnlyList<MetaAdAccount>>.Failure(Error.Unauthorized("Meta.CredentialUnavailable", "A credencial não pôde ser aberta. Reconecte a Meta.")); }
    }
}

/// <summary>Confirma acesso externo antes de enviar vínculos ao módulo Tenants via contrato in-memory.</summary>
/// <param name="vaults">Repositório de credenciais contextualizadas.</param>
/// <param name="encryption">Proteção criptográfica das credenciais.</param>
/// <param name="api">Adaptador da operação oficial da Meta.</param>
/// <param name="sender">Mediador de contratos e comandos.</param>
/// <param name="unitOfWork">Unidade de trabalho do módulo.</param>
public sealed class SelectMetaAdAccountsCommandHandler(IOAuthTokenVaultRepository vaults, IOAuthEncryptionService encryption,
    IMetaAdAccountsApi api, ISender sender, IIntegrationsUnitOfWork unitOfWork) : ICommandHandler<SelectMetaAdAccountsCommand, IReadOnlyList<Guid>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<Guid>>> Handle(SelectMetaAdAccountsCommand request, CancellationToken cancellationToken)
    {
        if (request.AccountIds is null || request.AccountIds.Count is < 1 or > 100 || request.AccountIds.Any(id =>
            string.IsNullOrWhiteSpace(id) || !id.StartsWith("act_", StringComparison.Ordinal) || id.Length < 5 || !id[4..].All(char.IsAsciiDigit)))
            return Result<IReadOnlyList<Guid>>.Failure(Error.Validation("Meta.InvalidAccounts", "Selecione de 1 a 100 contas Meta válidas."));
        var vault = await MetaAuthorization.LoadAsync(request.WorkspaceId, vaults, sender, cancellationToken);
        if (vault.IsFailure) return Result<IReadOnlyList<Guid>>.Failure(vault.Error);
        var available = await new GetMetaAdAccountsQueryHandler(vaults, encryption, api, sender, unitOfWork)
            .Handle(new(request.WorkspaceId), cancellationToken);
        if (available.IsFailure) return Result<IReadOnlyList<Guid>>.Failure(available.Error);
        var wanted = request.AccountIds.Distinct(StringComparer.Ordinal).ToArray();
        var selected = available.Value.Where(a => wanted.Contains(a.Id, StringComparer.Ordinal)).ToArray();
        if (selected.Length != wanted.Length)
            return Result<IReadOnlyList<Guid>>.Failure(Error.Forbidden("Meta.AccountNotAuthorized", "Uma das contas selecionadas não está acessível pela autorização deste workspace."));
        var result = await sender.Send(new RegisterConnectedAdAccountsCommand(request.WorkspaceId, vault.Value.Id,
            selected.Select(a => new VerifiedAdAccount(a.Id, a.Name, a.Currency, a.TimeZone, a.AccountStatus, string.Join(',', a.Tasks))).ToArray()), cancellationToken);
        if (result.IsSuccess)
        {
            vault.Value.Activate();
            vaults.Update(vault.Value);
            await unitOfWork.CommitAsync(cancellationToken);
        }
        return result;
    }
}

internal static class MetaAuthorization
{
    internal static async Task<Result<OAuthTokenVault>> LoadAsync(Guid workspaceId, IOAuthTokenVaultRepository vaults, ISender sender, CancellationToken cancellationToken)
    {
        var access = await sender.Send(new ValidateIntegrationWorkspaceQuery(workspaceId), cancellationToken);
        if (access.IsFailure) return Result<OAuthTokenVault>.Failure(access.Error);
        if (!access.Value) return Result<OAuthTokenVault>.Failure(Error.Forbidden("Meta.Forbidden", "Sem permissão para administrar esta conexão."));
        var vault = await vaults.GetByWorkspaceAndPlatformAsync(workspaceId, OAuthPlatform.MetaAds, cancellationToken);
        if (vault is null || vault.WorkspaceId != workspaceId || vault.Platform != OAuthPlatform.MetaAds ||
            (vault.Status != OAuthConnectionStatus.Active && vault.Status != "AwaitingAccountSelection") ||
            vault.AccessTokenExpiresAtUtc <= DateTime.UtcNow)
            return Result<OAuthTokenVault>.Failure(Error.Unauthorized("Meta.ConnectionUnavailable", "Conecte ou reconecte a Meta neste workspace."));
        if (!vault.Scopes.Split(',').Contains("ads_read", StringComparer.Ordinal))
            return Result<OAuthTokenVault>.Failure(Error.Forbidden("Meta.MissingAdsRead", "A autorização não possui ads_read."));
        return Result<OAuthTokenVault>.Success(vault);
    }
}
