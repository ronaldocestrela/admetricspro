using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;
using Tenants.Application.Squads.DTOs;
using Tenants.Application.Squads.Repositories;

namespace Tenants.Application.Squads.Queries.GetSquads;

/// <summary>
/// Manipulador responsável por retornar a lista sumarizada de squads do inquilino.
/// </summary>
public sealed class GetSquadsQueryHandler : IQueryHandler<GetSquadsQuery, IReadOnlyList<SquadSummaryDto>>
{
    private readonly ISquadRepository _squadRepository;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="GetSquadsQueryHandler"/>.
    /// </summary>
    /// <param name="squadRepository">Repositório de squads.</param>
    public GetSquadsQueryHandler(ISquadRepository squadRepository)
    {
        _squadRepository = squadRepository ?? throw new ArgumentNullException(nameof(squadRepository));
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<SquadSummaryDto>>> Handle(GetSquadsQuery request, CancellationToken cancellationToken)
    {
        var squads = await _squadRepository.GetAllAsync(request.ActiveOnly, cancellationToken);

        var dtos = squads.Select(s => new SquadSummaryDto(
            s.Id,
            s.Name,
            s.Description,
            s.IsActive,
            s.Members.Count,
            s.Workspaces.Count,
            s.CreatedAtUtc
        )).ToList();

        return Result<IReadOnlyList<SquadSummaryDto>>.Success(dtos);
    }
}
