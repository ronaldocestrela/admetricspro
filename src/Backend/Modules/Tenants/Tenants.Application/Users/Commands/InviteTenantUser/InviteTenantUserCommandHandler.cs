using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Security;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using Tenants.Application.Persistence;
using Tenants.Application.Users.Repositories;

namespace Tenants.Application.Users.Commands.InviteTenantUser;

/// <summary>
/// Manipulador do comando <see cref="InviteTenantUserCommand"/> que valida invariantes e cadastra o novo colaborador.
/// </summary>
public sealed class InviteTenantUserCommandHandler : ICommandHandler<InviteTenantUserCommand, Guid>
{
    private readonly ITenantUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITenantUnitOfWork _unitOfWork;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="InviteTenantUserCommandHandler"/>.
    /// </summary>
    /// <param name="userRepository">Repositório de usuários do inquilino.</param>
    /// <param name="passwordHasher">Serviço de hashing criptográfico de senhas.</param>
    /// <param name="unitOfWork">Unidade de trabalho transacional do inquilino.</param>
    public InviteTenantUserCommandHandler(
        ITenantUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ITenantUnitOfWork unitOfWork)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(InviteTenantUserCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Role) || !Enum.TryParse<TenantRole>(command.Role.Trim(), true, out var role))
        {
            return Result<Guid>.Failure(
                Error.Validation("TenantUser.InvalidRole", $"O papel '{command.Role}' não é um nível de governança válido no inquilino."));
        }

        var normalizedEmail = (command.Email ?? string.Empty).Trim().ToLowerInvariant();
        var existingUser = await _userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (existingUser is not null)
        {
            return Result<Guid>.Failure(
                Error.Conflict("TenantUser.EmailAlreadyInUse", $"Já existe um colaborador cadastrado com o e-mail '{command.Email}'."));
        }

        // Gera uma credencial inicial temporária segura
        var initialPassword = $"Temp@{Guid.NewGuid():N}";
        var passwordHash = _passwordHasher.HashPassword(initialPassword);

        var userResult = TenantUser.Create(
            Guid.NewGuid(),
            command.FullName,
            normalizedEmail,
            command.PhoneNumber,
            passwordHash,
            role);

        if (userResult.IsFailure)
        {
            return Result<Guid>.Failure(userResult.Error);
        }

        var user = userResult.Value;
        await _userRepository.AddAsync(user, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result<Guid>.Success(user.Id);
    }
}
