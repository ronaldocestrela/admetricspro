using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Integrations;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using Integrations.Application.Boost.Persistence;
using Integrations.Application.Persistence;
using Integrations.Domain.Campaigns;
using SocialPublishing.Contracts.Boost;

namespace Integrations.Application.Boost.Commands.CreatePaidBoostDraft;

/// <summary>
/// Cria rascunho local pausado de anúncio pago a partir de aceite de tração orgânica, sem POST na rede.
/// </summary>
public sealed class CreatePaidBoostDraftCommandHandler : ICommandHandler<CreatePaidBoostDraftCommand, Guid>
{
    private readonly ICampaignHierarchyRepository _hierarchyRepository;
    private readonly IPaidBoostDraftRepository _draftRepository;
    private readonly IIntegrationsUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="CreatePaidBoostDraftCommandHandler"/>.
    /// </summary>
    /// <param name="hierarchyRepository">Repositório de contas e hierarquia.</param>
    /// <param name="draftRepository">Repositório de rascunhos locais.</param>
    /// <param name="unitOfWork">Unit of work de Integrations.</param>
    /// <param name="timeProvider">Relógio UTC.</param>
    public CreatePaidBoostDraftCommandHandler(
        ICampaignHierarchyRepository hierarchyRepository,
        IPaidBoostDraftRepository draftRepository,
        IIntegrationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _hierarchyRepository = hierarchyRepository ?? throw new ArgumentNullException(nameof(hierarchyRepository));
        _draftRepository = draftRepository ?? throw new ArgumentNullException(nameof(draftRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(CreatePaidBoostDraftCommand request, CancellationToken cancellationToken)
    {
        if (request.WorkspaceId == Guid.Empty)
        {
            return Result<Guid>.Failure(
                Error.Validation("PaidBoostDraft.WorkspaceRequired", "O workspace é obrigatório para o rascunho."));
        }

        var existing = await _draftRepository.GetBySuggestionIdAsync(
            request.WorkspaceId,
            request.SuggestionId,
            cancellationToken);
        if (existing is not null)
        {
            return Result<Guid>.Success(existing.Id);
        }

        if (!TryMapPaidPlatform(request.Network, out var paidPlatform, out var missingAccountError))
        {
            return Result<Guid>.Failure(
                Error.Validation("PaidBoostDraft.NetworkUnsupported", "A rede orgânica informada não possui mapeamento de mídia paga."));
        }

        var accounts = await _hierarchyRepository.GetAccountsForSyncAsync(
            request.WorkspaceId,
            connectedAdAccountId: null,
            paidPlatform,
            cancellationToken);

        if (accounts.Count == 0)
        {
            return Result<Guid>.Failure(missingAccountError);
        }

        var account = SelectAccount(accounts);
        var draftResult = PaidBoostDraft.CreatePaused(
            Guid.NewGuid(),
            request.WorkspaceId,
            request.SuggestionId,
            request.OrganicPostId,
            account.Id,
            paidPlatform,
            request.Caption,
            request.MediaObjectKey,
            _timeProvider.GetUtcNow().UtcDateTime);

        if (draftResult.IsFailure)
        {
            return Result<Guid>.Failure(draftResult.Error);
        }

        await _draftRepository.AddAsync(draftResult.Value, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        return Result<Guid>.Success(draftResult.Value.Id);
    }

    private static ConnectedAdAccount SelectAccount(IReadOnlyList<ConnectedAdAccount> accounts) =>
        accounts
            .OrderBy(a => a.CreatedAtUtc)
            .ThenBy(a => a.Id)
            .First();

    private static bool TryMapPaidPlatform(
        string network,
        out string paidPlatform,
        out Error missingAccountError)
    {
        missingAccountError = Error.NotFound("Meta.AccountNotLinked", "Conta Meta não vinculada ativamente a este workspace.");
        paidPlatform = string.Empty;

        if (string.Equals(network, "Instagram", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(network, "FacebookPage", StringComparison.OrdinalIgnoreCase))
        {
            paidPlatform = "MetaAds";
            return true;
        }

        if (string.Equals(network, "TikTok", StringComparison.OrdinalIgnoreCase))
        {
            paidPlatform = "TikTokAds";
            missingAccountError = Error.NotFound(
                "SyncCampaignHierarchy.AccountNotFound",
                "A conta de anúncios solicitada não foi localizada.");
            return true;
        }

        missingAccountError = Error.Validation("PaidBoostDraft.NetworkUnsupported", "Rede orgânica não suportada.");
        return false;
    }
}
