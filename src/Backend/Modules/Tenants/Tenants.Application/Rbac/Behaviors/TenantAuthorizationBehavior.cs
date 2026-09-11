using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using BuildingBlocks.Application.Security;
using BuildingBlocks.Domain.Primitives;
using MediatR;
using Tenants.Application.Rbac.Attributes;
using Tenants.Application.Rbac.Models;
using Tenants.Application.Rbac.Services;

namespace Tenants.Application.Rbac.Behaviors;

/// <summary>
/// Pipeline behavior do MediatR para avaliação de autorização granular (RBAC) do inquilino.
/// Intercepta requisições decoradas com <see cref="RequireTenantPermissionAttribute"/>, avaliando
/// permissões, contexto de carteira e teto orçamentário antes da execução do manipulador.
/// </summary>
/// <typeparam name="TRequest">Tipo da requisição de comando ou consulta.</typeparam>
/// <typeparam name="TResponse">Tipo da resposta esperada baseada em Result.</typeparam>
public sealed class TenantAuthorizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly ConcurrentDictionary<Type, Func<Error, object>> GenericFailureFactoryCache = new();
    private static readonly IReadOnlyList<RequireTenantPermissionAttribute> RequiredAttributes =
        typeof(TRequest).GetCustomAttributes<RequireTenantPermissionAttribute>(inherit: true).ToList();

    private readonly ICurrentUserContext _currentUserContext;
    private readonly IPermissionEvaluator _permissionEvaluator;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="TenantAuthorizationBehavior{TRequest, TResponse}"/>.
    /// </summary>
    /// <param name="currentUserContext">Provedor contextual do usuário autenticado.</param>
    /// <param name="permissionEvaluator">Avaliador central de permissões do inquilino.</param>
    public TenantAuthorizationBehavior(
        ICurrentUserContext currentUserContext,
        IPermissionEvaluator permissionEvaluator)
    {
        _currentUserContext = currentUserContext ?? throw new ArgumentNullException(nameof(currentUserContext));
        _permissionEvaluator = permissionEvaluator ?? throw new ArgumentNullException(nameof(permissionEvaluator));
    }

    /// <inheritdoc />
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        if (RequiredAttributes.Count == 0)
        {
            return await next(cancellationToken);
        }

        var userId = _currentUserContext.UserId;
        if (!userId.HasValue || userId.Value == Guid.Empty)
        {
            return CreateFailureResult(
                Error.Unauthorized("Auth.Unauthorized", "Operação requer autenticação de usuário no inquilino."));
        }

        foreach (var attribute in RequiredAttributes)
        {
            var context = ResolveContext(request, attribute);

            var permissionResult = await _permissionEvaluator.HasPermissionAsync(
                userId.Value,
                attribute.Permission,
                context,
                cancellationToken);

            if (permissionResult.IsFailure)
            {
                return CreateFailureResult(permissionResult.Error);
            }

            if (!permissionResult.Value)
            {
                return CreateFailureResult(
                    Error.Forbidden("Auth.Forbidden", $"Usuário não possui a permissão requerida: {attribute.Permission}."));
            }
        }

        return await next(cancellationToken);
    }

    private static PermissionContext ResolveContext(TRequest request, RequireTenantPermissionAttribute attribute)
    {
        Guid? workspaceId = null;

        if (!string.IsNullOrWhiteSpace(attribute.WorkspaceIdProperty))
        {
            var prop = typeof(TRequest).GetProperty(attribute.WorkspaceIdProperty);
            if (prop?.GetValue(request) is Guid extractedGuid)
            {
                workspaceId = extractedGuid;
            }
        }

        return new PermissionContext(WorkspaceId: workspaceId);
    }

    private static TResponse CreateFailureResult(Error error)
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

        throw new InvalidOperationException(
            $"O tipo de resposta {responseType.Name} não implementa Result ou Result<T> para falha de autorização.");
    }

    private static Func<Error, object> BuildFailureFactory(Type resultType)
    {
        var valueType = resultType.GetGenericArguments()[0];
        var failureMethod = typeof(Result<>)
            .MakeGenericType(valueType)
            .GetMethod(nameof(Result.Failure), BindingFlags.Public | BindingFlags.Static, [typeof(Error)]);

        if (failureMethod is null)
        {
            throw new InvalidOperationException($"Não foi possível localizar o método Result<{valueType.Name}>.Failure.");
        }

        var errorParameter = Expression.Parameter(typeof(Error), "error");
        var callExpression = Expression.Call(failureMethod, errorParameter);
        var castExpression = Expression.Convert(callExpression, typeof(object));

        return Expression.Lambda<Func<Error, object>>(castExpression, errorParameter).Compile();
    }
}
