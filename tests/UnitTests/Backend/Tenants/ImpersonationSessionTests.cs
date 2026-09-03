using FluentAssertions;
using Master.Domain.Tenants;

namespace UnitTests.Backend.Tenants;

/// <summary>
/// Unit tests for the <see cref="ImpersonationSession"/> domain entity.
/// </summary>
public sealed class ImpersonationSessionTests
{
    /// <summary>
    /// Verifies that Create fails when mandatory parameters are invalid.
    /// </summary>
    [Theory]
    [InlineData(null, "Valid reason at least 10 chars", 30)]
    [InlineData("", "Valid reason at least 10 chars", 30)]
    [InlineData("   ", "Valid reason at least 10 chars", 30)]
    public void Create_ShouldFail_WhenTicketIsInvalid(string? ticket, string reason, int duration)
    {
        // Act
        var result = ImpersonationSession.Create(
            TenantId.New(),
            Guid.NewGuid(),
            ticket!,
            reason,
            duration,
            DateTime.UtcNow);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ImpersonationErrors.InvalidTicket.Code);
    }

    /// <summary>
    /// Verifies that Create fails when reason has fewer than 10 characters.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Curto")]
    [InlineData("123456789")]
    public void Create_ShouldFail_WhenReasonIsTooShort(string? reason)
    {
        // Act
        var result = ImpersonationSession.Create(
            TenantId.New(),
            Guid.NewGuid(),
            "INC-12345",
            reason!,
            30,
            DateTime.UtcNow);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ImpersonationErrors.InvalidReason.Code);
    }

    /// <summary>
    /// Verifies successful session creation with active state.
    /// </summary>
    [Fact]
    public void Create_ShouldSucceed_WhenDataIsValid()
    {
        // Arrange
        var tenantId = TenantId.New();
        var adminId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        // Act
        var result = ImpersonationSession.Create(
            tenantId,
            adminId,
            "INC-12345",
            "Investigação de suporte técnico",
            45,
            now);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var session = result.Value;
        session.TenantId.Should().Be(tenantId);
        session.SuperAdminId.Should().Be(adminId);
        session.SupportTicketId.Should().Be("INC-12345");
        session.Reason.Should().Be("Investigação de suporte técnico");
        session.CreatedAtUtc.Should().Be(now);
        session.ExpiresAtUtc.Should().Be(now.AddMinutes(45));
        session.RevokedAtUtc.Should().BeNull();
        session.IsActiveAt(now.AddMinutes(10)).Should().BeTrue();
        session.IsActiveAt(now.AddMinutes(50)).Should().BeFalse();
    }

    /// <summary>
    /// Verifies session revocation behavior.
    /// </summary>
    [Fact]
    public void Revoke_ShouldDeactivateSession_WhenCalledBeforeExpiration()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var session = ImpersonationSession.Create(
            TenantId.New(),
            Guid.NewGuid(),
            "INC-12345",
            "Investigação de suporte técnico",
            30,
            now).Value;

        // Act
        var revokeResult = session.Revoke("Incidente concluído", now.AddMinutes(15));

        // Assert
        revokeResult.IsSuccess.Should().BeTrue();
        session.RevokedAtUtc.Should().Be(now.AddMinutes(15));
        session.RevokeReason.Should().Be("Incidente concluído");
        session.IsActiveAt(now.AddMinutes(16)).Should().BeFalse();
    }
}
