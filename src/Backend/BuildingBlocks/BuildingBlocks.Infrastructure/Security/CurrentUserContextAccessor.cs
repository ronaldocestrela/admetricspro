using System.Security.Claims;
using BuildingBlocks.Application.Security;
using BuildingBlocks.Domain.Tenants;
using Microsoft.AspNetCore.Http;

namespace BuildingBlocks.Infrastructure.Security;

/// <summary>
/// Implementação de <see cref="ICurrentUserContext"/> que inspeciona as claims do usuário autenticado no <see cref="HttpContext"/>.
/// </summary>
public sealed class CurrentUserContextAccessor : ICurrentUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="CurrentUserContextAccessor"/>.
    /// </summary>
    /// <param name="httpContextAccessor">Acessor de contexto HTTP.</param>
    public CurrentUserContextAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    /// <inheritdoc />
    public Guid? UserId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity is not { IsAuthenticated: true })
            {
                return null;
            }

            var sub = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? user.FindFirst("sub")?.Value;

            return Guid.TryParse(sub, out var parsedGuid) ? parsedGuid : null;
        }
    }

    /// <inheritdoc />
    public string? UserEmail
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            return user?.FindFirst(ClaimTypes.Email)?.Value
                   ?? user?.FindFirst("email")?.Value;
        }
    }

    /// <inheritdoc />
    public TenantRole? Role
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            var roleClaim = user?.FindFirst(ClaimTypes.Role)?.Value
                            ?? user?.FindFirst("role")?.Value;

            return Enum.TryParse<TenantRole>(roleClaim, ignoreCase: true, out var role)
                ? role
                : null;
        }
    }

    /// <inheritdoc />
    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true;
}
