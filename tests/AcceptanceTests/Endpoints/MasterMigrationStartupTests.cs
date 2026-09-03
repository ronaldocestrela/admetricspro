using BuildingBlocks.Application.Persistence;
using FluentAssertions;
using Master.Application.Repositories;
using Master.Application.Services;
using Master.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace AcceptanceTests.Endpoints;

/// <summary>
/// Testes de inicialização e injeção de dependência do catálogo MasterDb no host da WebApi.
/// </summary>
public sealed class MasterMigrationStartupTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    /// <summary>
    /// Inicializa a suíte de testes com a fábrica da WebApi.
    /// </summary>
    /// <param name="factory">Fábrica de aplicação WebApi.</param>
    public MasterMigrationStartupTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Garante que os serviços de catálogo MasterDb (DbContext, Runner, Repositório e UnitOfWork) são resolvidos corretamente pelo container da API.
    /// </summary>
    [Fact]
    public void WebApiHost_ShouldResolveMasterCatalogServices_FromDependencyInjection()
    {
        // Arrange & Act
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetService<MasterDbContext>();
        var runner = scope.ServiceProvider.GetService<IMasterDatabaseMigrationRunner>();
        var repository = scope.ServiceProvider.GetService<ITenantRepository>();
        var unitOfWork = scope.ServiceProvider.GetService<IUnitOfWork>();

        // Assert
        dbContext.Should().NotBeNull();
        runner.Should().NotBeNull();
        repository.Should().NotBeNull();
        unitOfWork.Should().NotBeNull();
    }
}
