using System.Net;
using System.Text.Json;
using BackofficeApp.Services;
using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using Master.Application.Plans.DTOs;
using Master.Application.Tenants.Commands.RegisterTenantOnboarding;
using Master.Application.Tenants.Queries.CheckSubdomainAvailability;
using Master.Application.Tenants.Queries.CheckTaxDocumentAvailability;
using Master.Application.Tenants.Queries.GetTenantDetails;
using Master.Application.Users.DTOs;
using Master.Domain.Tenants;
using WebApp.Models;
using Xunit;
using WebAppTenantOnboardingService = WebApp.Services.TenantOnboardingClientService;
using WebAppTenantDirectoryService = WebApp.Services.TenantDirectoryService;
using WebAppPlanManagementService = WebApp.Services.PlanManagementService;

namespace UnitTests.Frontend.Services;

/// <summary>
/// Testes unitários para validar o comportamento dos clientes HTTP fortemente tipados do frontend.
/// Garante que nenhuma chamada acesse banco de dados diretamente e que o envelope Result seja tratado com precisão.
/// </summary>
public sealed class HttpClientServicesTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task TenantOnboardingClientService_CheckSubdomainAvailabilityAsync_ShouldCallApiAndReturnSuccess()
    {
        // Arrange
        var expectedResponse = Result<SubdomainAvailabilityResponse>.Success(new SubdomainAvailabilityResponse(
            Subdomain: "acme",
            IsAvailable: true,
            Reason: null,
            SuggestedAlternative: null));

        var handler = new TestHttpMessageHandler((request, cancellationToken) =>
        {
            request.RequestUri!.PathAndQuery.Should().Contain("/api/v1/tenants/check-subdomain?subdomain=acme");
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(expectedResponse, JsonOptions))
            };
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost:7001") };
        var service = new WebAppTenantOnboardingService(httpClient);

        // Act
        var result = await service.CheckSubdomainAvailabilityAsync("acme");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsAvailable.Should().BeTrue();
        result.Value.Subdomain.Should().Be("acme");
    }

    [Fact]
    public async Task TenantOnboardingClientService_CheckTaxDocumentAvailabilityAsync_ShouldCallApiAndReturnSuccess()
    {
        // Arrange
        var expectedResponse = Result<TaxDocumentAvailabilityResponse>.Success(new TaxDocumentAvailabilityResponse(
            Document: "52998224725",
            FormattedDocument: "529.982.247-25",
            IsValid: true,
            IsAvailable: true,
            DocumentType: "CPF",
            Reason: null));

        var handler = new TestHttpMessageHandler((request, cancellationToken) =>
        {
            request.RequestUri!.PathAndQuery.Should().Contain("/api/v1/tenants/check-document?document=529.982.247-25");
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(expectedResponse, JsonOptions))
            };
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost:7001") };
        var service = new WebAppTenantOnboardingService(httpClient);

        // Act
        var result = await service.CheckTaxDocumentAvailabilityAsync("529.982.247-25");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsValid.Should().BeTrue();
        result.Value.IsAvailable.Should().BeTrue();
        result.Value.DocumentType.Should().Be("CPF");
        result.Value.Document.Should().Be("52998224725");
    }

    [Fact]
    public async Task TenantDirectoryService_GetTenantsAsync_ShouldCallApiAndMapToViewModels()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var apiResponse = Result<IReadOnlyList<TenantDetailsResponse>>.Success(new List<TenantDetailsResponse>
        {
            new(
                Id: tenantId,
                CompanyName: "Empresa Teste",
                Cnpj: "12345678000199",
                Subdomain: "empresa",
                Status: "Active",
                Tier: "Pro",
                SubscriptionExpiresAtUtc: DateTime.UtcNow.AddYears(1),
                CreatedAtUtc: DateTime.UtcNow)
        });

        var handler = new TestHttpMessageHandler((request, cancellationToken) =>
        {
            request.RequestUri!.PathAndQuery.Should().Be("/api/v1/tenants");
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(apiResponse, JsonOptions))
            };
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost:7001") };
        var service = new WebAppTenantDirectoryService(httpClient);

        // Act
        var result = await service.GetTenantsAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value[0].Id.Should().Be(tenantId);
        result.Value[0].CompanyName.Should().Be("Empresa Teste");
        result.Value[0].Status.Should().Be("Active");
        result.Value[0].Tier.Should().Be("Pro");
    }

    [Fact]
    public async Task PlanManagementService_CreatePlanAsync_ShouldPostApiAndReturnNewId()
    {
        // Arrange
        var newPlanId = Guid.NewGuid();
        var apiResponse = Result<Guid>.Success(newPlanId);

        var handler = new TestHttpMessageHandler((request, cancellationToken) =>
        {
            request.RequestUri!.PathAndQuery.Should().Be("/api/v1/plans");
            request.Method.Should().Be(HttpMethod.Post);
            return new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent(JsonSerializer.Serialize(apiResponse, JsonOptions))
            };
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost:7001") };
        var service = new WebAppPlanManagementService(httpClient);

        var model = new PlanFormViewModel
        {
            Name = "Plano Pro",
            Description = "Descrição",
            Tier = SubscriptionTier.Pro,
            MonthlyPrice = 499m,
            AnnualDiscountPercentage = 20,
            MaxSeats = 10,
            MaxWorkspaces = 5,
            MonthlyAdSpendCap = 100000m
        };

        // Act
        var result = await service.CreatePlanAsync(model);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(newPlanId);
    }

    [Fact]
    public async Task BackofficeAuthClientService_AuthenticateAsync_ShouldPostApiAndReturnUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var authUser = new AuthenticatedBackofficeUserDto(
            Id: userId,
            Email: "admin@admetricspro.internal",
            FullName: "Super Administrador",
            Roles: new[] { "SuperAdmin" },
            LastLoginAtUtc: DateTime.UtcNow);

        var apiResponse = Result<AuthenticatedBackofficeUserDto>.Success(authUser);

        var handler = new TestHttpMessageHandler((request, cancellationToken) =>
        {
            request.RequestUri!.PathAndQuery.Should().Be("/api/v1/admin/auth/login");
            request.Method.Should().Be(HttpMethod.Post);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(apiResponse, JsonOptions))
            };
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost:7001") };
        var service = new BackofficeAuthClientService(httpClient);

        // Act
        var result = await service.AuthenticateAsync("admin@admetricspro.internal", "SenhaForte123!");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(userId);
        result.Value.Email.Should().Be("admin@admetricspro.internal");
        result.Value.Roles.Should().Contain("SuperAdmin");
    }

    private sealed class TestHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> _handler;

        public TestHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_handler(request, cancellationToken));
        }
    }
}
