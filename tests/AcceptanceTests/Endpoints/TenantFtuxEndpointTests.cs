using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Tenants.Application.Ftux.DTOs;
using Tenants.Application.Integrations.Repositories;
using Tenants.Application.Persistence;
using Tenants.Application.Squads.Repositories;
using Tenants.Application.Users.Repositories;
using Tenants.Application.Workspaces.Repositories;
using WebApi.Models;
using Xunit;

namespace AcceptanceTests.Endpoints;

/// <summary>
/// Testes de aceitação para os endpoints de FTUX, contas demonstrativas e usuários de inquilino.
/// </summary>
public sealed class TenantFtuxEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private static readonly Guid TestTenantId = Guid.NewGuid();
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Inicializa a suíte com a fábrica Web.
    /// </summary>
    /// <param name="factory">Fábrica de aplicação Web.</param>
    public TenantFtuxEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Valida que GET /api/v1/tenants/ftux-status retorna 200 OK com o progresso calculado.
    /// </summary>
    [Fact]
    public async Task GetFtuxStatus_DeveRetornarOkComStatusDoTenant()
    {
        // Arrange
        var fakeWorkspaceRepo = new FakeWorkspaceRepository();
        var fakeAdAccountRepo = new FakeConnectedAdAccountRepository();
        var fakeUserRepo = new FakeTenantUserRepository();
        var fakeSquadRepo = new FakeSquadRepository();

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddScoped<IWorkspaceRepository>(_ => fakeWorkspaceRepo);
                services.AddScoped<IConnectedAdAccountRepository>(_ => fakeAdAccountRepo);
                services.AddScoped<ITenantUserRepository>(_ => fakeUserRepo);
                services.AddScoped<ISquadRepository>(_ => fakeSquadRepo);
            });
        }).CreateClient();

        client.DefaultRequestHeaders.Add("X-Tenant-Id", TestTenantId.ToString());

        // Act
        var response = await client.GetAsync("/api/v1/tenants/ftux-status");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var envelope = await response.Content.ReadFromJsonAsync<Result<TenantFtuxStatusDto>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.IsSuccess.Should().BeTrue();
        envelope.Value.IsProvisioned.Should().BeTrue();
        envelope.Value.ProgressPercentage.Should().Be(25);
    }

    /// <summary>
    /// Valida que POST /api/v1/integrations/demo-account cria uma conta demo e retorna 201 Created.
    /// </summary>
    [Fact]
    public async Task ConnectDemoAccount_ComWorkspaceValido_DeveRetornarCreated()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var workspace = Workspace.Create(workspaceId, "Workspace Cliente", "12.345.678/0001-95", 1000m, "E-commerce").Value;

        var fakeWorkspaceRepo = new FakeWorkspaceRepository();
        fakeWorkspaceRepo.Seed(workspace);

        var fakeAdAccountRepo = new FakeConnectedAdAccountRepository();
        var fakeUow = new FakeTenantUnitOfWork();

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddScoped<IWorkspaceRepository>(_ => fakeWorkspaceRepo);
                services.AddScoped<IConnectedAdAccountRepository>(_ => fakeAdAccountRepo);
                services.AddScoped<ITenantUnitOfWork>(_ => fakeUow);
            });
        }).CreateClient();

        client.DefaultRequestHeaders.Add("X-Tenant-Id", TestTenantId.ToString());

        var request = new ConnectDemoAccountApiRequest(workspaceId, "MetaAds");

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/integrations/demo-account", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var envelope = await response.Content.ReadFromJsonAsync<Result<Guid>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.IsSuccess.Should().BeTrue();
        envelope.Value.Should().NotBeEmpty();
    }

    /// <summary>
    /// Valida que POST /api/v1/tenants/users convida colaborador com sucesso retornando 201 Created.
    /// </summary>
    [Fact]
    public async Task InviteUser_ComDadosValidos_DeveRetornarCreated()
    {
        // Arrange
        var fakeUserRepo = new FakeTenantUserRepository();
        var fakeUow = new FakeTenantUnitOfWork();

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddScoped<ITenantUserRepository>(_ => fakeUserRepo);
                services.AddScoped<ITenantUnitOfWork>(_ => fakeUow);
            });
        }).CreateClient();

        client.DefaultRequestHeaders.Add("X-Tenant-Id", TestTenantId.ToString());

        var request = new InviteTenantUserApiRequest("Gestor Tráfego", "gestor.novo@agencia.com", "MediaManager");

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/tenants/users", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var envelope = await response.Content.ReadFromJsonAsync<Result<Guid>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.IsSuccess.Should().BeTrue();
        envelope.Value.Should().NotBeEmpty();
    }

    private sealed class FakeWorkspaceRepository : IWorkspaceRepository
    {
        private readonly List<Workspace> _items = new();

        public void Seed(Workspace workspace) => _items.Add(workspace);

        public Task<Workspace?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(_items.FirstOrDefault(w => w.Id == id));

        public Task<IReadOnlyList<Workspace>> GetAllAsync(bool? activeOnly = null, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Workspace>>(_items);

        public Task<bool> ExistsByCnpjOrCpfAsync(string cnpjOrCpf, Guid? excludeId = null, CancellationToken cancellationToken = default)
            => Task.FromResult(_items.Any(w => w.CnpjOrCpf == cnpjOrCpf && (!excludeId.HasValue || w.Id != excludeId.Value)));

        public Task<int> CountActiveAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(_items.Count(w => w.IsActive));

        public Task AddAsync(Workspace workspace, CancellationToken cancellationToken = default)
        {
            _items.Add(workspace);
            return Task.CompletedTask;
        }

        public void Update(Workspace workspace) { }
    }

    private sealed class FakeConnectedAdAccountRepository : IConnectedAdAccountRepository
    {
        private readonly List<ConnectedAdAccount> _items = new();

        public Task<ConnectedAdAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(_items.FirstOrDefault(a => a.Id == id));

        public Task<IReadOnlyList<ConnectedAdAccount>> GetByWorkspaceIdAsync(Guid workspaceId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ConnectedAdAccount>>(_items.Where(a => a.WorkspaceId == workspaceId).ToList());

        public Task<IReadOnlyList<ConnectedAdAccount>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ConnectedAdAccount>>(_items);

        public Task<int> CountAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(_items.Count);

        public Task AddAsync(ConnectedAdAccount account, CancellationToken cancellationToken = default)
        {
            _items.Add(account);
            return Task.CompletedTask;
        }

        public void Update(ConnectedAdAccount account) { }
    }

    private sealed class FakeTenantUserRepository : ITenantUserRepository
    {
        private readonly List<TenantUser> _items = new();

        public Task<TenantUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(_items.FirstOrDefault(u => u.Id == id));

        public Task<TenantUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
            => Task.FromResult(_items.FirstOrDefault(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase)));

        public Task<bool> ExistsByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(_items.Any(u => u.Id == id));

        public Task<IReadOnlyList<TenantUser>> GetAllAsync(bool? activeOnly = null, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<TenantUser>>(_items);

        public Task AddAsync(TenantUser user, CancellationToken cancellationToken = default)
        {
            _items.Add(user);
            return Task.CompletedTask;
        }

        public void Update(TenantUser user) { }
    }

    private sealed class FakeSquadRepository : ISquadRepository
    {
        private readonly List<Squad> _items = new();

        public Task<Squad?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(_items.FirstOrDefault(s => s.Id == id));

        public Task<IReadOnlyList<Squad>> GetAllAsync(bool? activeOnly = null, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Squad>>(_items);

        public Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default)
            => Task.FromResult(_items.Any(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase) && (!excludeId.HasValue || s.Id != excludeId.Value)));

        public Task<IReadOnlyList<Squad>> GetSquadsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Squad>>(_items);

        public Task<IReadOnlyList<Squad>> GetSquadsByWorkspaceIdAsync(Guid workspaceId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Squad>>(_items);

        public Task AddAsync(Squad squad, CancellationToken cancellationToken = default)
        {
            _items.Add(squad);
            return Task.CompletedTask;
        }

        public void Update(Squad squad) { }

        public void Remove(Squad squad) { }
    }

    private sealed class FakeTenantUnitOfWork : ITenantUnitOfWork
    {
        public Task<int> CommitAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
    }
}
