using System.Net;
using System.Text.Json;
using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using NSubstitute;
using UnitTests.Frontend.Common;
using WebApp.Models;
using WebApp.Services;
using WebApp.State;
using Xunit;

namespace UnitTests.Frontend.Services;

/// <summary>
/// Testes unitários para <see cref="WorkspaceClientService"/>, incluindo notificação do catálogo após mutações bem-sucedidas.
/// </summary>
public sealed class WorkspaceClientServiceTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly ITenantStateProvider _tenantStateProvider = Substitute.For<ITenantStateProvider>();
    private readonly Guid _testTenantId = Guid.NewGuid();

    /// <summary>
    /// Inicializa a suíte mockando o tenant ativo.
    /// </summary>
    public WorkspaceClientServiceTests()
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
    /// Valida que CreateWorkspaceAsync notifica o catálogo apenas quando a API retorna sucesso.
    /// </summary>
    [Fact]
    public async Task CreateWorkspaceAsync_QuandoSucesso_DeveNotificarCatalogo()
    {
        var workspaceId = Guid.NewGuid();
        var handler = new TestHttpMessageHandler((_, _) => new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent(JsonSerializer.Serialize(Result<Guid>.Success(workspaceId), JsonOptions))
        });
        var notifier = Substitute.For<IWorkspaceCatalogNotifier>();
        var service = CreateService(handler, notifier);
        var model = new CreateWorkspaceModel("Cliente Novo", "12345678000195", 5000m, "E-commerce");

        var result = await service.CreateWorkspaceAsync(model);

        result.IsSuccess.Should().BeTrue();
        notifier.Received(1).NotifyChanged();
    }

    /// <summary>
    /// Valida que CreateWorkspaceAsync não notifica o catálogo quando a API retorna falha de negócio.
    /// </summary>
    [Fact]
    public async Task CreateWorkspaceAsync_QuandoFalha_DeveNaoNotificarCatalogo()
    {
        var handler = new TestHttpMessageHandler((_, _) => new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(JsonSerializer.Serialize(
                Result<Guid>.Failure(Error.Validation("Workspace.Limit", "Cota excedida.")), JsonOptions))
        });
        var notifier = Substitute.For<IWorkspaceCatalogNotifier>();
        var service = CreateService(handler, notifier);

        var result = await service.CreateWorkspaceAsync(new CreateWorkspaceModel("X", "12345678000195", 100m));

        result.IsSuccess.Should().BeFalse();
        notifier.DidNotReceive().NotifyChanged();
    }

    /// <summary>
    /// Valida que UpdateWorkspaceAsync notifica o catálogo apenas em sucesso.
    /// </summary>
    [Fact]
    public async Task UpdateWorkspaceAsync_QuandoSucesso_DeveNotificarCatalogo()
    {
        var handler = new TestHttpMessageHandler((_, _) => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(Result.Success(), JsonOptions))
        });
        var notifier = Substitute.For<IWorkspaceCatalogNotifier>();
        var service = CreateService(handler, notifier);

        var result = await service.UpdateWorkspaceAsync(
            Guid.NewGuid(),
            new UpdateWorkspaceModel("Nome", "12345678000195", 1000m, "E-commerce"));

        result.IsSuccess.Should().BeTrue();
        notifier.Received(1).NotifyChanged();
    }

    /// <summary>
    /// Valida que ToggleWorkspaceStatusAsync notifica o catálogo apenas em sucesso.
    /// </summary>
    [Fact]
    public async Task ToggleWorkspaceStatusAsync_QuandoSucesso_DeveNotificarCatalogo()
    {
        var handler = new TestHttpMessageHandler((_, _) => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(Result.Success(), JsonOptions))
        });
        var notifier = Substitute.For<IWorkspaceCatalogNotifier>();
        var service = CreateService(handler, notifier);

        var result = await service.ToggleWorkspaceStatusAsync(Guid.NewGuid());

        result.IsSuccess.Should().BeTrue();
        notifier.Received(1).NotifyChanged();
    }

    /// <summary>
    /// Valida que erros de rede não disparam notificação do catálogo.
    /// </summary>
    [Fact]
    public async Task CreateWorkspaceAsync_QuandoErroDeRede_DeveNaoNotificarCatalogo()
    {
        var handler = new TestHttpMessageHandler((_, _) => throw new HttpRequestException("offline"));
        var notifier = Substitute.For<IWorkspaceCatalogNotifier>();
        var service = CreateService(handler, notifier);

        var result = await service.CreateWorkspaceAsync(new CreateWorkspaceModel("X", "12345678000195", 100m));

        result.IsSuccess.Should().BeFalse();
        notifier.DidNotReceive().NotifyChanged();
    }

    private WorkspaceClientService CreateService(HttpMessageHandler handler, IWorkspaceCatalogNotifier notifier)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost:7001") };
        return new WorkspaceClientService(httpClient, _tenantStateProvider, catalogNotifier: notifier);
    }
}