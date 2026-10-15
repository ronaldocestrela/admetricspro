using BuildingBlocks.Domain.Integrations;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Infrastructure.Persistence;
using Integrations.Application.Persistence;
using Integrations.Domain.Meta;
using Microsoft.EntityFrameworkCore;

namespace Integrations.Infrastructure.Meta;

/// <summary>Reserva durável que evita duplicações inclusive entre instâncias do backend.</summary>
/// <param name="contexts">Acessor scoped do banco do tenant.</param>
/// <param name="unitOfWork">Unidade de trabalho do módulo.</param>
public sealed class MetaOperationRepository(ITenantDbContextAccessor contexts, IIntegrationsUnitOfWork unitOfWork) : IMetaOperationRepository
{
    /// <inheritdoc />
    public async Task<Result<bool>> TryBeginAsync(MetaApiOperation operation, CancellationToken cancellationToken)
    {
        var context = await contexts.GetDbContextAsync(cancellationToken);
        if (context.IsFailure) return Result<bool>.Failure(context.Error);
        if (await context.Value.MetaApiOperations.AnyAsync(o => o.Id == operation.Id, cancellationToken)) return Result<bool>.Success(false);
        await context.Value.MetaApiOperations.AddAsync(operation, cancellationToken);
        try { await unitOfWork.CommitAsync(cancellationToken); return Result<bool>.Success(true); }
        catch (DbUpdateException)
        {
            context.Value.Entry(operation).State = EntityState.Detached;
            if (await context.Value.MetaApiOperations.AnyAsync(o => o.Id == operation.Id, cancellationToken)) return Result<bool>.Success(false);
            return Result<bool>.Failure(Error.Failure("Meta.JournalUnavailable", "Não foi possível registrar a operação. Nenhuma escrita foi enviada à Meta."));
        }
    }
    /// <inheritdoc />
    public async Task<MetaApiOperation?> GetAsync(Guid id, Guid workspaceId, Guid accountId, CancellationToken cancellationToken)
    {
        var context = await contexts.GetDbContextAsync(cancellationToken);
        return context.IsFailure ? null : await context.Value.MetaApiOperations.FirstOrDefaultAsync(o => o.Id == id && o.WorkspaceId == workspaceId && o.AccountId == accountId, cancellationToken);
    }
    /// <inheritdoc />
    public void Update(MetaApiOperation operation)
    {
        var context = contexts.GetDbContextAsync().GetAwaiter().GetResult();
        if (context.IsSuccess) context.Value.MetaApiOperations.Update(operation);
    }
}
