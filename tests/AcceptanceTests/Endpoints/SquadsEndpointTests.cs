using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Tenants.Application.Persistence;
using Tenants.Application.Squads.DTOs;
using Tenants.Application.Squads.Repositories;
using Tenants.Application.Squads.Services;
using Tenants.Application.Users.Repositories;
using Tenants.Application.Workspaces.DTOs;
using Tenants.Application.Workspaces.Repositories;
using Tenants.Infrastructure.Squads;
using WebApi.Models;
using Xunit;

namespace AcceptanceTests.Endpoints;

/// <summary>
/// Testes de aceitação para os endpoints de gestão de times/squads (/api/v1/squads).
/// </summary>
public sealed class SquadsEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private static readonly Guid TestTenantId = Guid.NewGuid();

    /// <summary>
    /// Inicializa a fábrica da API web para execução dos testes.
    /// </summary>
    /// <param name="factory">Fábrica de aplicação.</param>
    public SquadsEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Valida que POST /api/v1/squads cadastra um squad e retorna 201 Created com Location.
    /// </summary>
    [Fact]
    public async Task CreateSquad_ComDadosValidos_DeveRetornarCreatedELocation()
    {
        // Arrange
        var fakeSquadRepo = new FakeSquadRepository();
        var fakeUserRepo = new FakeTenantUserRepository();
        var fakeWorkspaceRepo = new FakeWorkspaceRepository();
        var fakeUow = new FakeTenantUnitOfWork();

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddScoped<ISquadRepository>(_ => fakeSquadRepo);
                services.AddScoped<ITenantUserRepository>(_ => fakeUserRepo);
                services.AddScoped<IWorkspaceRepository>(_ => fakeWorkspaceRepo);
                services.AddScoped<ITenantUnitOfWork>(_ => fakeUow);
            });
        }).CreateClient();

        client.DefaultRequestHeaders.Add("X-Tenant-Id", TestTenantId.ToString());

        var request = new CreateSquadApiRequest("Squad E-commerce", "Squad focado em lojas virtuais");

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/squads", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().ToLowerInvariant().Should().Contain("/api/v1/squads/");

        var content = await response.Content.ReadAsStringAsync();
        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var envelope = JsonSerializer.Deserialize<ResultEnvelope<Guid>>(content, jsonOptions);

        envelope.Should().NotBeNull();
        envelope!.IsSuccess.Should().BeTrue();
        envelope.Value.Should().NotBeEmpty();
    }

    /// <summary>
    /// Valida que GET /api/v1/squads lista os squads cadastrados com status 200 OK.
    /// </summary>
    [Fact]
    public async Task GetSquads_DeveRetornarListaDeSquads()
    {
        // Arrange
        var fakeSquadRepo = new FakeSquadRepository();
        fakeSquadRepo.Items.Add(Squad.Create(Guid.NewGuid(), "Squad Alpha", null).Value);
        fakeSquadRepo.Items.Add(Squad.Create(Guid.NewGuid(), "Squad Beta", null).Value);

        var fakeUserRepo = new FakeTenantUserRepository();
        var fakeWorkspaceRepo = new FakeWorkspaceRepository();
        var fakeUow = new FakeTenantUnitOfWork();

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddScoped<ISquadRepository>(_ => fakeSquadRepo);
                services.AddScoped<ITenantUserRepository>(_ => fakeUserRepo);
                services.AddScoped<IWorkspaceRepository>(_ => fakeWorkspaceRepo);
                services.AddScoped<ITenantUnitOfWork>(_ => fakeUow);
            });
        }).CreateClient();

        client.DefaultRequestHeaders.Add("X-Tenant-Id", TestTenantId.ToString());

        // Act
        var response = await client.GetAsync("/api/v1/squads");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var envelope = JsonSerializer.Deserialize<ResultEnvelope<List<SquadSummaryDto>>>(content, jsonOptions);

        envelope.Should().NotBeNull();
        envelope!.IsSuccess.Should().BeTrue();
        envelope.Value.Should().HaveCount(2);
    }

    /// <summary>
    /// Valida o fluxo de alocação de membros, clientes e consulta de carteira (/portfolio).
    /// </summary>
    [Fact]
    public async Task FluxoCompleto_MembroEWorkspaceEPortfolio_DeveRetornarWorkspacesAutorizados()
    {
        // Arrange
        var fakeSquadRepo = new FakeSquadRepository();
        var fakeUserRepo = new FakeTenantUserRepository();
        var fakeWorkspaceRepo = new FakeWorkspaceRepository();
        var fakeUow = new FakeTenantUnitOfWork();

        var squad = Squad.Create(Guid.NewGuid(), "Squad Performance", null).Value;
        fakeSquadRepo.Items.Add(squad);

        var analystId = Guid.NewGuid();
        var analyst = TenantUser.Create(analystId, "Lucas Analista", "lucas@agencia.com", null, "hash", TenantRole.Analyst).Value;
        fakeUserRepo.Items.Add(analyst);

        var wsId = Guid.NewGuid();
        var workspace = Workspace.Create(wsId, "Loja Alpha", "123.456.789-09", 5000m, "E-commerce").Value;
        fakeWorkspaceRepo.Items.Add(workspace);

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddScoped<ISquadRepository>(_ => fakeSquadRepo);
                services.AddScoped<ITenantUserRepository>(_ => fakeUserRepo);
                services.AddScoped<IWorkspaceRepository>(_ => fakeWorkspaceRepo);
                services.AddScoped<ITenantUnitOfWork>(_ => fakeUow);
                services.AddScoped<IUserPortfolioService, UserPortfolioService>();
            });
        }).CreateClient();

        client.DefaultRequestHeaders.Add("X-Tenant-Id", TestTenantId.ToString());

        // 1. Adiciona membro
        var addMemberResponse = await client.PostAsJsonAsync($"/api/v1/squads/{squad.Id}/members", new AddSquadMemberApiRequest(analystId));
        addMemberResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // 2. Aloca workspace
        var assignWsResponse = await client.PostAsJsonAsync($"/api/v1/squads/{squad.Id}/workspaces", new AssignSquadWorkspaceApiRequest(wsId));
        assignWsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // 3. Consulta carteira do analista
        var portfolioResponse = await client.GetAsync($"/api/v1/squads/users/{analystId}/portfolio");
        portfolioResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await portfolioResponse.Content.ReadAsStringAsync();
        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var envelope = JsonSerializer.Deserialize<ResultEnvelope<List<WorkspaceDto>>>(content, jsonOptions);

        envelope.Should().NotBeNull();
        envelope!.IsSuccess.Should().BeTrue();
        envelope.Value.Should().ContainSingle(w => w.Id == wsId && w.Name == "Loja Alpha");
    }

    #region Fakes de Teste

    private sealed class ResultEnvelope<T>
    {
        public bool IsSuccess { get; set; }
        public bool IsFailure { get; set; }
        public T? Value { get; set; }
    }

    private sealed class FakeSquadRepository : ISquadRepository
    {
        public readonly List<Squad> Items = new();

        public Task<Squad?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(Items.FirstOrDefault(s => s.Id == id));

        public Task<IReadOnlyList<Squad>> GetAllAsync(bool? activeOnly = null, CancellationToken cancellationToken = default)
        {
            var query = Items.AsEnumerable();
            if (activeOnly.HasValue)
                query = query.Where(s => s.IsActive == activeOnly.Value);
            return Task.FromResult<IReadOnlyList<Squad>>(query.ToList());
        }

        public Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default)
        {
            var normalized = name.Trim().ToUpper();
            var exists = Items.Any(s => s.Name.ToUpper() == normalized && (!excludeId.HasValue || s.Id != excludeId.Value));
            return Task.FromResult(exists);
        }

        public Task<IReadOnlyList<Squad>> GetSquadsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var squads = Items.Where(s => s.IsActive && s.Members.Any(m => m.UserId == userId)).ToList();
            return Task.FromResult<IReadOnlyList<Squad>>(squads);
        }

        public Task<IReadOnlyList<Squad>> GetSquadsByWorkspaceIdAsync(Guid workspaceId, CancellationToken cancellationToken = default)
        {
            var squads = Items.Where(s => s.IsActive && s.Workspaces.Any(w => w.WorkspaceId == workspaceId)).ToList();
            return Task.FromResult<IReadOnlyList<Squad>>(squads);
        }

        public Task AddAsync(Squad squad, CancellationToken cancellationToken = default)
        {
            Items.Add(squad);
            return Task.CompletedTask;
        }

        public void Update(Squad squad) { }
        public void Remove(Squad squad) => Items.Remove(squad);
    }

    private sealed class FakeTenantUserRepository : ITenantUserRepository
    {
        public readonly List<TenantUser> Items = new();

        public Task<TenantUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(Items.FirstOrDefault(u => u.Id == id));

        public Task<TenantUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
            => Task.FromResult(Items.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase)));

        public Task<bool> ExistsByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(Items.Any(u => u.Id == id && u.IsActive));

        public Task<IReadOnlyList<TenantUser>> GetAllAsync(bool? activeOnly = null, CancellationToken cancellationToken = default)
        {
            var query = Items.AsEnumerable();
            if (activeOnly.HasValue)
                query = query.Where(u => u.IsActive == activeOnly.Value);
            return Task.FromResult<IReadOnlyList<TenantUser>>(query.ToList());
        }

        public Task AddAsync(TenantUser user, CancellationToken cancellationToken = default)
        {
            Items.Add(user);
            return Task.CompletedTask;
        }

        public void Update(TenantUser user) { }
    }

    private sealed class FakeWorkspaceRepository : IWorkspaceRepository
    {
        public readonly List<Workspace> Items = new();

        public Task<Workspace?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(Items.FirstOrDefault(w => w.Id == id));

        public Task<IReadOnlyList<Workspace>> GetAllAsync(bool? activeOnly = null, CancellationToken cancellationToken = default)
        {
            var query = Items.AsEnumerable();
            if (activeOnly.HasValue)
                query = query.Where(w => w.IsActive == activeOnly.Value);
            return Task.FromResult<IReadOnlyList<Workspace>>(query.ToList());
        }

        public Task<bool> ExistsByCnpjOrCpfAsync(string cnpjOrCpf, Guid? excludeId = null, CancellationToken cancellationToken = default)
            => Task.FromResult(Items.Any(w => w.CnpjOrCpf == cnpjOrCpf && (!excludeId.HasValue || w.Id != excludeId.Value)));

        public Task<int> CountActiveAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Items.Count(w => w.IsActive));

        public Task AddAsync(Workspace workspace, CancellationToken cancellationToken = default)
        {
            Items.Add(workspace);
            return Task.CompletedTask;
        }

        public void Update(Workspace workspace) { }
    }

    private sealed class FakeTenantUnitOfWork : ITenantUnitOfWork
    {
        public Task<int> CommitAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(1);
    }

    #endregion
}
