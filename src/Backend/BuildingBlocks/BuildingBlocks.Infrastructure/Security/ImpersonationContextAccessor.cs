using System.Security.Claims;
using BuildingBlocks.Application.Security;
using Microsoft.AspNetCore.Http;

namespace BuildingBlocks.Infrastructure.Security;

/// <summary>
/// Implements <see cref="IImpersonationContextAccessor"/> by inspecting claims in the active HTTP context.
/// </summary>
public sealed class ImpersonationContextAccessor : IImpersonationContextAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImpersonationContextAccessor"/> class.
    /// </summary>
    /// <param name="httpContextAccessor">HTTP context accessor.</param>
    public ImpersonationContextAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public IImpersonationContext Current
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user is null || user.Identity is not { IsAuthenticated: true })
            {
                return ImpersonationContext.Inactive;
            }

            var isImpersonatedClaim = user.FindFirst(ImpersonationClaims.IsImpersonated)?.Value;
            var isImpersonated = string.Equals(isImpersonatedClaim, "true", StringComparison.OrdinalIgnoreCase);

            if (!isImpersonated)
            {
                return ImpersonationContext.Inactive;
            }

            Guid? originalAdminId = null;
            if (Guid.TryParse(user.FindFirst(ImpersonationClaims.OriginalSuperAdminId)?.Value, out var adminGuid))
            {
                originalAdminId = adminGuid;
            }

            Guid? targetTenantId = null;
            if (Guid.TryParse(user.FindFirst(ImpersonationClaims.TenantId)?.Value, out var tenantGuid))
            {
                targetTenantId = tenantGuid;
            }

            Guid? sessionId = null;
            if (Guid.TryParse(user.FindFirst(ImpersonationClaims.SessionId)?.Value, out var sessionGuid))
            {
                sessionId = sessionGuid;
            }

            var ticketId = user.FindFirst(ImpersonationClaims.SupportTicketId)?.Value;

            return new ImpersonationContext(
                isImpersonated: true,
                originalSuperAdminId: originalAdminId,
                supportTicketId: ticketId,
                sessionId: sessionId,
                targetTenantId: targetTenantId);
        }
    }

    private sealed class ImpersonationContext : IImpersonationContext
    {
        public static readonly ImpersonationContext Inactive = new(
            isImpersonated: false,
            originalSuperAdminId: null,
            supportTicketId: null,
            sessionId: null,
            targetTenantId: null);

        public ImpersonationContext(
            bool isImpersonated,
            Guid? originalSuperAdminId,
            string? supportTicketId,
            Guid? sessionId,
            Guid? targetTenantId)
        {
            IsImpersonated = isImpersonated;
            OriginalSuperAdminId = originalSuperAdminId;
            SupportTicketId = supportTicketId;
            SessionId = sessionId;
            TargetTenantId = targetTenantId;
        }

        public bool IsImpersonated { get; }
        public Guid? OriginalSuperAdminId { get; }
        public string? SupportTicketId { get; }
        public Guid? SessionId { get; }
        public Guid? TargetTenantId { get; }
    }
}
