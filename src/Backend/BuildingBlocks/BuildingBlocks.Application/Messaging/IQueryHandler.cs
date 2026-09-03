using BuildingBlocks.Domain.Primitives;
using MediatR;

namespace BuildingBlocks.Application.Messaging;

/// <summary>
/// Defines a handler for queries returning a typed <see cref="Result{TResponse}"/>.
/// </summary>
/// <typeparam name="TQuery">The query type.</typeparam>
/// <typeparam name="TResponse">The payload type returned on success.</typeparam>
public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse>;
