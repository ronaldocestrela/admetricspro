using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Domain.Abstractions;
using FluentAssertions;

namespace UnitTests.Backend.Persistence;

/// <summary>
/// Unit tests validating persistence contracts behavior and fakes.
/// </summary>
public sealed class PersistenceContractsTests
{
    private sealed class CustomerAggregate : AggregateRoot<Guid>
    {
        public CustomerAggregate(Guid id, string name)
            : base(id)
        {
            Name = name;
        }

        public string Name { get; private set; }

        public void UpdateName(string newName)
        {
            Name = newName;
        }
    }

    private sealed class InMemoryCustomerRepository : IRepository<CustomerAggregate, Guid>
    {
        private readonly Dictionary<Guid, CustomerAggregate> _storage = new();

        public Task AddAsync(CustomerAggregate entity, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _storage[entity.Id] = entity;
            return Task.CompletedTask;
        }

        public Task<CustomerAggregate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _storage.TryGetValue(id, out var entity);
            return Task.FromResult(entity);
        }

        public void Update(CustomerAggregate entity)
        {
            _storage[entity.Id] = entity;
        }

        public void Remove(CustomerAggregate entity)
        {
            _storage.Remove(entity.Id);
        }
    }

    private sealed class InMemoryUnitOfWork : IUnitOfWork
    {
        public int CommitsCount { get; private set; }

        public Task<int> CommitAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CommitsCount++;
            return Task.FromResult(1);
        }
    }

    /// <summary>
    /// Verifies adding and retrieving an aggregate by id.
    /// </summary>
    [Fact]
    public async Task Repository_AddAndGetByIdAsync_ShouldPersistAndRetrieveAggregate()
    {
        // Arrange
        var repository = new InMemoryCustomerRepository();
        var customerId = Guid.NewGuid();
        var customer = new CustomerAggregate(customerId, "Acme Corp");

        // Act
        await repository.AddAsync(customer, CancellationToken.None);
        var retrieved = await repository.GetByIdAsync(customerId, CancellationToken.None);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Id.Should().Be(customerId);
        retrieved.Name.Should().Be("Acme Corp");
    }

    /// <summary>
    /// Verifies getting aggregate by non-existing id returns null.
    /// </summary>
    [Fact]
    public async Task Repository_GetByIdAsync_ShouldReturnNull_WhenEntityDoesNotExist()
    {
        // Arrange
        var repository = new InMemoryCustomerRepository();

        // Act
        var result = await repository.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies updating an existing aggregate in repository.
    /// </summary>
    [Fact]
    public async Task Repository_Update_ShouldModifyExistingAggregate()
    {
        // Arrange
        var repository = new InMemoryCustomerRepository();
        var customerId = Guid.NewGuid();
        var customer = new CustomerAggregate(customerId, "Old Name");
        await repository.AddAsync(customer, CancellationToken.None);

        // Act
        customer.UpdateName("New Name");
        repository.Update(customer);
        var updated = await repository.GetByIdAsync(customerId, CancellationToken.None);

        // Assert
        updated.Should().NotBeNull();
        updated!.Name.Should().Be("New Name");
    }

    /// <summary>
    /// Verifies removing an existing aggregate from repository.
    /// </summary>
    [Fact]
    public async Task Repository_Remove_ShouldDeleteAggregate()
    {
        // Arrange
        var repository = new InMemoryCustomerRepository();
        var customerId = Guid.NewGuid();
        var customer = new CustomerAggregate(customerId, "To Delete");
        await repository.AddAsync(customer, CancellationToken.None);

        // Act
        repository.Remove(customer);
        var retrieved = await repository.GetByIdAsync(customerId, CancellationToken.None);

        // Assert
        retrieved.Should().BeNull();
    }

    /// <summary>
    /// Verifies repository methods respect cancellation token.
    /// </summary>
    [Fact]
    public async Task Repository_ShouldRespectCancellationToken()
    {
        // Arrange
        var repository = new InMemoryCustomerRepository();
        var customer = new CustomerAggregate(Guid.NewGuid(), "Cancelled");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var actAdd = () => repository.AddAsync(customer, cts.Token);
        var actGet = () => repository.GetByIdAsync(customer.Id, cts.Token);

        // Assert
        await actAdd.Should().ThrowAsync<OperationCanceledException>();
        await actGet.Should().ThrowAsync<OperationCanceledException>();
    }

    /// <summary>
    /// Verifies UnitOfWork commits increment commit count.
    /// </summary>
    [Fact]
    public async Task UnitOfWork_CommitAsync_ShouldIncrementCommitCount()
    {
        // Arrange
        var unitOfWork = new InMemoryUnitOfWork();

        // Act
        var affected = await unitOfWork.CommitAsync(CancellationToken.None);

        // Assert
        affected.Should().Be(1);
        unitOfWork.CommitsCount.Should().Be(1);
    }
}
