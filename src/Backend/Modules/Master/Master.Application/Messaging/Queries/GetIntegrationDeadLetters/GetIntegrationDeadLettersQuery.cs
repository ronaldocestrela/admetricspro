using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;
using MediatR;

namespace Master.Application.Messaging.Queries.GetIntegrationDeadLetters;

/// <summary>
/// Consulta dead-letters do catálogo e dos tenants ativos/trial.
/// </summary>
/// <param name="Limit">Quantidade máxima por origem.</param>
public sealed record GetIntegrationDeadLettersQuery(int Limit = 50)
    : IRequest<Result<IReadOnlyList<IntegrationDeadLetterListItemDto>>>;