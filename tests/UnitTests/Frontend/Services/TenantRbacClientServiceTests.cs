using System.Net;
using System.Text.Json;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using FluentAssertions;
using NSubstitute;
using Tenants.Application.Audit.DTOs;
using Tenants.Application.Rbac.DTOs;
using UnitTests.Frontend.Common;
using WebApp.Services;
using WebApp.State;
using Xunit;

namespace UnitTests.Frontend.Services;

/// <summary>
/// Testes unitários para <see cref="TenantRbacClientService"/> validando a comunicação HTTP
/// para consulta de matriz RBAC, permissões de usuário, alteração de papel e auditoria.
/// </summary>
public sealed class TenantRbacClientServiceTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly ITenantStateProvider _tenantStateProvider = Substitute.For<ITenantStateProvider>();
    private readonly Guid _testTenantId = Guid.NewGuid();

    /// <summary>
    /// Inicializa a suíte com o mock do TenantStateProvider.
    /// </summary>
    public TenantRbacClientServiceTests()
    {
        var tenantState = new TenantState(
            TenantId: _testTenantId,
            Name: "Agência Alpha",
            Slug: "alpha",
            CustomDomain: null,
            Branding: WebApp.State.TenantBranding.Default);
        _tenantStateProvider.CurrentTenant.Returns(tenantState);
    }

    /// <summary>
    /// Valida que GetRbacMatrixAsync faz GET em /api/v1/tenants/rbac/matrix com cabeçalho X-Tenant-Id.
    /// </summary>
    [Fact]
    public async Task GetRbacMatrixAsync_DeveRetornarMatrizComSucesso()
    {
        // Arrange
        var matrixDto = new TenantRbacMatrixDto(
            new Dictionary<string, IReadOnlyList<string>>
            {
                ["Owner"] = ["ManageBilling", "ManageSettings"]
            },
            ["ManageBilling", "ManageSettings"]);

        var apiResult = Result<TenantRbacMatrixDto>.Success(matrixDto);

        var handler = new TestHttpMessageHandler((request, _) =>
        {
            request.Method.Should().Be(HttpMethod.Get);
            request.RequestUri!.AbsolutePath.Should().Be("/api/v1/tenants/rbac/matrix");
            request.Headers.GetValues("X-Tenant-Id").Should().Contain(_testTenantId.ToString());

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(apiResult, JsonOptions))
            };
        });

        var service = new TenantRbacClientService(new HttpClient(handler) { BaseAddress = new Uri("https://localhost") }, _tenantStateProvider);

        // Act
        var result = await service.GetRbacMatrixAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Roles.Should().ContainKey("Owner");
    }

    /// <summary>
    /// Valida que ChangeUserRoleAsync envia PUT para /api/v1/tenants/users/{id}/role com cabeçalho X-Tenant-Id.
    /// </summary>
    [Fact]
    public async Task ChangeUserRoleAsync_DeveEnviarRequisicaoERetornarSucesso()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var apiResult = Result.Success();

        var handler = new TestHttpMessageHandler((request, _) =>
        {
            request.Method.Should().Be(HttpMethod.Put);
            request.RequestUri!.AbsolutePath.Should().Be($"/api/v1/tenants/users/{userId}/role");
            request.Headers.GetValues("X-Tenant-Id").Should().Contain(_testTenantId.ToString());

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(apiResult, JsonOptions))
            };
        });

        var service = new TenantRbacClientService(new HttpClient(handler) { BaseAddress = new Uri("https://localhost") }, _tenantStateProvider);

        // Act
        var result = await service.ChangeUserRoleAsync(userId, TenantRole.SquadLeader);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    /// <summary>
    /// Valida que GetAuditLogsAsync faz GET em /api/v1/tenants/audit-logs com parâmetros e retorna lista paginada.
    /// </summary>
    [Fact]
    public async Task GetAuditLogsAsync_DeveRetornarLogsPaginados()
    {
        // Arrange
        var responseDto = new TenantAuditLogsResponse([], 0, 1, 50);
        var apiResult = Result<TenantAuditLogsResponse>.Success(responseDto);

        var handler = new TestHttpMessageHandler((request, _) =>
        {
            request.Method.Should().Be(HttpMethod.Get);
            request.RequestUri!.AbsolutePath.Should().Be("/api/v1/tenants/audit-logs");
            request.Headers.GetValues("X-Tenant-Id").Should().Contain(_testTenantId.ToString());

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(apiResult, JsonOptions))
            };
        });

        var service = new TenantRbacClientService(new HttpClient(handler) { BaseAddress = new Uri("https://localhost") }, _tenantStateProvider);

        // Act
        var result = await service.GetAuditLogsAsync(page: 1, pageSize: 50);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(0);
    }
}
