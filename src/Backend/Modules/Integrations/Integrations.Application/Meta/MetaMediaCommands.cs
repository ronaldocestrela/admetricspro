using System.Security.Cryptography;
using System.Text;
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

/// <summary>Upload autenticado com reserva idempotente; não cria nem ativa anúncios.</summary>
/// <param name="OperationId">Chave idempotente.</param>
/// <param name="WorkspaceId">Workspace.</param>
/// <param name="AccountId">Conta interna.</param>
/// <param name="ContentType">Tipo MIME.</param>
/// <param name="FileName">Nome do arquivo.</param>
/// <param name="Content">Bytes limitados.</param>
public sealed record UploadMetaMediaCommand(Guid OperationId, Guid WorkspaceId, Guid AccountId, string ContentType, string FileName, byte[] Content) : ICommand<MetaMediaResult>;

/// <summary>Consulta processamento somente de um vídeo enviado pelo contexto autorizado.</summary>
/// <param name="WorkspaceId">Workspace.</param>
/// <param name="AccountId">Conta interna.</param>
/// <param name="OperationId">Operação de upload que originou o vídeo.</param>
public sealed record GetMetaVideoStatusQuery(Guid WorkspaceId, Guid AccountId, Guid OperationId) : IQuery<MetaMediaResult>;

/// <summary>Registra uploads e impede novas tentativas automáticas de resultados incertos.</summary>
/// <param name="sender">Mediador de contratos e comandos.</param>
/// <param name="user">Usuário autenticado que solicita a operação.</param>
/// <param name="accounts">Repositório existente de contas ou hierarquia.</param>
/// <param name="vaults">Repositório de credenciais contextualizadas.</param>
/// <param name="encryption">Proteção criptográfica das credenciais.</param>
/// <param name="api">Adaptador da operação oficial da Meta.</param>
/// <param name="operations">Diário idempotente das operações.</param>
/// <param name="unitOfWork">Unidade de trabalho do módulo.</param>
public sealed class UploadMetaMediaCommandHandler(ISender sender, ICurrentUserContext user, ICampaignHierarchyRepository accounts,
    IOAuthTokenVaultRepository vaults, IOAuthEncryptionService encryption, IMetaMediaApi api,
    IMetaOperationRepository operations, IIntegrationsUnitOfWork unitOfWork) : ICommandHandler<UploadMetaMediaCommand, MetaMediaResult>
{
    /// <inheritdoc />
    public async Task<Result<MetaMediaResult>> Handle(UploadMetaMediaCommand request, CancellationToken cancellationToken)
    {
        if (request.Content is null || request.Content.Length is < 1 or > 100_000_000 || request.ContentType is not ("image/png" or "image/jpeg" or "video/mp4"))
            return Result<MetaMediaResult>.Failure(Error.Validation("Meta.InvalidMedia", "Arquivo vazio, excessivo ou formato não suportado."));
        var credentials = await MetaLinkedAccount.ResolveAsync(request.WorkspaceId, request.AccountId, true, sender, accounts, vaults, encryption, cancellationToken);
        if (credentials.IsFailure) return Result<MetaMediaResult>.Failure(credentials.Error);
        var mediaType = request.ContentType.StartsWith("image/", StringComparison.Ordinal) ? "image" : "video";
        var fingerprint = Convert.ToHexString(SHA256.HashData([.. Encoding.UTF8.GetBytes(request.ContentType + "|" + request.FileName + "|"), .. SHA256.HashData(request.Content)]));
        var operation = MetaApiOperation.Create(request.OperationId, request.WorkspaceId, request.AccountId, user.UserId ?? Guid.Empty, mediaType, fingerprint);
        if (operation.IsFailure) return Result<MetaMediaResult>.Failure(operation.Error);
        var claim = await operations.TryBeginAsync(operation.Value, cancellationToken);
        if (claim.IsFailure) return Result<MetaMediaResult>.Failure(claim.Error);
        if (!claim.Value)
        {
            var previous = await operations.GetAsync(request.OperationId, request.WorkspaceId, request.AccountId, cancellationToken);
            if (previous?.RequestHash == fingerprint && previous.State == "Succeeded" && previous.ExternalId is { } id)
                return Result<MetaMediaResult>.Success(new(id, mediaType, mediaType == "image" ? "Ready" : "Processing"));
            return Result<MetaMediaResult>.Failure(Error.Conflict("Meta.OperationAlreadySubmitted", "Este upload já foi enviado. Consulte o estado antes de repetir."));
        }
        var result = await api.UploadAsync(credentials.Value.Account.ExternalAccountId, credentials.Value.Token, request.ContentType, request.FileName, request.Content, cancellationToken);
        operation.Value.Complete(result.IsSuccess ? result.Value.ExternalId : null, result.IsFailure ? result.Error.Code : null);
        operations.Update(operation.Value);
        using var persistTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await unitOfWork.CommitAsync(persistTimeout.Token);
        return result;
    }
}

/// <summary>Confirma o vínculo local antes de consultar o processamento do vídeo.</summary>
/// <param name="sender">Mediador de contratos e comandos.</param>
/// <param name="accounts">Repositório existente de contas ou hierarquia.</param>
/// <param name="vaults">Repositório de credenciais contextualizadas.</param>
/// <param name="encryption">Proteção criptográfica das credenciais.</param>
/// <param name="api">Adaptador da operação oficial da Meta.</param>
/// <param name="operations">Diário idempotente das operações.</param>
public sealed class GetMetaVideoStatusQueryHandler(ISender sender, ICampaignHierarchyRepository accounts, IOAuthTokenVaultRepository vaults,
    IOAuthEncryptionService encryption, IMetaMediaApi api, IMetaOperationRepository operations) : IQueryHandler<GetMetaVideoStatusQuery, MetaMediaResult>
{
    /// <inheritdoc />
    public async Task<Result<MetaMediaResult>> Handle(GetMetaVideoStatusQuery request, CancellationToken cancellationToken)
    {
        var credentials = await MetaLinkedAccount.ResolveAsync(request.WorkspaceId, request.AccountId, false, sender, accounts, vaults, encryption, cancellationToken);
        if (credentials.IsFailure) return Result<MetaMediaResult>.Failure(credentials.Error);
        var operation = await operations.GetAsync(request.OperationId, request.WorkspaceId, request.AccountId, cancellationToken);
        if (operation is null || operation.Resource != "video" || operation.State != "Succeeded" || operation.ExternalId is null)
            return Result<MetaMediaResult>.Failure(Error.NotFound("Meta.VideoNotFound", "Vídeo enviado não encontrado nesta conta e workspace."));
        return await api.GetVideoStatusAsync(operation.ExternalId, credentials.Value.Token, cancellationToken);
    }
}
