using System.Net;
using System.Text.Json;
using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using Master.Application.Repositories;
using Master.Domain.Tenants;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AcceptanceTests.Endpoints;

/// <summary>
/// Testes de aceitação para o endpoint de reenvio de e-mail de boas-vindas (POST /api/v1/tenants/{id}/resend-welcome-email).
/// </summary>
public sealed class ResendWelcomeEmailEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="ResendWelcomeEmailEndpointTests"/>.
    /// </summary>
    /// <param name="factory">Fábrica da aplicação web.</param>
    public ResendWelcomeEmailEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Valida que o endpoint retorna HTTP 200 quando o tenant existe e o reenvio é bem-sucedido.
    /// </summary>
    [Fact]
    public async Task ResendWelcomeEmail_WhenTenantExists_ShouldReturnOk()
    {
        // Arrange
        var tenant = Tenant.Create(
            "Agência Alfa",
            "12345678000195",
            "agencia-alfa",
            SubscriptionTier.Pro,
            adminEmail: "alfa@agencia.com.br",
            adminFullName: "Alberto Costa").Value;

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddScoped<ITenantRepository>(_ => new FakeTenantRepository(tenant));
                services.AddScoped<ITenantNotificationLogRepository, FakeTenantNotificationLogRepository>();
                services.AddScoped<BuildingBlocks.Application.Persistence.IUnitOfWork, FakeUnitOfWork>();
            });
        }).CreateClient();

        // Act
        var response = await client.PostAsync($"/api/v1/tenants/{tenant.Id.Value}/resend-welcome-email", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var result = JsonSerializer.Deserialize<ResendResultEnvelope>(content, jsonOptions);

        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    /// <summary>
    /// Valida que o endpoint retorna HTTP 404 quando o tenant não é encontrado.
    /// </summary>
    [Fact]
    public async Task ResendWelcomeEmail_WhenTenantNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var missingTenantId = Guid.NewGuid();
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddScoped<ITenantRepository>(_ => new FakeTenantRepository(null));
            });
        }).CreateClient();

        // Act
        var response = await client.PostAsync($"/api/v1/tenants/{missingTenantId}/resend-welcome-email", null);

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
            return Task.FromResult(_tenant?.Id == id ? _tenant : null);
        }

        public Task<Tenant?> GetBySubdomainAsync(string subdomain, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_tenant);
        }

        public Task<Tenant?> GetByCnpjAsync(string cnpj, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_tenant);
        }

        public Task<IReadOnlyList<Tenant>> GetTenantsForDunningEvaluationAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Tenant>>(_tenant != null ? new[] { _tenant } : Array.Empty<Tenant>());
        }

        public Task<IReadOnlyList<Tenant>> GetTenantsForTrialNoticeEvaluationAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Tenant>>(_tenant != null ? new[] { _tenant } : Array.Empty<Tenant>());
        }

        public Task AddAsync(Tenant entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Update(Tenant entity) { }
        public void Remove(Tenant entity) { }
    }

    private sealed class FakeTenantNotificationLogRepository : ITenantNotificationLogRepository
    {
        public Task AddAsync(TenantNotificationLog log, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<IReadOnlySet<TrialNoticeType>> GetSentNoticeTypesForTenantAsync(TenantId tenantId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlySet<TrialNoticeType>>(new HashSet<TrialNoticeType>());
        }

        public Task<IReadOnlyList<TenantNotificationLog>> GetLogsByTenantIdAsync(TenantId tenantId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<TenantNotificationLog>>(Array.Empty<TenantNotificationLog>());
        }
    }

    private sealed class FakeUnitOfWork : BuildingBlocks.Application.Persistence.IUnitOfWork
    {
        public Task<int> CommitAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
    }

    private sealed record ResendResultEnvelope
    {
        public bool IsSuccess { get; init; }
        public bool IsFailure => !IsSuccess;
        public bool Value { get; init; }
    }
}
