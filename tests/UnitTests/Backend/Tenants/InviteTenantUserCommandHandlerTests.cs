using BuildingBlocks.Application.Emails;
using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Application.Security;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using BuildingBlocks.Infrastructure.Emails;
using BuildingBlocks.Infrastructure.MultiTenancy;
using Microsoft.Extensions.Options;
using NSubstitute;
using Tenants.Application.Persistence;
using Tenants.Application.Users.Commands.InviteTenantUser;
using Tenants.Application.Users.Options;
using Tenants.Application.Users.Repositories;
using Master.Application.Repositories;
using Master.Domain.Tenants;
using Tenants.Application.Users.Services;

namespace UnitTests.Backend.Tenants;

/// <summary>
/// Testes unitários para o comando de convite de colaboradores (<see cref="InviteTenantUserCommandHandler"/>).
/// </summary>
public sealed class InviteTenantUserCommandHandlerTests
{
    private readonly ITenantUserRepository _userRepository = Substitute.For<ITenantUserRepository>();
    private readonly ITenantUserInvitationRepository _invitationRepository = Substitute.For<ITenantUserInvitationRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly ITenantUnitOfWork _unitOfWork = Substitute.For<ITenantUnitOfWork>();
    private readonly IEmailSender _emailSender = new InMemoryEmailSender();
    private readonly ITenantInvitationEmailComposer _emailComposer = Substitute.For<ITenantInvitationEmailComposer>();
    private readonly ITenantInvitationTokenService _tokenService = new global::Tenants.Infrastructure.Users.TenantInvitationTokenService();
    private readonly ICurrentUserContext _currentUserContext = Substitute.For<ICurrentUserContext>();
    private readonly ITenantContextAccessor _tenantContextAccessor = new TenantContextAccessor();
    private readonly ITenantRepository _tenantRepository = Substitute.For<ITenantRepository>();

    private InviteTenantUserCommandHandler CreateHandler(TenantRole invokerRole = TenantRole.Owner)
    {
        _tenantContextAccessor.TenantContext = TenantContext.Create(Guid.NewGuid(), "vanguarda", TenantResolutionSource.Header);
        _currentUserContext.Role.Returns(invokerRole);
        _currentUserContext.UserId.Returns(Guid.NewGuid());

        _emailComposer.ComposeInvitationEmail(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<DateTime>())
            .Returns(call =>
            {
                var email = call.ArgAt<string>(0);
                return EmailMessage.Create(email, "Convite", "<p>Convite</p>", "Convite");
            });

        return new InviteTenantUserCommandHandler(
            _userRepository,
            _invitationRepository,
            _passwordHasher,
            _unitOfWork,
            _emailSender,
            _emailComposer,
            _tokenService,
            _currentUserContext,
            _tenantContextAccessor,
            _tenantRepository,
            Options.Create(new TenantInvitationOptions()));
    }

    /// <summary>
    /// Valida convite com persistência pendente e envio de e-mail.
    /// </summary>
    [Fact]
    public async Task Handle_WithValidData_ShouldCreatePendingUserAndInvitation()
    {
        _userRepository.GetByEmailAsync("gestor@agencia.com", Arg.Any<CancellationToken>())
            .Returns((TenantUser?)null);
        _passwordHasher.HashPassword(Arg.Any<string>())
            .Returns("secure_hash");

        var handler = CreateHandler();
        var command = new InviteTenantUserCommand("Gestor Silva", "gestor@agencia.com", "MediaManager");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value.UserId);
        Assert.True(result.Value.EmailDispatched);
        await _userRepository.Received(1).AddAsync(
            Arg.Is<TenantUser>(u => u.Email == "gestor@agencia.com" && u.Role == TenantRole.MediaManager && !u.IsActive),
            Arg.Any<CancellationToken>());
        await _invitationRepository.Received(1).AddAsync(Arg.Any<TenantUserInvitation>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Valida conflito de e-mail duplicado.
    /// </summary>
    [Fact]
    public async Task Handle_WhenEmailAlreadyExists_ShouldReturnConflict()
    {
        var existingUser = TenantUser.Create(Guid.NewGuid(), "Outro Usuário", "gestor@agencia.com", null, "hash", TenantRole.Analyst).Value;
        _userRepository.GetByEmailAsync("gestor@agencia.com", Arg.Any<CancellationToken>())
            .Returns(existingUser);

        var handler = CreateHandler();
        var command = new InviteTenantUserCommand("Gestor Silva", "gestor@agencia.com", "MediaManager");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("TenantUser.EmailAlreadyInUse", result.Error.Code);
    }

    /// <summary>
    /// Valida bloqueio de atribuição do papel Owner.
    /// </summary>
    [Fact]
    public async Task Handle_WithOwnerRole_ShouldReturnValidationFailure()
    {
        var handler = CreateHandler();
        var command = new InviteTenantUserCommand("Gestor Silva", "gestor@agencia.com", "Owner");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("TenantUser.OwnerNotAssignable", result.Error.Code);
    }

    /// <summary>
    /// Valida proibição para papéis que não são Owner/Admin.
    /// </summary>
    [Fact]
    public async Task Handle_WhenInvokerIsNotOwnerOrAdmin_ShouldReturnForbidden()
    {
        var handler = CreateHandler(TenantRole.MediaManager);
        var command = new InviteTenantUserCommand("Gestor Silva", "gestor@agencia.com", "Analyst");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("TenantUser.InviteForbidden", result.Error.Code);
    }
}