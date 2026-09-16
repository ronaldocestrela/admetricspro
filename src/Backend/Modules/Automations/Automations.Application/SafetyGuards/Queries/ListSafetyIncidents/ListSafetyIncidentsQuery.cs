using Automations.Application.SafetyGuards.DTOs;
using Automations.Domain.SafetyGuards;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;

namespace Automations.Application.SafetyGuards.Queries.ListSafetyIncidents;

/// <summary>
/// Consulta CQRS para listar o histórico de incidentes de travas de segurança de um workspace.
/// </summary>
/// <param name="WorkspaceId">Identificador do workspace associado.</param>
/// <param name="Limit">Quantidade máxima de incidentes a retornar (padrão: 50).</param>
public sealed record ListSafetyIncidentsQuery(
    Guid WorkspaceId,
    int Limit = 50) : IQuery<IReadOnlyList<SafetyIncidentDto>>;

/// <summary>
/// Manipulador da consulta <see cref="ListSafetyIncidentsQuery"/>.
/// </summary>
public sealed class ListSafetyIncidentsQueryHandler : IQueryHandler<ListSafetyIncidentsQuery, IReadOnlyList<SafetyIncidentDto>>
{
    private readonly ISafetyGuardIncidentRepository _repository;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="ListSafetyIncidentsQueryHandler"/>.
    /// </summary>
    /// <param name="repository">Repositório de incidentes de segurança do inquilino.</param>
    public ListSafetyIncidentsQueryHandler(ISafetyGuardIncidentRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<SafetyIncidentDto>>> Handle(
        ListSafetyIncidentsQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.WorkspaceId == Guid.Empty)
        {
            return Result<IReadOnlyList<SafetyIncidentDto>>.Failure(
                Error.Validation("SafetyGuards.InvalidWorkspace", "O identificador do workspace é obrigatório."));
        }

        var incidents = await _repository.GetRecentByWorkspaceIdAsync(
            query.WorkspaceId,
            query.Limit,
            cancellationToken);

        var dtoList = incidents.Select(SafetyIncidentDto.FromEntity).ToList();

        return Result<IReadOnlyList<SafetyIncidentDto>>.Success(dtoList);
    }
}
