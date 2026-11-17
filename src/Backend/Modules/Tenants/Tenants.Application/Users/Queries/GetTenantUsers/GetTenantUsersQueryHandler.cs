using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using Tenants.Application.Users.DTOs;
using Tenants.Application.Users.Repositories;

namespace Tenants.Application.Users.Queries.GetTenantUsers;

/// <summary>
/// Manipulador da consulta <see cref="GetTenantUsersQuery"/> que retorna a lista de colaboradores do inquilino.
/// </summary>
public sealed class GetTenantUsersQueryHandler : IQueryHandler<GetTenantUsersQuery, IReadOnlyList<TenantUserDto>>
{
    private readonly ITenantUserRepository _userRepository;
    private readonly ITenantUserInvitationRepository _invitationRepository;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="GetTenantUsersQueryHandler"/>.
    /// </summary>
    /// <param name="userRepository">Repositório de colaboradores do inquilino.</param>
    /// <param name="invitationRepository">Repositório de convites pendentes.</param>
    public GetTenantUsersQueryHandler(
        ITenantUserRepository userRepository,
        ITenantUserInvitationRepository invitationRepository)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _invitationRepository = invitationRepository ?? throw new ArgumentNullException(nameof(invitationRepository));
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<TenantUserDto>>> Handle(
        GetTenantUsersQuery query,
        CancellationToken cancellationToken)
    {
        var users = await _userRepository.GetAllAsync(query.ActiveOnly, cancellationToken);
        var userIds = users.Select(u => u.Id).ToArray();
        var openInvitations = await _invitationRepository.GetOpenByUserIdsAsync(userIds, cancellationToken);
        var pendingUserIds = openInvitations.Select(i => i.UserId).ToHashSet();

        var dtos = users.Select(u =>
        {
            var accountStatus = ResolveAccountStatus(u, pendingUserIds);
            return new TenantUserDto(
                u.Id,
                u.FullName,
                u.Email,
                u.PhoneNumber,
                u.Role.ToString(),
                u.IsActive,
                u.CreatedAtUtc,
                accountStatus);
        }).ToList();

        return Result<IReadOnlyList<TenantUserDto>>.Success(dtos);
    }

    private static string ResolveAccountStatus(TenantUser user, ISet<Guid> pendingUserIds)
    {
        if (user.IsActive)
        {
            return TenantUserAccountStatus.Active.ToString();
        }

        return pendingUserIds.Contains(user.Id)
            ? TenantUserAccountStatus.PendingAcceptance.ToString()
            : TenantUserAccountStatus.Inactive.ToString();
    }
}