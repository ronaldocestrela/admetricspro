using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;
using MediatR;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Application.Traction.DTOs;
using SocialPublishing.Contracts.Boost;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Traction;

namespace SocialPublishing.Application.Traction.Commands.AcceptPaidBoostSuggestion;

/// <summary>
/// Processa aceite de sugestão de impulsionamento e handoff para criação de rascunho pago.
/// </summary>
public sealed class AcceptPaidBoostSuggestionCommandHandler
    : ICommandHandler<AcceptPaidBoostSuggestionCommand, PaidBoostSuggestionDto>
{
    private readonly IPaidBoostSuggestionRepository _suggestionRepository;
    private readonly IOrganicPostRepository _postRepository;
    private readonly ISocialPublishingUnitOfWork _unitOfWork;
    private readonly ISender _sender;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="AcceptPaidBoostSuggestionCommandHandler"/>.
    /// </summary>
    /// <param name="suggestionRepository">Repositório de sugestões.</param>
    /// <param name="postRepository">Repositório de posts orgânicos.</param>
    /// <param name="unitOfWork">Unit of work social.</param>
    /// <param name="sender">Mediador in-memory para Integrations.</param>
    public AcceptPaidBoostSuggestionCommandHandler(
        IPaidBoostSuggestionRepository suggestionRepository,
        IOrganicPostRepository postRepository,
        ISocialPublishingUnitOfWork unitOfWork,
        ISender sender)
    {
        _suggestionRepository = suggestionRepository ?? throw new ArgumentNullException(nameof(suggestionRepository));
        _postRepository = postRepository ?? throw new ArgumentNullException(nameof(postRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    /// <inheritdoc />
    public async Task<Result<PaidBoostSuggestionDto>> Handle(
        AcceptPaidBoostSuggestionCommand request,
        CancellationToken cancellationToken)
    {
        if (request.WorkspaceId == Guid.Empty)
        {
            return Result<PaidBoostSuggestionDto>.Failure(SocialErrors.WorkspaceRequired);
        }

        var suggestion = await _suggestionRepository.GetByIdAsync(request.SuggestionId, cancellationToken);
        if (suggestion is null || suggestion.WorkspaceId != request.WorkspaceId)
        {
            return Result<PaidBoostSuggestionDto>.Failure(SocialErrors.BoostSuggestionNotFound);
        }

        if (suggestion.Status != PaidBoostSuggestionStatus.Open)
        {
            return Result<PaidBoostSuggestionDto>.Failure(SocialErrors.InvalidTransition);
        }

        var post = await _postRepository.GetByIdAsync(suggestion.OrganicPostId, cancellationToken);
        if (post is null || post.WorkspaceId != request.WorkspaceId)
        {
            return Result<PaidBoostSuggestionDto>.Failure(SocialErrors.PostNotFound);
        }

        var mediaObjectKey = post.Media
            .OrderBy(m => m.SortOrder)
            .Select(m => m.ObjectKey)
            .FirstOrDefault(key => !string.IsNullOrWhiteSpace(key));

        var draftResult = await _sender.Send(
            new CreatePaidBoostDraftCommand(
                request.WorkspaceId,
                suggestion.Id,
                suggestion.OrganicPostId,
                suggestion.Platform.ToString(),
                post.Caption,
                mediaObjectKey),
            cancellationToken);

        if (draftResult.IsFailure)
        {
            return Result<PaidBoostSuggestionDto>.Failure(draftResult.Error);
        }

        var acceptResult = suggestion.Accept(draftResult.Value);
        if (acceptResult.IsFailure)
        {
            return Result<PaidBoostSuggestionDto>.Failure(acceptResult.Error);
        }

        await _suggestionRepository.UpdateAsync(suggestion, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        return Result<PaidBoostSuggestionDto>.Success(PaidBoostSuggestionMapping.ToDto(suggestion));
    }
}
