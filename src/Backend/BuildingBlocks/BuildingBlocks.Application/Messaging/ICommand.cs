using BuildingBlocks.Domain.Primitives;
using MediatR;

namespace BuildingBlocks.Application.Messaging;

/// <summary>
/// Represents a command that produces a non-generic <see cref="Result"/>.
/// </summary>
public interface ICommand : IRequest<Result>;

/// <summary>
/// Represents a command that produces a typed <see cref="Result{TResponse}"/>.
/// </summary>
/// <typeparam name="TResponse">The payload type returned on success.</typeparam>
public interface ICommand<TResponse> : IRequest<Result<TResponse>>;
