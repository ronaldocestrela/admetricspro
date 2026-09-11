using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using Tenants.Application.Audit.Repositories;
using Tenants.Application.Persistence;
using Tenants.Application.Users.Repositories;

namespace Tenants.Application.Users.Commands.ChangeTenantUserRole;

/// <summary>
/// Manipulador responsável por alterar o papel de um colaborador e registrar a trilha imutável em <see cref="TenantAuditLog"/>.
/// </summary>
public sealed class ChangeTenantUserRoleCommandHandler : ICommandHandler<ChangeTenantUserRoleCommand>
{
    private readonly ITenantUserRepository _userRepository;
    private readonly ITenantAuditLogRepository _auditRepository;
    private readonly ITenantUnitOfWork _unitOfWork;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="ChangeTenantUserRoleCommandHandler"/>.
    /// </summary>
    /// <param name="userRepository">Repositório de usuários do inquilino.</param>
    /// <param name="auditRepository">Repositório de auditoria do inquilino.</param>
    /// <param name="unitOfWork">Unidade de trabalho transacional.</param>
    public ChangeTenantUserRoleCommandHandler(
        ITenantUserRepository userRepository,
        ITenantAuditLogRepository auditRepository,
        ITenantUnitOfWork unitOfWork)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    /// <inheritdoc />
    public async Task<Result> Handle(ChangeTenantUserRoleCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null)
        {
            return Result.Failure(Error.NotFound("TenantUser.NotFound", "Colaborador não localizado no inquilino."));
        }

        if (!user.IsActive)
        {
            return Result.Failure(Error.Validation("TenantUser.Inactive", "Não é possível alterar papel de colaborador inativo."));
        }

        if (user.Role == command.NewRole)
        {
            return Result.Success();
        }

        var oldRole = user.Role;
        var changeResult = user.ChangeRole(command.NewRole);
        if (changeResult.IsFailure)
        {
            return changeResult;
        }

        var auditLogResult = TenantAuditLog.Create(
            Guid.NewGuid(),
            command.OperatorUserId,
            command.OperatorUserEmail,
            action: "User.RoleChanged",
            resource: "TenantUser",
            resourceId: user.Id.ToString(),
            details: $"Papel do colaborador '{user.FullName}' ({user.Email}) alterado de '{oldRole}' para '{command.NewRole}'.",
            ipAddress: command.IpAddress,
            createdAtUtc: DateTime.UtcNow);

        if (auditLogResult.IsFailure)
        {
            return Result.Failure(auditLogResult.Error);
        }

        await _auditRepository.AddAsync(auditLogResult.Value, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
