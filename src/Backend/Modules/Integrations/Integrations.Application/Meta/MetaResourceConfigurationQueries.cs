using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;
using Integrations.Application.Options;
using Integrations.Application.Persistence;
using Integrations.Domain.Campaigns;
using Integrations.Domain.Meta;
using Integrations.Domain.OAuth;
using MediatR;
using Microsoft.Extensions.Options;

namespace Integrations.Application.Meta;

/// <summary>Consulta o catálogo de configurações para a conta autorizada.</summary>
/// <param name="WorkspaceId">Workspace.</param>
/// <param name="AccountId">Conta interna vinculada.</param>
/// <param name="Resource">Recurso oficial.</param>
public sealed record GetMetaResourceOptionsQuery(Guid WorkspaceId, Guid AccountId, string Resource) : IQuery<MetaResourceDefinition>;

/// <summary>Consulta os valores reais das configurações do objeto autorizado.</summary>
/// <param name="WorkspaceId">Workspace.</param>
/// <param name="AccountId">Conta interna vinculada.</param>
/// <param name="Resource">Recurso oficial.</param>
/// <param name="ExternalId">Objeto remoto.</param>
/// <param name="Fields">Campos individuais, ou nulo para todos os campos consultáveis.</param>
public sealed record GetMetaResourceConfigurationQuery(Guid WorkspaceId, Guid AccountId, string Resource,
    string ExternalId, IReadOnlyList<string>? Fields = null) : IQuery<MetaResourceConfiguration>;

/// <summary>Expõe opções documentadas somente após validar o vínculo e a versão da integração.</summary>
/// <param name="sender">Mediador de autorização.</param>
/// <param name="accounts">Repositório de contas.</param>
/// <param name="vaults">Cofre do workspace.</param>
/// <param name="encryption">Proteção de credenciais.</param>
/// <param name="options">Versão configurada.</param>
public sealed class GetMetaResourceOptionsQueryHandler(ISender sender, ICampaignHierarchyRepository accounts,
    IOAuthTokenVaultRepository vaults, IOAuthEncryptionService encryption, IOptions<MetaAdsOAuthOptions> options)
    : IQueryHandler<GetMetaResourceOptionsQuery, MetaResourceDefinition>
{
    /// <inheritdoc />
    public async Task<Result<MetaResourceDefinition>> Handle(GetMetaResourceOptionsQuery request, CancellationToken cancellationToken)
    {
        var definition = MetaResourceCatalog.Get(request.Resource);
        if (definition.IsFailure) return definition;
        if (options.Value.GraphApiVersion != definition.Value.ApiVersion)
            return Result<MetaResourceDefinition>.Failure(Error.Validation("Meta.ConfigurationVersionMismatch", "O catálogo de configurações requer a versão Graph v26.0 configurada no backend."));
        var credentials = await MetaLinkedAccount.ResolveAsync(request.WorkspaceId, request.AccountId, false, sender, accounts, vaults, encryption, cancellationToken);
        return credentials.IsFailure ? Result<MetaResourceDefinition>.Failure(credentials.Error) : definition;
    }
}

/// <summary>Obtém dados atuais sem substituir campos indisponíveis por padrões locais.</summary>
/// <param name="sender">Mediador de autorização.</param>
/// <param name="accounts">Repositório de contas.</param>
/// <param name="vaults">Cofre do workspace.</param>
/// <param name="encryption">Proteção de credenciais.</param>
/// <param name="api">Adaptador Graph contextualizado.</param>
public sealed class GetMetaResourceConfigurationQueryHandler(ISender sender, ICampaignHierarchyRepository accounts,
    IOAuthTokenVaultRepository vaults, IOAuthEncryptionService encryption, IMetaManagementApi api)
    : IQueryHandler<GetMetaResourceConfigurationQuery, MetaResourceConfiguration>
{
    /// <inheritdoc />
    public async Task<Result<MetaResourceConfiguration>> Handle(GetMetaResourceConfigurationQuery request, CancellationToken cancellationToken)
    {
        var definition = MetaResourceCatalog.Get(request.Resource);
        if (definition.IsFailure) return Result<MetaResourceConfiguration>.Failure(definition.Error);
        var credentials = await MetaLinkedAccount.ResolveAsync(request.WorkspaceId, request.AccountId, false, sender, accounts, vaults, encryption, cancellationToken);
        if (credentials.IsFailure) return Result<MetaResourceConfiguration>.Failure(credentials.Error);
        var fields = request.Fields ?? definition.Value.Fields.Where(f => f.Readable && f.Restriction is null).Select(f => f.Name).ToArray();
        return await api.ReadConfigurationAsync(credentials.Value.Account.ExternalAccountId, request.Resource, request.ExternalId,
            fields, credentials.Value.Token, cancellationToken);
    }
}
