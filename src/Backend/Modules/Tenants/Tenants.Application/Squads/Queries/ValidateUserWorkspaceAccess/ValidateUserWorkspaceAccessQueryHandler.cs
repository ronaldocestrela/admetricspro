using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;
using Tenants.Application.Squads.Services;

namespace Tenants.Application.Squads.Queries.ValidateUserWorkspaceAccess;

/// <summary>
/// Manipulador responsável por validar a permissão de acesso a um workspace via serviço de carteira.
/// </summary>
public sealed class ValidateUserWorkspaceAccessQueryHandler : IQueryHandler<ValidateUserWorkspaceAccessQuery, bool>
{
    private readonly IUserPortfolioService _portfolioService;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="ValidateUserWorkspaceAccessQueryHandler"/>.
    /// </summary>
    /// <param name="portfolioService">Serviço de governança de carteira.</param>
    public ValidateUserWorkspaceAccessQueryHandler(IUserPortfolioService portfolioService)
    {
        _portfolioService = portfolioService ?? throw new ArgumentNullException(nameof(portfolioService));
    }

    /// <inheritdoc />
    public async Task<Result<bool>> Handle(ValidateUserWorkspaceAccessQuery request, CancellationToken cancellationToken)
    {
        return await _portfolioService.HasAccessToWorkspaceAsync(request.UserId, request.WorkspaceId, cancellationToken);
    }
}
