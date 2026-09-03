using System.Reflection;
using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using MediatR;

namespace UnitTests.Backend.Compliance;

/// <summary>
/// Suíte de testes automatizados de conformidade arquitetural com o AGENTS.md.
/// Valida guardrails inegociáveis: Pattern Result&lt;T&gt;, limites de contexto do Monólito Modular,
/// suporte obrigatório a CancellationToken e isolamento de persistência.
/// </summary>
public sealed class AgentsArchitectureComplianceTests
{
    private static readonly Assembly DomainAssembly = typeof(Master.Domain.Tenants.Tenant).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(Master.Application.Tenants.Commands.CreateTenant.CreateTenantCommand).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(Master.Infrastructure.Persistence.MasterDbContext).Assembly;
    private static readonly Assembly BuildingBlocksDomainAssembly = typeof(BuildingBlocks.Domain.Primitives.Result).Assembly;
    private static readonly Assembly BuildingBlocksApplicationAssembly = typeof(BuildingBlocks.Application.Behaviors.ValidationBehavior<,>).Assembly;

    /// <summary>
    /// AGENTS.md Seção 1 (Princípio 5) e Seção 3.1:
    /// Todo command handler ou query handler MediatR deve retornar Result ou Result&lt;T&gt;.
    /// Proibido retorno de tipos brutos não encapsulados ou fluxo baseado em exceptions.
    /// </summary>
    [Fact]
    public void AllMediatRRequestHandlers_MustReturnResultOrResultOfT()
    {
        // Arrange
        var handlerInterfaceType = typeof(IRequestHandler<,>);

        var handlerTypes = ApplicationAssembly.GetExportedTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .Where(t => t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerInterfaceType))
            .ToList();

        handlerTypes.Should().NotBeEmpty("A camada de aplicação deve conter handlers MediatR.");

        var nonResultHandlers = new List<string>();

        // Act & Assert
        foreach (var handler in handlerTypes)
        {
            var handlerInterfaces = handler.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerInterfaceType);

            foreach (var hi in handlerInterfaces)
            {
                var responseType = hi.GetGenericArguments()[1];
                var isResult = responseType == typeof(Result) ||
                               (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>));

                if (!isResult)
                {
                    nonResultHandlers.Add($"{handler.FullName} implementa {hi} com tipo de retorno inválido '{responseType.FullName}'.");
                }
            }
        }

        nonResultHandlers.Should().BeEmpty(
            $"Todos os handlers MediatR devem retornar Result ou Result<T> conforme AGENTS.md Seção 1.5 e 3.1:{Environment.NewLine}{string.Join(Environment.NewLine, nonResultHandlers)}");
    }

    /// <summary>
    /// AGENTS.md Seção 1 (Princípio 2) e Seção 3.3:
    /// O Domínio deve ser puro e independente de detalhes de infraestrutura ou apresentação.
    /// Não deve referenciar Entity Framework, ASP.NET Core ou Infrastructure.
    /// </summary>
    [Theory]
    [InlineData("Master.Domain")]
    [InlineData("BuildingBlocks.Domain")]
    public void DomainLayer_MustNotReferenceInfrastructureOrPresentation(string domainAssemblyName)
    {
        // Arrange
        var assembly = domainAssemblyName == "Master.Domain" ? DomainAssembly : BuildingBlocksDomainAssembly;
        var referencedAssemblies = assembly.GetReferencedAssemblies()
            .Select(a => a.Name)
            .Where(n => n is not null)
            .ToList();

        // Assert
        referencedAssemblies.Should().NotContain(name => name!.Contains("Infrastructure"),
            $"{domainAssemblyName} não pode depender da camada de Infrastructure.");

        referencedAssemblies.Should().NotContain(name => name!.Contains("EntityFrameworkCore"),
            $"{domainAssemblyName} não pode depender de persistência (EF Core).");

        referencedAssemblies.Should().NotContain(name => name!.Contains("AspNetCore"),
            $"{domainAssemblyName} não pode depender de ASP.NET Core.");
    }

    /// <summary>
    /// AGENTS.md Seção 1 (Princípio 2):
    /// A camada de Aplicação não deve referenciar a camada de Infraestrutura ou WebApi diretamente.
    /// </summary>
    [Fact]
    public void ApplicationLayer_MustNotReferenceInfrastructureOrWebApi()
    {
        // Arrange
        var referencedAssemblies = ApplicationAssembly.GetReferencedAssemblies()
            .Select(a => a.Name)
            .Where(n => n is not null)
            .ToList();

        // Assert
        referencedAssemblies.Should().NotContain(name => name!.Contains("Master.Infrastructure"),
            "Master.Application não pode referenciar diretamente Master.Infrastructure.");

        referencedAssemblies.Should().NotContain(name => name!.Contains("WebApi"),
            "Master.Application não pode referenciar WebApi.");
    }

    /// <summary>
    /// AGENTS.md Seção 3.4:
    /// Todo método assíncrono declarado nas interfaces de Repositório deve aceitar CancellationToken obrigatoriamente.
    /// </summary>
    [Fact]
    public void AllRepositoryInterfaces_AsyncMethods_MustAcceptCancellationToken()
    {
        // Arrange
        var repositoryInterfaces = ApplicationAssembly.GetExportedTypes()
            .Concat(BuildingBlocksApplicationAssembly.GetExportedTypes())
            .Where(t => t.IsInterface && t.Name.EndsWith("Repository"))
            .ToList();

        repositoryInterfaces.Should().NotBeEmpty("Devem existir interfaces de repositório na Aplicação.");

        var violatingMethods = new List<string>();

        // Act & Assert
        foreach (var repoInterface in repositoryInterfaces)
        {
            var methods = repoInterface.GetMethods(BindingFlags.Public | BindingFlags.Instance);

            foreach (var method in methods)
            {
                var isAsync = typeof(Task).IsAssignableFrom(method.ReturnType);
                if (!isAsync)
                    continue;

                var hasCancellationToken = method.GetParameters()
                    .Any(p => p.ParameterType == typeof(CancellationToken));

                if (!hasCancellationToken)
                {
                    violatingMethods.Add($"{repoInterface.Name}.{method.Name} não possui parâmetro CancellationToken.");
                }
            }
        }

        violatingMethods.Should().BeEmpty(
            $"Todos os métodos assíncronos em repositórios devem receber CancellationToken (AGENTS.md Seção 3.4):{Environment.NewLine}{string.Join(Environment.NewLine, violatingMethods)}");
    }

    /// <summary>
    /// AGENTS.md Seção 3.4:
    /// As operações de persistência transacional devem expor UnitOfWork.
    /// </summary>
    [Fact]
    public void MasterInfrastructure_MustImplementUnitOfWork()
    {
        // Arrange
        var uowInterface = typeof(BuildingBlocks.Application.Persistence.IUnitOfWork);
        var uowImplementations = InfrastructureAssembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface && uowInterface.IsAssignableFrom(t))
            .ToList();

        // Assert
        uowImplementations.Should().NotBeEmpty("Master.Infrastructure deve implementar IUnitOfWork.");
    }
}
