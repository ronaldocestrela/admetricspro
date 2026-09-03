using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using Master.Application.Billing.Dunning;
using Master.Application.Repositories;
using Master.Application.Tenants.Commands.ImpersonateTenant;
using Master.Domain.Tenants;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using WebApi.Models;

namespace AcceptanceTests.Endpoints;

/// <summary>
/// Acceptance tests verifying HTTP contracts and serialization for tenant impersonation endpoints.
/// </summary>
public sealed class ImpersonationEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImpersonationEndpointTests"/> class.
    /// </summary>
    /// <param name="factory">Application factory.</param>
    public ImpersonationEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private HttpClient CreateTestClient(Tenant? tenant, ImpersonationSession? session = null)
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddScoped<IDunningEngineService, FakeDunningEngineService>();
                services.AddScoped<ITenantRepository>(_ => new FakeTenantRepository(tenant));
                services.AddScoped<IImpersonationSessionRepository>(_ => new FakeImpersonationSessionRepository(session));
                services.AddScoped<Master.Application.Auditing.IMasterAuditService, FakeMasterAuditService>();
                services.AddScoped<IUnitOfWork>(_ => new FakeUnitOfWork());
            });
        }).CreateClient();
    }

    /// <summary>
    /// Verifies that POST /api/v1/tenants/{tenantId}/impersonate returns HTTP 200 with JWT token and envelope.
    /// </summary>
    [Fact]
    public async Task ImpersonateTenant_ShouldReturnOk_WhenRequestIsValid()
    {
        // Arrange
        var tenant = Tenant.Create("Beta Inc", "11222333000181", "beta").Value;
        var client = CreateTestClient(tenant);

        var request = new ImpersonateTenantApiRequest(
            SuperAdminId: Guid.NewGuid(),
            SupportTicketId: "INC-12345",
            Reason: "Diagnóstico de erro em relatórios de anúncios",
            DurationMinutes: 30);

        // Act
        var response = await client.PostAsJsonAsync($"/api/v1/tenants/{tenant.Id.Value}/impersonate", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var result = JsonSerializer.Deserialize<ImpersonateResultEnvelope>(content, jsonOptions);

        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.Value.TokenType.Should().Be("Bearer");
        result.Value.TenantId.Should().Be(tenant.Id.Value);
        result.Value.SupportTicketId.Should().Be("INC-12345");
    }

    /// <summary>
    /// Verifies that POST returns 422 UnprocessableEntity when justification or ticket are invalid.
    /// </summary>
    [Fact]
    public async Task ImpersonateTenant_ShouldReturnUnprocessableEntity_WhenTicketIsMissing()
    {
        // Arrange
        var tenant = Tenant.Create("Beta Inc", "11222333000181", "beta").Value;
        var client = CreateTestClient(tenant);

        var request = new ImpersonateTenantApiRequest(
            SuperAdminId: Guid.NewGuid(),
            SupportTicketId: "", // Invalid
            Reason: "Diagnóstico de erro",
            DurationMinutes: 30);

        // Act
        var response = await client.PostAsJsonAsync($"/api/v1/tenants/{tenant.Id.Value}/impersonate", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    /// <summary>
    /// Verifies that POST returns 404 NotFound when tenant does not exist.
    /// </summary>
    [Fact]
    public async Task ImpersonateTenant_ShouldReturnNotFound_WhenTenantDoesNotExist()
    {
        // Arrange
        var client = CreateTestClient(null);
        var tenantId = Guid.NewGuid();

        var request = new ImpersonateTenantApiRequest(
            SuperAdminId: Guid.NewGuid(),
            SupportTicketId: "INC-9999",
            Reason: "Diagnóstico de erro em relatórios de anúncios",
            DurationMinutes: 30);

        // Act
        var response = await client.PostAsJsonAsync($"/api/v1/tenants/{tenantId}/impersonate", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    /// <summary>
    /// Verifies that POST /api/v1/tenants/{tenantId}/impersonate/{sessionId}/terminate returns HTTP 200 when session exists.
    /// </summary>
    [Fact]
    public async Task TerminateImpersonation_ShouldReturnOk_WhenSessionExists()
    {
        // Arrange
        var tenant = Tenant.Create("Beta Inc", "11222333000181", "beta").Value;
        var session = ImpersonationSession.Create(
            tenant.Id,
            Guid.NewGuid(),
            "INC-12345",
            "Atendimento de suporte para verificação",
            30,
            DateTime.UtcNow).Value;

        var client = CreateTestClient(tenant, session);
        var request = new TerminateImpersonationApiRequest("Atendimento finalizado com sucesso");

        // Act
        var response = await client.PostAsJsonAsync($"/api/v1/tenants/{tenant.Id.Value}/impersonate/{session.Id}/terminate", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// Verifies that POST terminate returns HTTP 404 when session does not exist.
    /// </summary>
    [Fact]
    public async Task TerminateImpersonation_ShouldReturnNotFound_WhenSessionDoesNotExist()
    {
        // Arrange
        var tenant = Tenant.Create("Beta Inc", "11222333000181", "beta").Value;
        var client = CreateTestClient(tenant, null);
        var request = new TerminateImpersonationApiRequest("Fechamento");

        // Act
        var response = await client.PostAsJsonAsync($"/api/v1/tenants/{tenant.Id.Value}/impersonate/{Guid.NewGuid()}/terminate", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private sealed class FakeTenantRepository : ITenantRepository
    {
        private readonly Tenant? _tenant;

        public FakeTenantRepository(Tenant? tenant)
        {
            _tenant = tenant;
        }

        public Task<Tenant?> GetByIdAsync(TenantId id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_tenant);
        }

        public Task<Tenant?> GetBySubdomainAsync(string subdomain, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_tenant);
        }

        public Task<IReadOnlyList<Tenant>> GetTenantsForDunningEvaluationAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Tenant>>(_tenant != null ? new[] { _tenant } : Array.Empty<Tenant>());
        }

        public Task AddAsync(Tenant entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Update(Tenant entity) { }
        public void Remove(Tenant entity) { }
    }

    private sealed class FakeImpersonationSessionRepository : IImpersonationSessionRepository
    {
        private readonly ImpersonationSession? _session;

        public FakeImpersonationSessionRepository(ImpersonationSession? session = null)
        {
            _session = session;
        }

        public Task AddAsync(ImpersonationSession entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<ImpersonationSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(_session);
        public void Update(ImpersonationSession entity) { }
        public void Remove(ImpersonationSession entity) { }
        public Task<IReadOnlyList<ImpersonationSession>> GetActiveByTenantIdAsync(TenantId tenantId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ImpersonationSession>>(_session != null ? new[] { _session } : Array.Empty<ImpersonationSession>());
        public Task<ImpersonationSession?> GetActiveSessionByIdAsync(Guid sessionId, DateTime referenceUtc, CancellationToken cancellationToken = default) =>
            Task.FromResult(_session);
    }

    private sealed class FakeMasterAuditService : Master.Application.Auditing.IMasterAuditService
    {
        public Task<Result<Guid>> RecordAsync(
            string action,
            string resource,
            string? resourceId = null,
            string? details = null,
            Guid? tenantId = null,
            string? ipAddress = null,
            IEnumerable<string>? additionalTags = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result<Guid>.Success(Guid.NewGuid()));
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<int> CommitAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
    }

    private sealed class FakeDunningEngineService : Master.Application.Billing.Dunning.IDunningEngineService
    {
        public Task<Result<Master.Application.Billing.Dunning.DunningExecutionSummary>> ProcessDunningCycleAsync(
            DateTime? referenceDateUtc = null,
            CancellationToken cancellationToken = default)
        {
            var summary = new Master.Application.Billing.Dunning.DunningExecutionSummary(0, 0, 0, 0, DateTime.UtcNow);
            return Task.FromResult(Result<Master.Application.Billing.Dunning.DunningExecutionSummary>.Success(summary));
        }
    }

    private sealed class ImpersonateResultEnvelope
    {
        public bool IsSuccess { get; set; }
        public ImpersonateTenantResponse? Value { get; set; }
        public Error? Error { get; set; }
    }
}
