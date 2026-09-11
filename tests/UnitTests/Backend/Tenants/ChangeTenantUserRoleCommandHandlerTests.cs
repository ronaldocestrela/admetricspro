using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using FluentAssertions;
using NSubstitute;
using Tenants.Application.Audit.Repositories;
using Tenants.Application.Persistence;
using Tenants.Application.Users.Commands.ChangeTenantUserRole;
using Tenants.Application.Users.Repositories;
using Xunit;

namespace UnitTests.Backend.Tenants;

/// <summary>
/// Testes unitários para <see cref="ChangeTenantUserRoleCommandHandler"/> validando a alteração
/// de papel de colaborador e a gravação imutável do log de auditoria.
/// </summary>
public sealed class ChangeTenantUserRoleCommandHandlerTests
{
    private readonly ITenantUserRepository _userRepository = Substitute.For<ITenantUserRepository>();
    private readonly ITenantAuditLogRepository _auditRepository = Substitute.For<ITenantAuditLogRepository>();
    private readonly ITenantUnitOfWork _unitOfWork = Substitute.For<ITenantUnitOfWork>();
    private readonly ChangeTenantUserRoleCommandHandler _handler;

    /// <summary>
    /// Inicializa a suíte de testes com os mocks das dependências.
    /// </summary>
    public ChangeTenantUserRoleCommandHandlerTests()
    {
        _handler = new ChangeTenantUserRoleCommandHandler(
            _userRepository,
            _auditRepository,
            _unitOfWork);
    }

    /// <summary>
    /// Valida que a alteração de papel é executada com sucesso e um log de auditoria é gravado.
    /// </summary>
    [Fact]
    public async Task Handle_ComDadosValidos_DeveAlterarPapelEGravarAuditoria()
    {
        // Arrange
        var targetUserId = Guid.NewGuid();
        var operatorId = Guid.NewGuid();
        var targetUser = TenantUser.Create(
            targetUserId,
            "Carlos Operador",
            "carlos@agencia.com",
            null,
            "hash",
            TenantRole.MediaManager).Value;

        _userRepository.GetByIdAsync(targetUserId, Arg.Any<CancellationToken>()).Returns(targetUser);

        var command = new ChangeTenantUserRoleCommand(
            targetUserId,
            TenantRole.SquadLeader,
            operatorId,
            "admin@agencia.com",
            "192.168.1.1");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        targetUser.Role.Should().Be(TenantRole.SquadLeader);

        await _auditRepository.Received(1).AddAsync(
            Arg.Is<TenantAuditLog>(log =>
                log.UserId == operatorId &&
                log.Action == "User.RoleChanged" &&
                log.ResourceId == targetUserId.ToString()),
            Arg.Any<CancellationToken>());

        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Valida que tentar alterar o papel de um usuário inexistente retorna erro NotFound.
    /// </summary>
    [Fact]
    public async Task Handle_QuandoUsuarioNaoExiste_DeveRetornarNotFound()
    {
        // Arrange
        var targetUserId = Guid.NewGuid();
        _userRepository.GetByIdAsync(targetUserId, Arg.Any<CancellationToken>()).Returns((TenantUser?)null);

        var command = new ChangeTenantUserRoleCommand(
            targetUserId,
            TenantRole.Admin,
            Guid.NewGuid(),
            "admin@agencia.com",
            "127.0.0.1");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("TenantUser.NotFound");
        await _auditRepository.DidNotReceive().AddAsync(Arg.Any<TenantAuditLog>(), Arg.Any<CancellationToken>());
    }
}
