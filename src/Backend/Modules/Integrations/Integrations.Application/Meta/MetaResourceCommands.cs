using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Security;
using BuildingBlocks.Domain.Integrations;
using BuildingBlocks.Domain.Primitives;
using Integrations.Application.Persistence;
using Integrations.Domain.Campaigns;
using Integrations.Domain.Meta;
using Integrations.Domain.OAuth;
using MediatR;

namespace Integrations.Application.Meta;

/// <summary>Escrita explícita de recurso Meta com chave de idempotência e conta interna.</summary>
/// <param name="OperationId">Chave única preservada em novas tentativas do mesmo pedido.</param>
/// <param name="WorkspaceId">Workspace autorizado.</param>
/// <param name="AccountId">Conta interna vinculada.</param>
/// <param name="Resource">campaigns, adsets, ads ou adcreatives.</param>
/// <param name="ExternalId">Objeto editado, nulo para criação.</param>
/// <param name="Data">Campos oficiais permitidos.</param>
/// <param name="ConfirmActivation">Confirmação explícita de possível gasto.</param>
public sealed record WriteMetaResourceCommand(Guid OperationId, Guid WorkspaceId, Guid AccountId, string Resource,
    string? ExternalId, JsonElement Data, bool ConfirmActivation = false) : ICommand<MetaWriteResult>;

/// <summary>Consulta de recurso remoto vinculado à conta autorizada.</summary>
/// <param name="WorkspaceId">Workspace.</param>
/// <param name="AccountId">Conta interna.</param>
/// <param name="Resource">Tipo de recurso.</param>
/// <param name="ExternalId">Objeto externo.</param>
public sealed record GetMetaResourceQuery(Guid WorkspaceId, Guid AccountId, string Resource, string ExternalId) : IQuery<JsonElement>;

/// <summary>Consulta de ativos elegíveis sem expor dados individuais de públicos.</summary>
/// <param name="WorkspaceId">Workspace.</param>
/// <param name="AccountId">Conta interna.</param>
/// <param name="Asset">Tipo de ativo oficial.</param>
public sealed record GetMetaAssetsQuery(Guid WorkspaceId, Guid AccountId, string Asset) : IQuery<IReadOnlyList<JsonElement>>;

/// <summary>Pesquisa interesses, idiomas ou localizações por fonte oficial.</summary>
/// <param name="WorkspaceId">Workspace.</param>
/// <param name="AccountId">Conta interna.</param>
/// <param name="Type">Tipo oficial.</param>
/// <param name="Query">Termo pesquisado.</param>
public sealed record SearchMetaTargetingQuery(Guid WorkspaceId, Guid AccountId, string Type, string Query) : IQuery<IReadOnlyList<JsonElement>>;

/// <summary>Autoriza a conta antes da pesquisa de opções elegíveis na Meta.</summary>
/// <param name="sender">Mediador de contratos e comandos.</param>
/// <param name="accounts">Repositório existente de contas ou hierarquia.</param>
/// <param name="vaults">Repositório de credenciais contextualizadas.</param>
/// <param name="encryption">Proteção criptográfica das credenciais.</param>
/// <param name="api">Adaptador da operação oficial da Meta.</param>
public sealed class SearchMetaTargetingQueryHandler(ISender sender, ICampaignHierarchyRepository accounts, IOAuthTokenVaultRepository vaults,
    IOAuthEncryptionService encryption, IMetaManagementApi api) : IQueryHandler<SearchMetaTargetingQuery, IReadOnlyList<JsonElement>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<JsonElement>>> Handle(SearchMetaTargetingQuery request, CancellationToken cancellationToken)
    {
        var credentials = await MetaLinkedAccount.ResolveAsync(request.WorkspaceId, request.AccountId, false, sender, accounts, vaults, encryption, cancellationToken);
        return credentials.IsFailure ? Result<IReadOnlyList<JsonElement>>.Failure(credentials.Error) :
            await api.SearchTargetingAsync(request.Type, request.Query, credentials.Value.Token, cancellationToken);
    }
}

/// <summary>Executa escritas reais somente após autorização, validação e reserva durável.</summary>
/// <param name="sender">Mediador de contratos e comandos.</param>
/// <param name="user">Usuário autenticado que solicita a operação.</param>
/// <param name="accounts">Repositório existente de contas ou hierarquia.</param>
/// <param name="vaults">Repositório de credenciais contextualizadas.</param>
/// <param name="encryption">Proteção criptográfica das credenciais.</param>
/// <param name="api">Adaptador da operação oficial da Meta.</param>
/// <param name="operations">Diário idempotente das operações.</param>
/// <param name="unitOfWork">Unidade de trabalho do módulo.</param>
public sealed class WriteMetaResourceCommandHandler(ISender sender, ICurrentUserContext user, ICampaignHierarchyRepository accounts,
    IOAuthTokenVaultRepository vaults, IOAuthEncryptionService encryption, IMetaManagementApi api,
    IMetaOperationRepository operations, IIntegrationsUnitOfWork unitOfWork) : ICommandHandler<WriteMetaResourceCommand, MetaWriteResult>
{
    /// <inheritdoc />
    public async Task<Result<MetaWriteResult>> Handle(WriteMetaResourceCommand request, CancellationToken cancellationToken)
    {
        var data = MetaWriteValidation.Validate(request.Resource, request.ExternalId, request.Data, request.ConfirmActivation);
        if (data.IsFailure) return Result<MetaWriteResult>.Failure(data.Error);
        var credentials = await MetaLinkedAccount.ResolveAsync(request.WorkspaceId, request.AccountId, true, sender, accounts, vaults, encryption, cancellationToken);
        if (credentials.IsFailure) return Result<MetaWriteResult>.Failure(credentials.Error);
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
            { request.WorkspaceId, request.AccountId, request.Resource, request.ExternalId, Data = data.Value }))));
        var creation = MetaApiOperation.Create(request.OperationId, request.WorkspaceId, request.AccountId, user.UserId ?? Guid.Empty, request.Resource, hash);
        if (creation.IsFailure) return Result<MetaWriteResult>.Failure(creation.Error);
        var claim = await operations.TryBeginAsync(creation.Value, cancellationToken);
        if (claim.IsFailure) return Result<MetaWriteResult>.Failure(claim.Error);
        if (!claim.Value)
        {
            var previous = await operations.GetAsync(request.OperationId, request.WorkspaceId, request.AccountId, cancellationToken);
            if (previous is null || previous.RequestHash != hash)
                return Result<MetaWriteResult>.Failure(Error.Conflict("Meta.OperationConflict", "A chave de operação já foi usada por outro pedido."));
            if (previous.State == "Succeeded" && previous.ExternalId is { } id) return Result<MetaWriteResult>.Success(new(id, true));
            return Result<MetaWriteResult>.Failure(Error.Conflict("Meta.OperationAlreadySubmitted", "Esta operação já foi enviada. Consulte seu estado; pedidos pendentes ou incertos não são repetidos."));
        }
        var result = await api.WriteAsync(credentials.Value.Account.ExternalAccountId, request.Resource, request.ExternalId, data.Value, credentials.Value.Token, cancellationToken);
        if (result.IsFailure && result.Error.Code == "Meta.TokenInvalid")
        { credentials.Value.Vault.MarkExpired(); vaults.Update(credentials.Value.Vault); }
        creation.Value.Complete(result.IsSuccess ? result.Value.ExternalId : request.ExternalId, result.IsFailure ? result.Error.Code : null);
        operations.Update(creation.Value);
        // Preserva o resultado mesmo se o navegador cancelou após a Meta receber o POST.
        using var persistTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await unitOfWork.CommitAsync(persistTimeout.Token);
        return result;
    }
}

/// <summary>Consulta detalhes com isolamento de conta confirmado externamente.</summary>
/// <param name="sender">Mediador de contratos e comandos.</param>
/// <param name="accounts">Repositório existente de contas ou hierarquia.</param>
/// <param name="vaults">Repositório de credenciais contextualizadas.</param>
/// <param name="encryption">Proteção criptográfica das credenciais.</param>
/// <param name="api">Adaptador da operação oficial da Meta.</param>
public sealed class GetMetaResourceQueryHandler(ISender sender, ICampaignHierarchyRepository accounts, IOAuthTokenVaultRepository vaults,
    IOAuthEncryptionService encryption, IMetaManagementApi api) : IQueryHandler<GetMetaResourceQuery, JsonElement>
{
    /// <inheritdoc />
    public async Task<Result<JsonElement>> Handle(GetMetaResourceQuery request, CancellationToken cancellationToken)
    {
        var credentials = await MetaLinkedAccount.ResolveAsync(request.WorkspaceId, request.AccountId, false, sender, accounts, vaults, encryption, cancellationToken);
        return credentials.IsFailure ? Result<JsonElement>.Failure(credentials.Error) :
            await api.GetAsync(credentials.Value.Account.ExternalAccountId, request.Resource, request.ExternalId, credentials.Value.Token, cancellationToken);
    }
}

/// <summary>Consulta páginas, Instagram, pixels ou públicos acessíveis à conta selecionada.</summary>
/// <param name="sender">Mediador de contratos e comandos.</param>
/// <param name="accounts">Repositório existente de contas ou hierarquia.</param>
/// <param name="vaults">Repositório de credenciais contextualizadas.</param>
/// <param name="encryption">Proteção criptográfica das credenciais.</param>
/// <param name="api">Adaptador da operação oficial da Meta.</param>
public sealed class GetMetaAssetsQueryHandler(ISender sender, ICampaignHierarchyRepository accounts, IOAuthTokenVaultRepository vaults,
    IOAuthEncryptionService encryption, IMetaManagementApi api) : IQueryHandler<GetMetaAssetsQuery, IReadOnlyList<JsonElement>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<JsonElement>>> Handle(GetMetaAssetsQuery request, CancellationToken cancellationToken)
    {
        var credentials = await MetaLinkedAccount.ResolveAsync(request.WorkspaceId, request.AccountId, false, sender, accounts, vaults, encryption, cancellationToken);
        return credentials.IsFailure ? Result<IReadOnlyList<JsonElement>>.Failure(credentials.Error) :
            await api.ListAssetsAsync(credentials.Value.Account.ExternalAccountId, request.Asset, credentials.Value.Token, cancellationToken);
    }
}
