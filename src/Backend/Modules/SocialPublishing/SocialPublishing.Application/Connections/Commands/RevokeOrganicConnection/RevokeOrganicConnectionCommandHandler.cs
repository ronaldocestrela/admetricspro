using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Domain.Primitives;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Domain;

namespace SocialPublishing.Application.Connections.Commands.RevokeOrganicConnection;

/// <summary>
/// Manipulador do comando <see cref="RevokeOrganicConnectionCommand"/>.
/// </summary>
public sealed class RevokeOrganicConnectionCommandHandler : ICommandHandler<RevokeOrganicConnectionCommand>
{
    private readonly IOrganicConnectionRepository _connectionRepository;
    private readonly ISocialPublishingUnitOfWork _unitOfWork;
    private readonly ITenantContextAccessor _tenantContextAccessor;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="RevokeOrganicConnectionCommandHandler"/>.
    /// </summary>
    /// <param name="connectionRepository">Repositório de conexões orgânicas.</param>
    /// <param name="unitOfWork">Unit of work transacional do módulo.</param>
    /// <param name="tenantContextAccessor">Contexto de tenant da requisição.</param>
    public RevokeOrganicConnectionCommandHandler(
        IOrganicConnectionRepository connectionRepository,
        ISocialPublishingUnitOfWork unitOfWork,
        ITenantContextAccessor tenantContextAccessor)
    {
        _connectionRepository = connectionRepository ?? throw new ArgumentNullException(nameof(connectionRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _tenantContextAccessor = tenantContextAccessor ?? throw new ArgumentNullException(nameof(tenantContextAccessor));
    }

    /// <inheritdoc />
    public async Task<Result> Handle(RevokeOrganicConnectionCommand request, CancellationToken cancellationToken)
    {
        if (request.WorkspaceId == Guid.Empty)
        {
            return Result.Failure(SocialErrors.WorkspaceRequired);
        }

        if (request.ConnectionId == Guid.Empty)
        {
            return Result.Failure(SocialErrors.ConnectionNotFound);
        }

        var tenantId = _tenantContextAccessor.TenantContext?.TenantId;
        if (!tenantId.HasValue || tenantId.Value == Guid.Empty)
        {
            return Result.Failure(SocialErrors.TenantRequired);
        }

        var connection = await _connectionRepository.GetByIdAsync(request.ConnectionId, cancellationToken);
        if (connection is null || connection.WorkspaceId != request.WorkspaceId)
        {
            return Result.Failure(SocialErrors.ConnectionNotFound);
        }

        var revokeResult = connection.Revoke();
        if (revokeResult.IsFailure)
        {
            return revokeResult;
        }

        await _connectionRepository.UpdateAsync(connection, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
