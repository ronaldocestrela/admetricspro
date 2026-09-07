using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BuildingBlocks.Application.Tenants.Queries.GetTenantPlanLimits;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Tenants.Application.Persistence;
using Tenants.Application.Workspaces.Repositories;
using WebApi.Models;
using Xunit;

namespace AcceptanceTests.Endpoints;

/// <summary>
/// Testes de aceitação para os endpoints de gestão de clientes/workspaces (/api/v1/workspaces).
/// </summary>
public sealed class WorkspacesEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private static readonly Guid TestTenantId = Guid.NewGuid();
    private const string ValidCpf = "123.456.789-09";

    /// <summary>
    /// Inicializa a suíte de testes com a fábrica de aplicação Web.
    /// </summary>
    /// <param name="factory">Fábrica de aplicação Web.</param>
    public WorkspacesEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Valida que POST /api/v1/workspaces cria com sucesso o workspace e retorna 201 Created com cabeçalho Location.
    /// </summary>
    [Fact]
    public async Task CreateWorkspace_ComDadosValidos_DeveRetornarCreatedELocation()
    {
        // Arrange
        var fakeRepo = new FakeWorkspaceRepository();
        var fakeUow = new FakeTenantUnitOfWork();
        var fakeLimitsHandler = new FakePlanLimitsHandler(maxWorkspaces: 3);

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddScoped<IWorkspaceRepository>(_ => fakeRepo);
                services.AddScoped<ITenantUnitOfWork>(_ => fakeUow);
                services.AddScoped<IRequestHandler<GetTenantPlanLimitsQuery, Result<TenantPlanLimitsDto>>>(_ => fakeLimitsHandler);
            });
        }).CreateClient();

        client.DefaultRequestHeaders.Add("X-Tenant-Id", TestTenantId.ToString());

        var request = new CreateWorkspaceApiRequest("Cliente Beta LTDA", ValidCpf, 5000m, "E-commerce");

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/workspaces", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().ToLowerInvariant().Should().Contain("/api/v1/workspaces/");

        var content = await response.Content.ReadAsStringAsync();
        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var envelope = JsonSerializer.Deserialize<WorkspaceResultEnvelope<Guid>>(content, jsonOptions);

        envelope.Should().NotBeNull();
        envelope!.IsSuccess.Should().BeTrue();
        envelope.Value.Should().NotBeEmpty();
    }

    /// <summary>
    /// Valida que POST /api/v1/workspaces retorna BadRequest quando a cota de workspaces foi excedida.
    /// </summary>
    [Fact]
    public async Task CreateWorkspace_QuandoCotaExcedida_DeveRetornarBadRequest()
    {
        // Arrange
        var fakeRepo = new FakeWorkspaceRepository();
        // Simula 3 workspaces ativos existentes
        for (int i = 0; i < 3; i++)
        {
            var w = Workspace.Create(Guid.NewGuid(), $"Cliente {i}", ValidCpf, 1000m, "Varejo").Value;
            fakeRepo.Items.Add(w);
        }

        var fakeUow = new FakeTenantUnitOfWork();
        var fakeLimitsHandler = new FakePlanLimitsHandler(maxWorkspaces: 3);

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddScoped<IWorkspaceRepository>(_ => fakeRepo);
                services.AddScoped<ITenantUnitOfWork>(_ => fakeUow);
                services.AddScoped<IRequestHandler<GetTenantPlanLimitsQuery, Result<TenantPlanLimitsDto>>>(_ => fakeLimitsHandler);
            });
        }).CreateClient();

        client.DefaultRequestHeaders.Add("X-Tenant-Id", TestTenantId.ToString());

        var request = new CreateWorkspaceApiRequest("Cliente Excedente", ValidCpf, 5000m, "Varejo");

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/workspaces", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Workspace.QuotaExceeded");
    }

    /// <summary>
    /// Valida que GET /api/v1/workspaces retorna a lista de workspaces com status HTTP 200 OK.
    /// </summary>
    [Fact]
    public async Task GetWorkspaces_DeveRetornarOkComLista()
    {
        // Arrange
        var fakeRepo = new FakeWorkspaceRepository();
        var w1 = Workspace.Create(Guid.NewGuid(), "Cliente Listagem", ValidCpf, 1000m, "Varejo").Value;
        fakeRepo.Items.Add(w1);

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddScoped<IWorkspaceRepository>(_ => fakeRepo);
            });
        }).CreateClient();

        client.DefaultRequestHeaders.Add("X-Tenant-Id", TestTenantId.ToString());

        // Act
        var response = await client.GetAsync("/api/v1/workspaces");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Cliente Listagem");
    }

    /// <summary>
    /// Valida que GET /api/v1/workspaces/{id} retorna NotFound caso o identificador não exista.
    /// </summary>
    [Fact]
    public async Task GetWorkspaceById_QuandoInexistente_DeveRetornarNotFound()
    {
        // Arrange
        var fakeRepo = new FakeWorkspaceRepository();

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddScoped<IWorkspaceRepository>(_ => fakeRepo);
            });
        }).CreateClient();

        client.DefaultRequestHeaders.Add("X-Tenant-Id", TestTenantId.ToString());

        // Act
        var response = await client.GetAsync($"/api/v1/workspaces/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private sealed class FakeWorkspaceRepository : IWorkspaceRepository
    {
        public List<Workspace> Items { get; } = new();

        public Task<Workspace?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Items.FirstOrDefault(w => w.Id == id));
        }

        public Task<IReadOnlyList<Workspace>> GetAllAsync(bool? activeOnly = null, CancellationToken cancellationToken = default)
        {
            var query = Items.AsEnumerable();
            if (activeOnly.HasValue)
            {
                query = query.Where(w => w.IsActive == activeOnly.Value);
            }
            return Task.FromResult<IReadOnlyList<Workspace>>(query.ToList());
        }

        public Task<bool> ExistsByCnpjOrCpfAsync(string cnpjOrCpf, Guid? excludeId = null, CancellationToken cancellationToken = default)
        {
            var exists = Items.Any(w => w.CnpjOrCpf == cnpjOrCpf && (!excludeId.HasValue || w.Id != excludeId.Value));
            return Task.FromResult(exists);
        }

        public Task<int> CountActiveAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Items.Count(w => w.IsActive));
        }

        public Task AddAsync(Workspace workspace, CancellationToken cancellationToken = default)
        {
            Items.Add(workspace);
            return Task.CompletedTask;
        }

        public void Update(Workspace workspace)
        {
            var idx = Items.FindIndex(w => w.Id == workspace.Id);
            if (idx >= 0)
            {
                Items[idx] = workspace;
            }
        }
    }

    private sealed class FakeTenantUnitOfWork : ITenantUnitOfWork
    {
        public Task<int> CommitAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
    }

    private sealed class FakePlanLimitsHandler : IRequestHandler<GetTenantPlanLimitsQuery, Result<TenantPlanLimitsDto>>
    {
        private readonly int _maxWorkspaces;

        public FakePlanLimitsHandler(int maxWorkspaces)
        {
            _maxWorkspaces = maxWorkspaces;
        }

        public Task<Result<TenantPlanLimitsDto>> Handle(GetTenantPlanLimitsQuery request, CancellationToken cancellationToken)
        {
            return Task.FromResult(Result<TenantPlanLimitsDto>.Success(
                new TenantPlanLimitsDto("Starter", _maxWorkspaces, 3, 50000m)));
        }
    }

    private sealed class WorkspaceResultEnvelope<T>
    {
        public bool IsSuccess { get; set; }
        public bool IsFailure { get; set; }
        public T Value { get; set; } = default!;
    }
}
