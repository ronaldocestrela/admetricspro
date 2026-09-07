using BuildingBlocks.Application.Security;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using NSubstitute;
using Tenants.Application.Persistence;
using Tenants.Application.Users.Commands.InviteTenantUser;
using Tenants.Application.Users.Repositories;

namespace UnitTests.Backend.Tenants;

/// <summary>
/// Testes unitários para o comando de convite/cadastro de novos colaboradores no inquilino (<see cref="InviteTenantUserCommandHandler"/>).
/// </summary>
public sealed class InviteTenantUserCommandHandlerTests
{
    private readonly ITenantUserRepository _userRepository = Substitute.For<ITenantUserRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly ITenantUnitOfWork _unitOfWork = Substitute.For<ITenantUnitOfWork>();

    private InviteTenantUserCommandHandler CreateHandler()
    {
        return new InviteTenantUserCommandHandler(
            _userRepository,
            _passwordHasher,
            _unitOfWork);
    }

    /// <summary>
    /// Valida que ao convidar um colaborador com dados válidos, o usuário é persistido com sucesso.
    /// </summary>
    [Fact]
    public async Task Handle_WithValidData_ShouldCreateUserAndReturnId()
    {
        // Arrange
        _userRepository.GetByEmailAsync("gestor@agencia.com", Arg.Any<CancellationToken>())
            .Returns((TenantUser?)null);
        _passwordHasher.HashPassword(Arg.Any<string>())
            .Returns("secure_hash");

        var handler = CreateHandler();
        var command = new InviteTenantUserCommand("Gestor Silva", "gestor@agencia.com", "MediaManager");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);
        await _userRepository.Received(1).AddAsync(Arg.Is<TenantUser>(u => u.Email == "gestor@agencia.com" && u.Role == TenantRole.MediaManager), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Valida que ao tentar convidar com e-mail já existente no inquilino, retorna conflito.
    /// </summary>
    [Fact]
    public async Task Handle_WhenEmailAlreadyExists_ShouldReturnConflict()
    {
        // Arrange
        var existingUser = TenantUser.Create(Guid.NewGuid(), "Outro Usuário", "gestor@agencia.com", null, "hash", TenantRole.Analyst).Value;
        _userRepository.GetByEmailAsync("gestor@agencia.com", Arg.Any<CancellationToken>())
            .Returns(existingUser);

        var handler = CreateHandler();
        var command = new InviteTenantUserCommand("Gestor Silva", "gestor@agencia.com", "MediaManager");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("TenantUser.EmailAlreadyInUse", result.Error.Code);
        await _userRepository.DidNotReceive().AddAsync(Arg.Any<TenantUser>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Valida que papel inválido retorna erro de validação.
    /// </summary>
    [Fact]
    public async Task Handle_WithInvalidRole_ShouldReturnValidationFailure()
    {
        // Arrange
        var handler = CreateHandler();
        var command = new InviteTenantUserCommand("Gestor Silva", "gestor@agencia.com", "InvalidRole");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("TenantUser.InvalidRole", result.Error.Code);
    }
}
