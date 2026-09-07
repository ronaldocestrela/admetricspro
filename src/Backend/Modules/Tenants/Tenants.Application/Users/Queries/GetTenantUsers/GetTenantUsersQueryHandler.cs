using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;
using Tenants.Application.Users.DTOs;
using Tenants.Application.Users.Repositories;

namespace Tenants.Application.Users.Queries.GetTenantUsers;

/// <summary>
/// Manipulador da consulta <see cref="GetTenantUsersQuery"/> que retorna a lista de colaboradores do inquilino.
/// </summary>
public sealed class GetTenantUsersQueryHandler : IQueryHandler<GetTenantUsersQuery, IReadOnlyList<TenantUserDto>>
{
    private readonly ITenantUserRepository _userRepository;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="GetTenantUsersQueryHandler"/>.
    /// </summary>
    /// <param name="userRepository">Repositório de colaboradores do inquilino.</param>
    public GetTenantUsersQueryHandler(ITenantUserRepository userRepository)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<TenantUserDto>>> Handle(
        GetTenantUsersQuery query,
        CancellationToken cancellationToken)
    {
        var users = await _userRepository.GetAllAsync(query.ActiveOnly, cancellationToken);

        var dtos = users.Select(u => new TenantUserDto(
            u.Id,
            u.FullName,
            u.Email,
            u.PhoneNumber,
            u.Role.ToString(),
            u.IsActive,
            u.CreatedAtUtc)).ToList();

        return Result<IReadOnlyList<TenantUserDto>>.Success(dtos);
    }
}
