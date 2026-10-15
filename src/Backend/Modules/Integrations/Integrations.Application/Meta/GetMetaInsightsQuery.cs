using BuildingBlocks.Application.Integrations;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;
using Integrations.Application.Persistence;
using Integrations.Domain.Campaigns;
using Integrations.Domain.Meta;
using Integrations.Domain.OAuth;
using MediatR;

namespace Integrations.Application.Meta;

/// <summary>Consulta Insights de uma conta vinculada ao workspace.</summary>
/// <param name="WorkspaceId">Workspace autorizado.</param>
/// <param name="AccountId">ID interno da conta vinculada.</param>
/// <param name="Filter">Filtros de leitura.</param>
public sealed record GetMetaInsightsQuery(Guid WorkspaceId, Guid AccountId, MetaInsightsFilter Filter) : IQuery<IReadOnlyList<MetaInsight>>;

/// <summary>Resolve autorização e vínculo antes de consultar métricas da conta.</summary>
/// <param name="sender">Mediador de contratos e comandos.</param>
/// <param name="accounts">Repositório existente de contas ou hierarquia.</param>
/// <param name="vaults">Repositório de credenciais contextualizadas.</param>
/// <param name="encryption">Proteção criptográfica das credenciais.</param>
/// <param name="api">Adaptador da operação oficial da Meta.</param>
/// <param name="unitOfWork">Unidade de trabalho do módulo.</param>
public sealed class GetMetaInsightsQueryHandler(ISender sender, ICampaignHierarchyRepository accounts,
    IOAuthTokenVaultRepository vaults, IOAuthEncryptionService encryption, IMetaInsightsApi api,
    IIntegrationsUnitOfWork unitOfWork) : IQueryHandler<GetMetaInsightsQuery, IReadOnlyList<MetaInsight>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<MetaInsight>>> Handle(GetMetaInsightsQuery request, CancellationToken cancellationToken)
    {
        var credentials = await MetaLinkedAccount.ResolveAsync(request.WorkspaceId, request.AccountId, false, sender, accounts, vaults, encryption, cancellationToken);
        if (credentials.IsFailure) return Result<IReadOnlyList<MetaInsight>>.Failure(credentials.Error);
        var result = await api.ReadAsync(credentials.Value.Account.ExternalAccountId, credentials.Value.Token, request.Filter, cancellationToken);
        if (result.IsFailure && result.Error.Code == "Meta.TokenInvalid")
        {
            credentials.Value.Vault.MarkExpired();
            vaults.Update(credentials.Value.Vault);
            await unitOfWork.CommitAsync(cancellationToken);
        }
        return result;
    }
}

internal sealed class MetaLinkedAccount
{
    internal required BuildingBlocks.Domain.Tenants.ConnectedAdAccount Account { get; init; }
    internal required BuildingBlocks.Domain.Integrations.OAuthTokenVault Vault { get; init; }
    internal required string Token { get; init; }

    internal static async Task<Result<MetaLinkedAccount>> ResolveAsync(Guid workspaceId, Guid accountId, bool write,
        ISender sender, ICampaignHierarchyRepository accounts, IOAuthTokenVaultRepository vaults,
        IOAuthEncryptionService encryption, CancellationToken cancellationToken)
    {
        var access = await sender.Send(new ValidateIntegrationWorkspaceQuery(workspaceId, false, write), cancellationToken);
        if (access.IsFailure) return Result<MetaLinkedAccount>.Failure(access.Error);
        if (!access.Value) return Result<MetaLinkedAccount>.Failure(Error.Forbidden("Meta.Forbidden", "Sem permissão para operar neste workspace."));
        var list = await accounts.GetAccountsForSyncAsync(workspaceId, accountId, "MetaAds", cancellationToken);
        var account = list.FirstOrDefault(a => a.Id == accountId && a.WorkspaceId == workspaceId && a.Platform == "MetaAds" && !a.IsDemo && a.Status == "Connected");
        if (account is null) return Result<MetaLinkedAccount>.Failure(Error.NotFound("Meta.AccountNotLinked", "Conta Meta não vinculada ativamente a este workspace."));
        var vault = await vaults.GetByWorkspaceAndPlatformAsync(workspaceId, "MetaAds", cancellationToken);
        if (vault is null || vault.WorkspaceId != workspaceId || vault.Platform != "MetaAds" || vault.Id != account.OAuthTokenVaultId ||
            vault.Status != "Active" || vault.AccessTokenExpiresAtUtc <= DateTime.UtcNow)
            return Result<MetaLinkedAccount>.Failure(Error.Unauthorized("Meta.ConnectionUnavailable", "Reconecte a Meta e selecione novamente as contas deste workspace."));
        if (!vault.Scopes.Split(',').Contains(write ? "ads_management" : "ads_read", StringComparer.Ordinal))
            return Result<MetaLinkedAccount>.Failure(Error.Forbidden("Meta.ScopeMissing", "A autorização Meta não possui a permissão necessária."));
        if (write && !account.AuthorizedTasks.Split(',').Any(t => t is "ADVERTISE" or "MANAGE"))
            return Result<MetaLinkedAccount>.Failure(Error.Forbidden("Meta.WriteForbidden", "A autorização não permite gerenciar anúncios desta conta."));
        try
        {
            var token = encryption.Decrypt(vault.EncryptedAccessToken, BuildingBlocks.Domain.Integrations.OAuthTokenVault.EncryptionPurpose(vault.WorkspaceId, vault.Id, vault.Platform));
            if (string.IsNullOrWhiteSpace(token)) return Result<MetaLinkedAccount>.Failure(Error.Unauthorized("Meta.CredentialUnavailable", "Reconecte a Meta."));
            return Result<MetaLinkedAccount>.Success(new() { Account = account, Vault = vault, Token = token });
        }
        catch (Exception ex) when (ex is System.Security.Cryptography.CryptographicException or FormatException)
        { return Result<MetaLinkedAccount>.Failure(Error.Unauthorized("Meta.CredentialUnavailable", "A credencial não pôde ser aberta. Reconecte a Meta.")); }
    }
}
