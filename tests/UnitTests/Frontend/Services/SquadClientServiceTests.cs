using System.Net;
using System.Text.Json;
using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using NSubstitute;
using Tenants.Application.Squads.DTOs;
using Tenants.Application.Workspaces.DTOs;
using UnitTests.Frontend.Common;
using WebApp.Models;
using WebApp.Services;
using WebApp.State;
using Xunit;

namespace UnitTests.Frontend.Services;

/// <summary>
/// Testes unitários para <see cref="SquadClientService"/> validando a comunicação HTTP com a Web API
/// e o encapsulamento no padrão <see cref="Result{T}"/>.
/// </summary>
public sealed class SquadClientServiceTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly ITenantStateProvider _tenantStateProvider = Substitute.For<ITenantStateProvider>();
    private readonly Guid _testTenantId = Guid.NewGuid();

    /// <summary>
    /// Inicializa a suíte mockando o tenant ativo.
    /// </summary>
    public SquadClientServiceTests()
    {
        var tenantState = new TenantState(
            TenantId: _testTenantId,
            Name: "Agência Alpha",
            Slug: "alpha",
            CustomDomain: null,
            Branding: TenantBranding.Default);
        _tenantStateProvider.CurrentTenant.Returns(tenantState);
    }

    /// <summary>
    /// Valida que CreateSquadAsync envia POST para /api/v1/squads com cabeçalho X-Tenant-Id e retorna Result com ID.
    /// </summary>
    [Fact]
    public async Task CreateSquadAsync_ComDadosValidos_DeveEnviarRequisicaoERetornarId()
    {
        // Arrange
        var squadId = Guid.NewGuid();
        var apiResult = Result<Guid>.Success(squadId);

        var handler = new TestHttpMessageHandler((request, _) =>
        {
            request.Method.Should().Be(HttpMethod.Post);
            request.RequestUri!.AbsolutePath.Should().Be("/api/v1/squads");
            request.Headers.GetValues("X-Tenant-Id").Should().Contain(_testTenantId.ToString());

            return new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent(JsonSerializer.Serialize(apiResult, JsonOptions))
            };
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost:7001") };
        var service = new SquadClientService(httpClient, _tenantStateProvider);
        var model = new CreateSquadModel("Squad Varejo", "Focado em e-commerces");

        // Act
        var result = await service.CreateSquadAsync(model);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(squadId);
    }

    /// <summary>
    /// Valida que GetSquadsAsync envia GET para /api/v1/squads e deserializa a lista de resumos.
    /// </summary>
    [Fact]
    public async Task GetSquadsAsync_DeveRetornarListaDeSquads()
    {
        // Arrange
        var squads = new List<SquadSummaryDto>
        {
            new(Guid.NewGuid(), "Squad Alpha", "Descrição", true, 3, 5, DateTime.UtcNow)
        };
        var apiResult = Result<IReadOnlyList<SquadSummaryDto>>.Success(squads);

        var handler = new TestHttpMessageHandler((request, _) =>
        {
            request.Method.Should().Be(HttpMethod.Get);
            request.RequestUri!.PathAndQuery.Should().Be("/api/v1/squads?activeOnly=true");
            request.Headers.GetValues("X-Tenant-Id").Should().Contain(_testTenantId.ToString());

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(apiResult, JsonOptions))
            };
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost:7001") };
        var service = new SquadClientService(httpClient, _tenantStateProvider);

        // Act
        var result = await service.GetSquadsAsync(activeOnly: true);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value.First().Name.Should().Be("Squad Alpha");
    }

    /// <summary>
    /// Valida que GetSquadByIdAsync envia GET para /api/v1/squads/{id} e retorna os detalhes completos.
    /// </summary>
    [Fact]
    public async Task GetSquadByIdAsync_DeveRetornarDetalhesCompletos()
    {
        // Arrange
        var squadId = Guid.NewGuid();
        var details = new SquadDetailsDto(
            squadId,
            "Squad Performance",
            "Desc",
            true,
            new List<SquadMemberDto>(),
            new List<SquadWorkspaceDto>(),
            DateTime.UtcNow,
            null);
        var apiResult = Result<SquadDetailsDto>.Success(details);

        var handler = new TestHttpMessageHandler((request, _) =>
        {
            request.Method.Should().Be(HttpMethod.Get);
            request.RequestUri!.AbsolutePath.Should().Be($"/api/v1/squads/{squadId}");

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(apiResult, JsonOptions))
            };
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost:7001") };
        var service = new SquadClientService(httpClient, _tenantStateProvider);

        // Act
        var result = await service.GetSquadByIdAsync(squadId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(squadId);
        result.Value.Name.Should().Be("Squad Performance");
    }

    /// <summary>
    /// Valida que AddSquadMemberAsync envia POST para /api/v1/squads/{id}/members.
    /// </summary>
    [Fact]
    public async Task AddSquadMemberAsync_DeveEnviarRequisicaoERetornarSucesso()
    {
        // Arrange
        var squadId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var apiResult = Result.Success();

        var handler = new TestHttpMessageHandler((request, _) =>
        {
            request.Method.Should().Be(HttpMethod.Post);
            request.RequestUri!.AbsolutePath.Should().Be($"/api/v1/squads/{squadId}/members");

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(apiResult, JsonOptions))
            };
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost:7001") };
        var service = new SquadClientService(httpClient, _tenantStateProvider);

        // Act
        var result = await service.AddSquadMemberAsync(squadId, userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    /// <summary>
    /// Valida que AssignSquadWorkspaceAsync envia POST para /api/v1/squads/{id}/workspaces.
    /// </summary>
    [Fact]
    public async Task AssignSquadWorkspaceAsync_DeveEnviarRequisicaoERetornarSucesso()
    {
        // Arrange
        var squadId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var apiResult = Result.Success();

        var handler = new TestHttpMessageHandler((request, _) =>
        {
            request.Method.Should().Be(HttpMethod.Post);
            request.RequestUri!.AbsolutePath.Should().Be($"/api/v1/squads/{squadId}/workspaces");

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(apiResult, JsonOptions))
            };
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost:7001") };
        var service = new SquadClientService(httpClient, _tenantStateProvider);

        // Act
        var result = await service.AssignSquadWorkspaceAsync(squadId, workspaceId);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    /// <summary>
    /// Valida que GetUserPortfolioAsync consulta /api/v1/squads/users/{userId}/portfolio e retorna lista de workspaces.
    /// </summary>
    [Fact]
    public async Task GetUserPortfolioAsync_DeveRetornarWorkspacesAutorizados()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var ws = new WorkspaceDto(Guid.NewGuid(), "Cliente Alpha", "123.456.789-09", 5000m, "Varejo", true, DateTime.UtcNow, null);
        var apiResult = Result<IReadOnlyList<WorkspaceDto>>.Success(new List<WorkspaceDto> { ws });

        var handler = new TestHttpMessageHandler((request, _) =>
        {
            request.Method.Should().Be(HttpMethod.Get);
            request.RequestUri!.AbsolutePath.Should().Be($"/api/v1/squads/users/{userId}/portfolio");

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(apiResult, JsonOptions))
            };
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost:7001") };
        var service = new SquadClientService(httpClient, _tenantStateProvider);

        // Act
        var result = await service.GetUserPortfolioAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(w => w.Name == "Cliente Alpha");
    }

    /// <summary>
    /// Valida que falha de rede retorna Failure sem estourar exceção para a camada de apresentação.
    /// </summary>
    [Fact]
    public async Task CreateSquadAsync_QuandoErroDeConexao_DeveRetornarResultFailure()
    {
        // Arrange
        var handler = new TestHttpMessageHandler((_, _) => throw new HttpRequestException("Falha de conexão"));
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost:7001") };
        var service = new SquadClientService(httpClient, _tenantStateProvider);

        // Act
        var result = await service.CreateSquadAsync(new CreateSquadModel("Squad"));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Squad.NetworkError");
    }
}
