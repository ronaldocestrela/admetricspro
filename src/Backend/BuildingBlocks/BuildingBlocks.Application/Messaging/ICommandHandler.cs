using BuildingBlocks.Domain.Primitives;
using MediatR;

namespace BuildingBlocks.Application.Messaging;

/// <summary>
/// Defines a handler for commands returning a non-generic <see cref="Result"/>.
/// </summary>
/// <typeparam name="TCommand">The command type.</typeparam>
public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand, Result>
    where TCommand : ICommand;

/// <summary>
/// Defines a handler for commands returning a typed <see cref="Result{TResponse}"/>.
/// </summary>
/// <typeparam name="TCommand">The command type.</typeparam>
/// <typeparam name="TResponse">The payload type returned on success.</typeparam>
public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, Result<TResponse>>
    where TCommand : ICommand<TResponse>;
