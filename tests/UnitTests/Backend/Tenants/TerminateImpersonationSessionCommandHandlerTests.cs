using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using Master.Application.Auditing;
using Master.Application.Repositories;
using Master.Application.Tenants.Commands.TerminateImpersonationSession;
using Master.Domain.Auditing;
using Master.Domain.Tenants;
using NSubstitute;

namespace UnitTests.Backend.Tenants;

/// <summary>
/// Unit tests for <see cref="TerminateImpersonationSessionCommandHandler"/>.
/// </summary>
public sealed class TerminateImpersonationSessionCommandHandlerTests
{
    private readonly ITenantRepository _tenantRepository = Substitute.For<ITenantRepository>();
    private readonly IImpersonationSessionRepository _sessionRepository = Substitute.For<IImpersonationSessionRepository>();
    private readonly IMasterAuditService _auditService = Substitute.For<IMasterAuditService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly TerminateImpersonationSessionCommandHandler _handler;

    /// <summary>
    /// Initializes test dependencies.
    /// </summary>
    public TerminateImpersonationSessionCommandHandlerTests()
    {
        _handler = new TerminateImpersonationSessionCommandHandler(
            _tenantRepository,
            _sessionRepository,
            _auditService,
            _unitOfWork);
    }

    /// <summary>
    /// Verifies that handler fails when the tenant is not found.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldFail_WhenTenantDoesNotExist()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        _tenantRepository.GetByIdAsync(Arg.Any<TenantId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Tenant?>(null));

        var command = new TerminateImpersonationSessionCommand(tenantId, sessionId, "Fechamento");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Tenant.NotFound");
    }

    /// <summary>
    /// Verifies that handler fails when the session is not found.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldFail_WhenSessionDoesNotExist()
    {
        // Arrange
        var tenant = Tenant.Create("Beta Inc", "11222333000181", "beta").Value;
        _tenantRepository.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Tenant?>(tenant));

        _sessionRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ImpersonationSession?>(null));

        var command = new TerminateImpersonationSessionCommand(tenant.Id.Value, Guid.NewGuid(), "Fechamento");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ImpersonationErrors.SessionNotFound.Code);
    }

    /// <summary>
    /// Verifies that handler terminates active session, commits changes, and registers audit entry.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldRevokeSessionAndAudit_WhenValid()
    {
        // Arrange
        var tenant = Tenant.Create("Beta Inc", "11222333000181", "beta").Value;
        var superAdminId = Guid.NewGuid();
        var session = ImpersonationSession.Create(
            tenant.Id,
            superAdminId,
            "INC-12345",
            "Diagnóstico de métricas",
            30,
            DateTime.UtcNow).Value;

        _tenantRepository.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Tenant?>(tenant));

        _sessionRepository.GetByIdAsync(session.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ImpersonationSession?>(session));

        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        _auditService.RecordAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<Guid?>(),
            Arg.Any<string?>(),
            Arg.Any<IEnumerable<string>?>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<Guid>.Success(Guid.NewGuid())));

        var command = new TerminateImpersonationSessionCommand(tenant.Id.Value, session.Id, "Atendimento concluído pelo suporte");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        session.RevokedAtUtc.Should().NotBeNull();
        session.RevokeReason.Should().Be("Atendimento concluído pelo suporte");

        _sessionRepository.Received(1).Update(session);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        await _auditService.Received(1).RecordAsync(
            action: "Impersonation.Terminated",
            resource: "ImpersonationSession",
            resourceId: session.Id.ToString(),
            details: Arg.Is<string>(d => d.Contains("Atendimento concluído")),
            tenantId: tenant.Id.Value,
            ipAddress: null,
            additionalTags: Arg.Is<IEnumerable<string>>(tags => tags.Contains(MasterAuditTags.PerformedBySuperadmin)),
            cancellationToken: Arg.Any<CancellationToken>());
    }
}
