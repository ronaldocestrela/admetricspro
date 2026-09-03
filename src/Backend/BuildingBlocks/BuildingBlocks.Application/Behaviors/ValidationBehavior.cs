using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using BuildingBlocks.Domain.Primitives;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace BuildingBlocks.Application.Behaviors;

/// <summary>
/// Intercepts MediatR requests to execute FluentValidation validators, converting validation failures
/// into typed <see cref="Result"/> or <see cref="Result{TValue}"/> failures without throwing exceptions.
/// </summary>
/// <typeparam name="TRequest">The incoming request type.</typeparam>
/// <typeparam name="TResponse">The expected response type, implementing <see cref="Result"/>.</typeparam>
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly ConcurrentDictionary<Type, Func<Error, object>> GenericFailureFactoryCache = new();
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="validators">The collection of validators registered for <typeparamref name="TRequest"/>.</param>
    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators ?? throw new ArgumentNullException(nameof(validators));
    }

    /// <summary>
    /// Handles the request by running validation rules and converting any failures into a failed <see cref="Result"/>.
    /// </summary>
    /// <param name="request">The incoming request.</param>
    /// <param name="next">The continuation delegate for the next step in the pipeline.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A response representing success or a validation failure.</returns>
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        if (!_validators.Any())
        {
            return await next(cancellationToken);
        }

        var context = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .Select(f => (f.PropertyName, f.ErrorMessage))
            .ToList();

        if (failures.Count == 0)
        {
            return await next(cancellationToken);
        }

        var validationError = ValidationError.Create(failures);
        return CreateFailureResult(validationError);
    }

    private static TResponse CreateFailureResult(ValidationError error)
    {
        var responseType = typeof(TResponse);

        if (responseType == typeof(Result))
        {
            return (TResponse)(object)Result.Failure(error);
        }

        if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var factory = GenericFailureFactoryCache.GetOrAdd(responseType, BuildFailureFactory);
            return (TResponse)factory(error);
        }

        throw new ValidationException(
            "Validation failed for a request whose response type does not implement Result or Result<T>.",
            error.Errors.SelectMany(kvp => kvp.Value.Select(msg => new ValidationFailure(kvp.Key, msg))));
    }

    private static Func<Error, object> BuildFailureFactory(Type resultType)
    {
        var valueType = resultType.GetGenericArguments()[0];
        var failureMethod = typeof(Result<>)
            .MakeGenericType(valueType)
            .GetMethod(nameof(Result.Failure), BindingFlags.Public | BindingFlags.Static, new[] { typeof(Error) });

        if (failureMethod is null)
        {
            throw new InvalidOperationException($"Unable to locate Result<{valueType.Name}>.Failure method.");
        }

        var errorParameter = Expression.Parameter(typeof(Error), "error");
        var callExpression = Expression.Call(failureMethod, errorParameter);
        var castExpression = Expression.Convert(callExpression, typeof(object));

        return Expression.Lambda<Func<Error, object>>(castExpression, errorParameter).Compile();
    }
}
