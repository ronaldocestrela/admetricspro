using System.Security.Claims;
using BuildingBlocks.Application.Security;
using BuildingBlocks.Infrastructure.Security;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using NSubstitute;

namespace UnitTests.Backend.Security;

/// <summary>
/// Unit tests for <see cref="ImpersonationContextAccessor"/>.
/// </summary>
public sealed class ImpersonationContextAccessorTests
{
    private readonly IHttpContextAccessor _httpContextAccessor = Substitute.For<IHttpContextAccessor>();

    /// <summary>
    /// Verifies that Current returns inactive impersonation when HttpContext is null or user is unauthenticated.
    /// </summary>
    [Fact]
    public void Current_ShouldReturnInactive_WhenUserIsNotAuthenticated()
    {
        // Arrange
        _httpContextAccessor.HttpContext.Returns((HttpContext?)null);
        var accessor = new ImpersonationContextAccessor(_httpContextAccessor);

        // Act
        var context = accessor.Current;

        // Assert
        context.IsImpersonated.Should().BeFalse();
        context.OriginalSuperAdminId.Should().BeNull();
        context.SupportTicketId.Should().BeNull();
        context.SessionId.Should().BeNull();
        context.TargetTenantId.Should().BeNull();
    }

    /// <summary>
    /// Verifies that Current parses and returns all contextual impersonation claims when present in user identity.
    /// </summary>
    [Fact]
    public void Current_ShouldReturnActiveImpersonation_WhenUserHasImpersonationClaims()
    {
        // Arrange
        var superAdminId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        const string ticketId = "INC-12345";

        var claims = new[]
        {
            new Claim(ImpersonationClaims.IsImpersonated, "true"),
            new Claim(ImpersonationClaims.OriginalSuperAdminId, superAdminId.ToString()),
            new Claim(ImpersonationClaims.TenantId, tenantId.ToString()),
            new Claim(ImpersonationClaims.SessionId, sessionId.ToString()),
            new Claim(ImpersonationClaims.SupportTicketId, ticketId)
        };

        var identity = new ClaimsIdentity(claims, "Bearer");
        var user = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = user };
        _httpContextAccessor.HttpContext.Returns(httpContext);

        var accessor = new ImpersonationContextAccessor(_httpContextAccessor);

        // Act
        var context = accessor.Current;

        // Assert
        context.IsImpersonated.Should().BeTrue();
        context.OriginalSuperAdminId.Should().Be(superAdminId);
        context.TargetTenantId.Should().Be(tenantId);
        context.SessionId.Should().Be(sessionId);
        context.SupportTicketId.Should().Be(ticketId);
    }
}
