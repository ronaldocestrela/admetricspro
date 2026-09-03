using BuildingBlocks.Domain.Primitives;
using MediatR;

namespace BuildingBlocks.Application.Messaging;

/// <summary>
/// Represents a query that returns a typed <see cref="Result{TResponse}"/>.
/// </summary>
/// <typeparam name="TResponse">The payload type returned on success.</typeparam>
public interface IQuery<TResponse> : IRequest<Result<TResponse>>;
