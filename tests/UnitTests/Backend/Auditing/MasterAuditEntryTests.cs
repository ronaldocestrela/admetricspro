using FluentAssertions;
using Master.Domain.Auditing;

namespace UnitTests.Backend.Auditing;

/// <summary>
/// Unit tests for the <see cref="MasterAuditEntry"/> immutable domain aggregate.
/// Validates business invariants, mandatory tagging for superadmin impersonation,
/// and append-only constraints.
/// </summary>
public sealed class MasterAuditEntryTests
{
    /// <summary>
    /// Verifies that Create fails when action or resource are empty.
    /// </summary>
    [Theory]
    [InlineData(null, "Tenant")]
    [InlineData("", "Tenant")]
    [InlineData("   ", "Tenant")]
    [InlineData("Tenant.Update", null)]
    [InlineData("Tenant.Update", "")]
    [InlineData("Tenant.Update", "   ")]
    public void Create_ShouldFail_WhenActionOrResourceIsInvalid(string? action, string? resource)
    {
        // Act
        var result = MasterAuditEntry.Record(
            tenantId: Guid.NewGuid(),
            action: action!,
            resource: resource!,
            resourceId: "123",
            details: "Sample update",
            isImpersonated: false,
            superAdminId: null,
            supportTicketId: null,
            impersonationSessionId: null,
            ipAddress: "127.0.0.1",
            createdAtUtc: DateTime.UtcNow);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Audit.InvalidActionOrResource");
    }

    /// <summary>
    /// Verifies that when operation is performed in impersonation mode,
    /// it fails if SuperAdminId, SupportTicketId, or ImpersonationSessionId are missing.
    /// </summary>
    [Fact]
    public void Create_ShouldFail_WhenImpersonated_AndSuperAdminMetadataIsMissing()
    {
        // Act: missing SuperAdminId
        var result1 = MasterAuditEntry.Record(
            tenantId: Guid.NewGuid(),
            action: "Tenant.Update",
            resource: "Tenant",
            resourceId: "123",
            details: "Updating details",
            isImpersonated: true,
            superAdminId: null,
            supportTicketId: "INC-84920",
            impersonationSessionId: Guid.NewGuid(),
            ipAddress: "127.0.0.1",
            createdAtUtc: DateTime.UtcNow);

        // Act: missing SupportTicketId
        var result2 = MasterAuditEntry.Record(
            tenantId: Guid.NewGuid(),
            action: "Tenant.Update",
            resource: "Tenant",
            resourceId: "123",
            details: "Updating details",
            isImpersonated: true,
            superAdminId: Guid.NewGuid(),
            supportTicketId: "",
            impersonationSessionId: Guid.NewGuid(),
            ipAddress: "127.0.0.1",
            createdAtUtc: DateTime.UtcNow);

        // Act: missing SessionId
        var result3 = MasterAuditEntry.Record(
            tenantId: Guid.NewGuid(),
            action: "Tenant.Update",
            resource: "Tenant",
            resourceId: "123",
            details: "Updating details",
            isImpersonated: true,
            superAdminId: Guid.NewGuid(),
            supportTicketId: "INC-84920",
            impersonationSessionId: null,
            ipAddress: "127.0.0.1",
            createdAtUtc: DateTime.UtcNow);

        // Assert
        result1.IsFailure.Should().BeTrue();
        result1.Error.Code.Should().Be("Audit.ImpersonationMetadataRequired");

        result2.IsFailure.Should().BeTrue();
        result2.Error.Code.Should().Be("Audit.ImpersonationMetadataRequired");

        result3.IsFailure.Should().BeTrue();
        result3.Error.Code.Should().Be("Audit.ImpersonationMetadataRequired");
    }

    /// <summary>
    /// Verifies that when isImpersonated is true, the entry automatically receives
    /// the mandatory tag 'performed_by_superadmin' and stores contextual metadata.
    /// </summary>
    [Fact]
    public void Create_ShouldIncludePerformedBySuperadminTag_WhenImpersonated()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var superAdminId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var ticket = "INC-84920";
        var utcNow = DateTime.UtcNow;

        // Act
        var result = MasterAuditEntry.Record(
            tenantId: tenantId,
            action: "Campaign.Pause",
            resource: "Campaign",
            resourceId: "cmp-456",
            details: "Pausa emergencial solicitada no suporte",
            isImpersonated: true,
            superAdminId: superAdminId,
            supportTicketId: ticket,
            impersonationSessionId: sessionId,
            ipAddress: "192.168.1.100",
            createdAtUtc: utcNow);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var entry = result.Value;
        entry.Id.Should().NotBeEmpty();
        entry.TenantId.Should().Be(tenantId);
        entry.Action.Should().Be("Campaign.Pause");
        entry.Resource.Should().Be("Campaign");
        entry.ResourceId.Should().Be("cmp-456");
        entry.Details.Should().Be("Pausa emergencial solicitada no suporte");
        entry.IsImpersonated.Should().BeTrue();
        entry.SuperAdminId.Should().Be(superAdminId);
        entry.SupportTicketId.Should().Be(ticket);
        entry.ImpersonationSessionId.Should().Be(sessionId);
        entry.IpAddress.Should().Be("192.168.1.100");
        entry.CreatedAtUtc.Should().Be(utcNow);
        entry.Tags.Should().Contain(MasterAuditTags.PerformedBySuperadmin);
    }

    /// <summary>
    /// Verifies that standard non-impersonated operations do not receive
    /// the 'performed_by_superadmin' tag by default.
    /// </summary>
    [Fact]
    public void Create_ShouldNotIncludePerformedBySuperadminTag_WhenNotImpersonated()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var utcNow = DateTime.UtcNow;

        // Act
        var result = MasterAuditEntry.Record(
            tenantId: tenantId,
            action: "Tenant.UpdateProfile",
            resource: "Tenant",
            resourceId: tenantId.ToString(),
            details: "Atualização de dados cadastrais",
            isImpersonated: false,
            superAdminId: null,
            supportTicketId: null,
            impersonationSessionId: null,
            ipAddress: "10.0.0.1",
            createdAtUtc: utcNow);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var entry = result.Value;
        entry.IsImpersonated.Should().BeFalse();
        entry.SuperAdminId.Should().BeNull();
        entry.SupportTicketId.Should().BeNull();
        entry.ImpersonationSessionId.Should().BeNull();
        entry.Tags.Should().NotContain(MasterAuditTags.PerformedBySuperadmin);
    }
}
