using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Infrastructure.Security;
using FluentAssertions;
using Master.Application.Repositories;
using Master.Domain.Tenants;
using Master.Infrastructure.Services;
using Microsoft.Extensions.Caching.Memory;

namespace UnitTests.Backend.MultiTenancy;

/// <summary>
/// Unit tests for <see cref="CachedTenantConnectionResolver"/>.
/// </summary>
public sealed class CachedTenantConnectionResolverTests
{
    private static readonly string EncryptionKey = Convert.ToBase64String(new byte[32]
    {
        10, 20, 30, 40, 50, 60, 70, 80,
        11, 21, 31, 41, 51, 61, 71, 81,
        12, 22, 32, 42, 52, 62, 72, 82,
        13, 23, 33, 43, 53, 63, 73, 83
    });

    private readonly IEncryptionService _encryptionService = new AesEncryptionService(EncryptionKey);
    private readonly IMemoryCache _memoryCache = new MemoryCache(new MemoryCacheOptions());
    private readonly FakeTenantRepository _repository = new();
    private readonly FakeTenantContextAccessor _contextAccessor = new();

    private CachedTenantConnectionResolver CreateSut()
    {
        return new CachedTenantConnectionResolver(
            _repository,
            _encryptionService,
            _contextAccessor,
            _memoryCache);
    }

    /// <summary>
    /// Verifies resolving connection string by ID decrypts and caches the connection.
    /// </summary>
    [Fact]
    public async Task ResolveConnectionStringAsync_ShouldReturnDecryptedConnectionString_AndCacheResult()
    {
        // Arrange
        var tenant = Tenant.Create("Agencia Alpha", "12345678000190", "alpha").Value;
        const string plainConnection = "Server=sql;Database=Tenant_alpha;User Id=sa;Password=Secret!;";
        tenant.SetEncryptedConnectionString(_encryptionService.Encrypt(plainConnection));
        await _repository.AddAsync(tenant);

        var sut = CreateSut();

        // Act
        var result = await sut.ResolveConnectionStringAsync(tenant.Id.Value);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(plainConnection);
        _repository.GetByIdCallCount.Should().Be(1);

        // Act again (should hit cache)
        var secondResult = await sut.ResolveConnectionStringAsync(tenant.Id.Value);
        secondResult.IsSuccess.Should().BeTrue();
        secondResult.Value.Should().Be(plainConnection);
        _repository.GetByIdCallCount.Should().Be(1); // Call count didn't increase!
    }

    /// <summary>
    /// Verifies resolving connection string returns NotFound when tenant does not exist.
    /// </summary>
    [Fact]
    public async Task ResolveConnectionStringAsync_ShouldReturnNotFound_WhenTenantDoesNotExist()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = await sut.ResolveConnectionStringAsync(Guid.NewGuid());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Tenant.NotFound");
    }

    /// <summary>
    /// Verifies resolving connection string returns Inactive error when tenant is suspended.
    /// </summary>
    [Fact]
    public async Task ResolveConnectionStringAsync_ShouldReturnInactive_WhenTenantIsSuspended()
    {
        // Arrange
        var tenant = Tenant.Create("Agencia Susp", "12345678000190", "susp").Value;
        tenant.SetEncryptedConnectionString(_encryptionService.Encrypt("Server=sql;Database=Tenant_susp;"));
        tenant.Suspend("Pagamento atrasado");
        await _repository.AddAsync(tenant);

        var sut = CreateSut();

        // Act
        var result = await sut.ResolveConnectionStringAsync(tenant.Id.Value);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Tenant.Inactive");
    }

    /// <summary>
    /// Verifies resolving connection string by subdomain works and caches the connection.
    /// </summary>
    [Fact]
    public async Task ResolveConnectionStringBySubdomainAsync_ShouldResolveAndCache_WhenSubdomainMatches()
    {
        // Arrange
        var tenant = Tenant.Create("Agencia Sub", "12345678000190", "agencia-sub").Value;
        const string plainConnection = "Server=sql;Database=Tenant_sub;User Id=sa;Password=Secret!;";
        tenant.SetEncryptedConnectionString(_encryptionService.Encrypt(plainConnection));
        await _repository.AddAsync(tenant);

        var sut = CreateSut();

        // Act
        var result = await sut.ResolveConnectionStringBySubdomainAsync("agencia-sub");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(plainConnection);
        _repository.GetBySubdomainCallCount.Should().Be(1);

        // Act again (should hit cache)
        var secondResult = await sut.ResolveConnectionStringBySubdomainAsync("agencia-sub");
        secondResult.IsSuccess.Should().BeTrue();
        _repository.GetBySubdomainCallCount.Should().Be(1);
    }

    /// <summary>
    /// Verifies resolving current tenant connection uses context accessor.
    /// </summary>
    [Fact]
    public async Task ResolveCurrentTenantConnectionStringAsync_ShouldUseTenantContextAccessor_WhenContextIsResolved()
    {
        // Arrange
        var tenant = Tenant.Create("Agencia Context", "12345678000190", "context").Value;
        const string plainConnection = "Server=sql;Database=Tenant_ctx;";
        tenant.SetEncryptedConnectionString(_encryptionService.Encrypt(plainConnection));
        await _repository.AddAsync(tenant);

        _contextAccessor.TenantContext = new FakeTenantContext(tenant.Id.Value, tenant.Subdomain);
        var sut = CreateSut();

        // Act
        var result = await sut.ResolveCurrentTenantConnectionStringAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(plainConnection);
    }

    /// <summary>
    /// Verifies resolving current tenant connection returns error when context is not resolved.
    /// </summary>
    [Fact]
    public async Task ResolveCurrentTenantConnectionStringAsync_ShouldReturnContextNotResolved_WhenContextIsNull()
    {
        // Arrange
        _contextAccessor.TenantContext = new FakeUnresolvedTenantContext();
        var sut = CreateSut();

        // Act
        var result = await sut.ResolveCurrentTenantConnectionStringAsync();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Tenant.ContextNotResolved");
    }

    /// <summary>
    /// Verifies invalidating cache forces a new repository query.
    /// </summary>
    [Fact]
    public async Task InvalidateCache_ShouldForceReloadFromRepository()
    {
        // Arrange
        var tenant = Tenant.Create("Agencia Inval", "12345678000190", "inval").Value;
        const string plainConnection = "Server=sql;Database=Tenant_inval;";
        tenant.SetEncryptedConnectionString(_encryptionService.Encrypt(plainConnection));
        await _repository.AddAsync(tenant);

        var sut = CreateSut();
        await sut.ResolveConnectionStringAsync(tenant.Id.Value);
        _repository.GetByIdCallCount.Should().Be(1);

        // Act
        sut.InvalidateCache(tenant.Id.Value);
        var reloaded = await sut.ResolveConnectionStringAsync(tenant.Id.Value);

        // Assert
        reloaded.IsSuccess.Should().BeTrue();
        _repository.GetByIdCallCount.Should().Be(2); // Second repo call performed!
    }

    private sealed class FakeTenantRepository : ITenantRepository
    {
        private readonly Dictionary<Guid, Tenant> _tenants = new();

        public int GetByIdCallCount { get; private set; }
        public int GetBySubdomainCallCount { get; private set; }

        public Task AddAsync(Tenant entity, CancellationToken cancellationToken = default)
        {
            _tenants[entity.Id.Value] = entity;
            return Task.CompletedTask;
        }

        public Task<Tenant?> GetByIdAsync(TenantId id, CancellationToken cancellationToken = default)
        {
            GetByIdCallCount++;
            _tenants.TryGetValue(id.Value, out var tenant);
            return Task.FromResult(tenant);
        }

        public Task<Tenant?> GetBySubdomainAsync(string subdomain, CancellationToken cancellationToken = default)
        {
            GetBySubdomainCallCount++;
            var normalized = subdomain.Trim().ToLowerInvariant();
            var tenant = _tenants.Values.FirstOrDefault(t => t.Subdomain == normalized);
            return Task.FromResult(tenant);
        }

        public void Update(Tenant entity) => _tenants[entity.Id.Value] = entity;
        public void Remove(Tenant entity) => _tenants.Remove(entity.Id.Value);

        public Task<IReadOnlyList<Tenant>> GetTenantsForDunningEvaluationAsync(CancellationToken cancellationToken = default)
        {
            var list = _tenants.Values
                .Where(t => t.PaymentDueDateUtc != null || t.DunningStage != DunningStage.None)
                .ToList();
            return Task.FromResult<IReadOnlyList<Tenant>>(list);
        }
    }

    private sealed class FakeTenantContextAccessor : ITenantContextAccessor
    {
        public ITenantContext TenantContext { get; set; } = new FakeUnresolvedTenantContext();
    }

    private sealed class FakeUnresolvedTenantContext : ITenantContext
    {
        public Guid? TenantId => null;
        public string? Subdomain => null;
        public string? RawIdentifier => null;
        public TenantResolutionSource Source => TenantResolutionSource.None;
        public bool IsResolved => false;
    }

    private sealed class FakeTenantContext : ITenantContext
    {
        public FakeTenantContext(Guid tenantId, string subdomain)
        {
            TenantId = tenantId;
            Subdomain = subdomain;
            RawIdentifier = subdomain;
            IsResolved = true;
            Source = TenantResolutionSource.Subdomain;
        }

        public Guid? TenantId { get; }
        public string? Subdomain { get; }
        public string? RawIdentifier { get; }
        public TenantResolutionSource Source { get; }
        public bool IsResolved { get; }
    }
}
