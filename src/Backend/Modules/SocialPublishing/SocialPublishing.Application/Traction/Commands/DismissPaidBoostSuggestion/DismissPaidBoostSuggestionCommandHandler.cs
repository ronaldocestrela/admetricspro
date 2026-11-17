using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Application.Traction.DTOs;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Traction;

namespace SocialPublishing.Application.Traction.Commands.DismissPaidBoostSuggestion;

/// <summary>
/// Processa dispensa de sugestão de impulsionamento orgânico.
/// </summary>
public sealed class DismissPaidBoostSuggestionCommandHandler
    : ICommandHandler<DismissPaidBoostSuggestionCommand, PaidBoostSuggestionDto>
{
    private readonly IPaidBoostSuggestionRepository _suggestionRepository;
    private readonly ISocialPublishingUnitOfWork _unitOfWork;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="DismissPaidBoostSuggestionCommandHandler"/>.
    /// </summary>
    /// <param name="suggestionRepository">Repositório de sugestões.</param>
    /// <param name="unitOfWork">Unit of work social.</param>
    public DismissPaidBoostSuggestionCommandHandler(
        IPaidBoostSuggestionRepository suggestionRepository,
        ISocialPublishingUnitOfWork unitOfWork)
    {
        _suggestionRepository = suggestionRepository ?? throw new ArgumentNullException(nameof(suggestionRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    /// <inheritdoc />
    public async Task<Result<PaidBoostSuggestionDto>> Handle(
        DismissPaidBoostSuggestionCommand request,
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

        var dismissResult = suggestion.Dismiss();
        if (dismissResult.IsFailure)
        {
            return Result<PaidBoostSuggestionDto>.Failure(dismissResult.Error);
        }

        await _suggestionRepository.UpdateAsync(suggestion, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        return Result<PaidBoostSuggestionDto>.Success(PaidBoostSuggestionMapping.ToDto(suggestion));
    }
}
