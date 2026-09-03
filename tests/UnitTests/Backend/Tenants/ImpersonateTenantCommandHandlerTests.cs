using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using Master.Application.Repositories;
using Master.Application.Services;
using Master.Application.Tenants.Commands.ImpersonateTenant;
using Master.Domain.Tenants;
using NSubstitute;

namespace UnitTests.Backend.Tenants;

/// <summary>
/// Unit tests for <see cref="ImpersonateTenantCommandHandler"/>.
/// </summary>
public sealed class ImpersonateTenantCommandHandlerTests
{
    private readonly ITenantRepository _tenantRepository = Substitute.For<ITenantRepository>();
    private readonly IImpersonationSessionRepository _sessionRepository = Substitute.For<IImpersonationSessionRepository>();
    private readonly IImpersonationTokenService _tokenService = Substitute.For<IImpersonationTokenService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    /// <summary>
    /// Verifies failure when the requested tenant does not exist in the catalog.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenTenantDoesNotExist()
    {
        // Arrange
        var handler = new ImpersonateTenantCommandHandler(
            _tenantRepository,
            _sessionRepository,
            _tokenService,
            _unitOfWork);

        var tenantId = TenantId.New();
        var command = new ImpersonateTenantCommand(
            tenantId,
            Guid.NewGuid(),
            "INC-9999",
            "Investigação de erro de sincronização de campanhas",
            30);

        _tenantRepository.GetByIdAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns((Tenant?)null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Tenant.NotFound");
        await _sessionRepository.DidNotReceive().AddAsync(Arg.Any<ImpersonationSession>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies failure when the requested tenant is suspended or inactive.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldReturnTenantInactive_WhenTenantIsSuspended()
    {
        // Arrange
        var handler = new ImpersonateTenantCommandHandler(
            _tenantRepository,
            _sessionRepository,
            _tokenService,
            _unitOfWork);

        var tenant = Tenant.Create("Alpha Corp", "11222333000181", "alpha").Value;
        tenant.Suspend("Inadimplência financeira");

        var command = new ImpersonateTenantCommand(
            tenant.Id,
            Guid.NewGuid(),
            "INC-9999",
            "Investigação de suporte técnico",
            30);

        _tenantRepository.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>())
            .Returns(tenant);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ImpersonationErrors.TenantInactive.Code);
        await _sessionRepository.DidNotReceive().AddAsync(Arg.Any<ImpersonationSession>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies failure when token generation fails in the security service.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenTokenGenerationFails()
    {
        // Arrange
        var handler = new ImpersonateTenantCommandHandler(
            _tenantRepository,
            _sessionRepository,
            _tokenService,
            _unitOfWork);

        var tenant = Tenant.Create("Alpha Corp", "11222333000181", "alpha").Value;
        var command = new ImpersonateTenantCommand(
            tenant.Id,
            Guid.NewGuid(),
            "INC-9999",
            "Investigação de suporte técnico em campanhas",
            30);

        _tenantRepository.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>())
            .Returns(tenant);

        _tokenService.GenerateToken(Arg.Any<ImpersonationSession>(), tenant)
            .Returns(Result<string>.Failure(Error.Failure("Token.GenerationFailed", "Signing key unavailable.")));

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Token.GenerationFailed");
        await _sessionRepository.DidNotReceive().AddAsync(Arg.Any<ImpersonationSession>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies successful session creation, JWT issuance and database persistence when input is valid.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldIssueTokenAndPersistSession_WhenCommandIsValid()
    {
        // Arrange
        var handler = new ImpersonateTenantCommandHandler(
            _tenantRepository,
            _sessionRepository,
            _tokenService,
            _unitOfWork);

        var tenant = Tenant.Create("Alpha Corp", "11222333000181", "alpha").Value;
        var superAdminId = Guid.NewGuid();
        var command = new ImpersonateTenantCommand(
            tenant.Id,
            superAdminId,
            "INC-12345",
            "Investigação de suporte técnico em campanhas",
            45);

        _tenantRepository.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>())
            .Returns(tenant);

        const string generatedJwt = "header.payload.signature";
        _tokenService.GenerateToken(Arg.Any<ImpersonationSession>(), tenant)
            .Returns(Result<string>.Success(generatedJwt));

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var response = result.Value;
        response.AccessToken.Should().Be(generatedJwt);
        response.TokenType.Should().Be("Bearer");
        response.TenantId.Should().Be(tenant.Id.Value);
        response.TenantName.Should().Be("Alpha Corp");
        response.SuperAdminId.Should().Be(superAdminId);
        response.SupportTicketId.Should().Be("INC-12345");
        response.ExpiresInSeconds.Should().BeGreaterThan(0);

        await _sessionRepository.Received(1).AddAsync(
            Arg.Is<ImpersonationSession>(s =>
                s.TenantId == tenant.Id &&
                s.SuperAdminId == superAdminId &&
                s.SupportTicketId == "INC-12345" &&
                s.Reason == "Investigação de suporte técnico em campanhas"),
            Arg.Any<CancellationToken>());

        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}
