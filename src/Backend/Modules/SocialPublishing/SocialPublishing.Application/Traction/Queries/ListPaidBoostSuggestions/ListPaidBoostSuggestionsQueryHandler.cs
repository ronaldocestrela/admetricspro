using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Application.Traction.DTOs;
using SocialPublishing.Domain;

namespace SocialPublishing.Application.Traction.Queries.ListPaidBoostSuggestions;

/// <summary>
/// Consulta sugestões de impulsionamento persistidas para o workspace.
/// </summary>
public sealed class ListPaidBoostSuggestionsQueryHandler
    : IQueryHandler<ListPaidBoostSuggestionsQuery, IReadOnlyList<PaidBoostSuggestionDto>>
{
    private readonly IPaidBoostSuggestionRepository _suggestionRepository;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="ListPaidBoostSuggestionsQueryHandler"/>.
    /// </summary>
    /// <param name="suggestionRepository">Repositório de sugestões.</param>
    public ListPaidBoostSuggestionsQueryHandler(IPaidBoostSuggestionRepository suggestionRepository)
    {
        _suggestionRepository = suggestionRepository ?? throw new ArgumentNullException(nameof(suggestionRepository));
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<PaidBoostSuggestionDto>>> Handle(
        ListPaidBoostSuggestionsQuery request,
        CancellationToken cancellationToken)
    {
        if (request.WorkspaceId == Guid.Empty)
        {
            return Result<IReadOnlyList<PaidBoostSuggestionDto>>.Failure(SocialErrors.WorkspaceRequired);
        }

        var suggestions = await _suggestionRepository.ListByWorkspaceAsync(request.WorkspaceId, cancellationToken);
        var dtos = suggestions.Select(PaidBoostSuggestionMapping.ToDto).ToList();
        return Result<IReadOnlyList<PaidBoostSuggestionDto>>.Success(dtos);
    }
}
