using BuildingBlocks.Application.Persistence;
using Master.Infrastructure.Persistence;

namespace Master.Infrastructure.Repositories;

/// <summary>
/// Unit of work implementation over <see cref="MasterDbContext"/>.
/// </summary>
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly MasterDbContext _masterDbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnitOfWork"/> class.
    /// </summary>
    /// <param name="masterDbContext">Master catalog context.</param>
    public UnitOfWork(MasterDbContext masterDbContext)
    {
        _masterDbContext = masterDbContext;
    }

    /// <inheritdoc />
    public Task<int> CommitAsync(CancellationToken cancellationToken)
    {
        return _masterDbContext.SaveChangesAsync(cancellationToken);
    }
}