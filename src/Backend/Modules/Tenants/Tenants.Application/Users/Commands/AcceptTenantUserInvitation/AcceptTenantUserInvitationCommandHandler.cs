using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;
using Tenants.Application.Users.Services;

namespace Tenants.Application.Users.Commands.AcceptTenantUserInvitation;

/// <summary>
/// Manipulador público de aceite de convite de colaborador.
/// </summary>
public sealed class AcceptTenantUserInvitationCommandHandler : ICommandHandler<AcceptTenantUserInvitationCommand>
{
    private readonly ITenantInvitationPublicGateway _publicGateway;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="AcceptTenantUserInvitationCommandHandler"/>.
    /// </summary>
    /// <param name="publicGateway">Gateway de operações públicas de convite.</param>
    public AcceptTenantUserInvitationCommandHandler(ITenantInvitationPublicGateway publicGateway)
    {
        _publicGateway = publicGateway ?? throw new ArgumentNullException(nameof(publicGateway));
    }

    /// <inheritdoc />
    public async Task<Result> Handle(AcceptTenantUserInvitationCommand command, CancellationToken cancellationToken)
    {
        if (!string.Equals(command.Password, command.ConfirmPassword, StringComparison.Ordinal))
        {
            return Result.Failure(
                Error.Validation("TenantUser.PasswordMismatch", "As senhas não coincidem."));
        }

        var passwordPolicy = TenantPasswordPolicy.Validate(command.Password);
        if (passwordPolicy.IsFailure)
        {
            return passwordPolicy;
        }

        return await _publicGateway.AcceptAsync(
            command.Token,
            command.Subdomain,
            command.Password,
            cancellationToken);
    }
}