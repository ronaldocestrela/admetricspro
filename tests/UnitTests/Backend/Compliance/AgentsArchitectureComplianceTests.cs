using System.Reflection;
using System.Xml.Linq;
using BuildingBlocks.Application.Behaviors;
using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
    private static readonly Assembly TenantsApplicationAssembly = typeof(global::Tenants.Application.Auth.Commands.AuthenticateTenantUser.AuthenticateTenantUserCommand).Assembly;

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

        var socialPublishingApplication = SocialPublishingComplianceAssemblyLoader.TryLoad("SocialPublishing.Application");
        socialPublishingApplication.Should().NotBeNull("SocialPublishing.Application deve existir na solution.");

        var handlerTypes = ApplicationAssembly.GetExportedTypes()
            .Concat(TenantsApplicationAssembly.GetExportedTypes())
            .Concat(socialPublishingApplication!.GetExportedTypes())
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
    [Theory]
    [InlineData("Master.Application", "Master.Infrastructure")]
    [InlineData("Tenants.Application", "Tenants.Infrastructure")]
    public void ApplicationLayer_MustNotReferenceInfrastructureOrWebApi(string applicationAssemblyName, string forbiddenInfrastructureName)
    {
        // Arrange
        var assembly = applicationAssemblyName == "Master.Application" ? ApplicationAssembly : TenantsApplicationAssembly;
        var referencedAssemblies = assembly.GetReferencedAssemblies()
            .Select(a => a.Name)
            .Where(n => n is not null)
            .ToList();

        // Assert
        referencedAssemblies.Should().NotContain(name => name!.Contains(forbiddenInfrastructureName),
            $"{applicationAssemblyName} não pode referenciar diretamente {forbiddenInfrastructureName}.");

        referencedAssemblies.Should().NotContain(name => name!.Contains("WebApi"),
            $"{applicationAssemblyName} não pode referenciar WebApi.");
    }

    /// <summary>
    /// AGENTS.md Seção 3.4:
    /// Todo método assíncrono declarado nas interfaces de Repositório deve aceitar CancellationToken obrigatoriamente.
    /// </summary>
    [Fact]
    public void AllRepositoryInterfaces_AsyncMethods_MustAcceptCancellationToken()
    {
        // Arrange
        var socialPublishingApplication = SocialPublishingComplianceAssemblyLoader.TryLoad("SocialPublishing.Application");
        socialPublishingApplication.Should().NotBeNull("SocialPublishing.Application deve existir na solution.");

        var repositoryInterfaces = ApplicationAssembly.GetExportedTypes()
            .Concat(BuildingBlocksApplicationAssembly.GetExportedTypes())
            .Concat(socialPublishingApplication!.GetExportedTypes())
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

    /// <summary>
    /// Roadmap SocialPublishing Subfase 1.3: Infrastructure expõe unit of work dedicado ao módulo.
    /// </summary>
    [Fact]
    public void SocialPublishingInfrastructure_MustImplementSocialPublishingUnitOfWork()
    {
        var assembly = SocialPublishingComplianceAssemblyLoader.Load("SocialPublishing.Infrastructure");
        var uowInterface = typeof(global::SocialPublishing.Application.Abstractions.ISocialPublishingUnitOfWork);
        var implementations = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface && uowInterface.IsAssignableFrom(t))
            .ToList();

        implementations.Should().NotBeEmpty("SocialPublishing.Infrastructure deve implementar ISocialPublishingUnitOfWork.");
    }

    /// <summary>
    /// Roadmap SocialPublishing Subfase 1.1: SocialPublishing.Domain permanece puro (somente BuildingBlocks.Domain).
    /// </summary>
    [Fact]
    public void SocialPublishingDomain_MustNotReferenceInfrastructureOrPresentation()
    {
        var assembly = SocialPublishingComplianceAssemblyLoader.Load("SocialPublishing.Domain");
        var referencedAssemblies = assembly.GetReferencedAssemblies()
            .Select(a => a.Name)
            .Where(n => n is not null)
            .ToList();

        referencedAssemblies.Should().NotContain(name => name!.Contains("Infrastructure"),
            "SocialPublishing.Domain não pode depender da camada de Infrastructure.");
        referencedAssemblies.Should().NotContain(name => name!.Contains("EntityFrameworkCore"),
            "SocialPublishing.Domain não pode depender de persistência (EF Core).");
        referencedAssemblies.Should().NotContain(name => name!.Contains("AspNetCore"),
            "SocialPublishing.Domain não pode depender de ASP.NET Core.");

        var projectReferences = SocialPublishingComplianceAssemblyLoader.GetProjectReferenceAssemblyNames(
            "SocialPublishing.Domain");
        projectReferences.Should().BeEquivalentTo(["BuildingBlocks.Domain"],
            "SocialPublishing.Domain deve referenciar apenas BuildingBlocks.Domain.");
    }

    /// <summary>
    /// Roadmap SocialPublishing Subfase 1.1: Application isolada de Infrastructure, WebApi e outros módulos.
    /// </summary>
    [Fact]
    public void SocialPublishingApplication_MustNotReferenceForbiddenAssemblies()
    {
        var assembly = SocialPublishingComplianceAssemblyLoader.Load("SocialPublishing.Application");
        var referencedAssemblies = assembly.GetReferencedAssemblies()
            .Select(a => a.Name)
            .Where(n => n is not null)
            .ToList();

        referencedAssemblies.Should().NotContain(name => name!.Contains("SocialPublishing.Infrastructure"),
            "SocialPublishing.Application não pode referenciar SocialPublishing.Infrastructure.");
        referencedAssemblies.Should().NotContain(name => name!.Contains("WebApi"),
            "SocialPublishing.Application não pode referenciar WebApi.");
        referencedAssemblies.Should().NotContain(name => name!.StartsWith("Integrations.", StringComparison.Ordinal),
            "SocialPublishing.Application não pode referenciar Integrations.*.");
        referencedAssemblies.Should().NotContain(name => name!.StartsWith("Analytics.", StringComparison.Ordinal),
            "SocialPublishing.Application não pode referenciar Analytics.*.");
        referencedAssemblies.Should().NotContain(name => name!.Contains("Tenants.Infrastructure"),
            "SocialPublishing.Application não pode referenciar Tenants.Infrastructure.");
        referencedAssemblies.Should().NotContain(name => name!.EndsWith(".Infrastructure", StringComparison.Ordinal),
            "SocialPublishing.Application não pode referenciar *.Infrastructure.");
    }

    /// <summary>
    /// Roadmap SocialPublishing Subfase 1.1: Infrastructure não acopla Integrations nem Analytics.
    /// </summary>
    [Fact]
    public void SocialPublishingInfrastructure_MustNotReferenceIntegrationsOrAnalyticsInfrastructure()
    {
        var assembly = SocialPublishingComplianceAssemblyLoader.Load("SocialPublishing.Infrastructure");
        var referencedAssemblies = assembly.GetReferencedAssemblies()
            .Select(a => a.Name)
            .Where(n => n is not null)
            .ToList();

        referencedAssemblies.Should().NotContain(name => name!.Contains("Integrations.Infrastructure"),
            "SocialPublishing.Infrastructure não pode referenciar Integrations.Infrastructure.");
        referencedAssemblies.Should().NotContain(name => name!.Contains("Analytics.Infrastructure"),
            "SocialPublishing.Infrastructure não pode referenciar Analytics.Infrastructure.");
    }

    /// <summary>
    /// Subfase 6.3: Integrations.Application referencia apenas SocialPublishing.Contracts entre módulos Social.
    /// </summary>
    [Fact]
    public void IntegrationsApplication_MayReferenceSocialPublishingContractsOnly()
    {
        var assembly = Assembly.Load(new AssemblyName("Integrations.Application"));
        var referencedAssemblies = assembly.GetReferencedAssemblies()
            .Select(a => a.Name)
            .Where(n => n is not null)
            .ToList();

        referencedAssemblies.Should().NotContain(name => name!.StartsWith("SocialPublishing.Domain", StringComparison.Ordinal));
        referencedAssemblies.Should().NotContain(name => name!.StartsWith("SocialPublishing.Application", StringComparison.Ordinal));
        referencedAssemblies.Should().NotContain(name => name!.StartsWith("SocialPublishing.Infrastructure", StringComparison.Ordinal));
        referencedAssemblies.Should().Contain("SocialPublishing.Contracts");
    }

    /// <summary>
    /// Subfase 6.2: Analytics.Application referencia apenas SocialPublishing.Contracts entre módulos Social.
    /// </summary>
    [Fact]
    public void AnalyticsApplication_MayReferenceSocialPublishingContractsOnly()
    {
        var assembly = Assembly.Load(new AssemblyName("Analytics.Application"));
        var referencedAssemblies = assembly.GetReferencedAssemblies()
            .Select(a => a.Name)
            .Where(n => n is not null)
            .ToList();

        referencedAssemblies.Should().NotContain(name => name!.StartsWith("SocialPublishing.Domain", StringComparison.Ordinal));
        referencedAssemblies.Should().NotContain(name => name!.StartsWith("SocialPublishing.Application", StringComparison.Ordinal));
        referencedAssemblies.Should().NotContain(name => name!.StartsWith("SocialPublishing.Infrastructure", StringComparison.Ordinal));
        referencedAssemblies.Should().Contain("SocialPublishing.Contracts");
    }

    /// <summary>
    /// Roadmap SocialPublishing Subfase 1.1: Contracts expõe apenas tipos de integração leves sobre BuildingBlocks.
    /// </summary>
    [Fact]
    public void SocialPublishingContracts_MustReferenceOnlyBuildingBlocks()
    {
        var assembly = SocialPublishingComplianceAssemblyLoader.Load("SocialPublishing.Contracts");
        var referencedAssemblies = assembly.GetReferencedAssemblies()
            .Select(a => a.Name)
            .Where(n => n is not null)
            .ToList();

        var projectReferences = SocialPublishingComplianceAssemblyLoader.GetProjectReferenceAssemblyNames(
            "SocialPublishing.Contracts");
        projectReferences.Should().BeEquivalentTo(["BuildingBlocks.Domain", "BuildingBlocks.Application"],
            "SocialPublishing.Contracts deve referenciar somente BuildingBlocks.Domain e BuildingBlocks.Application.");
    }

    /// <summary>
    /// AGENTS.md Seção 4: WebApp não pode referenciar SocialPublishing.Infrastructure.
    /// </summary>
    [Fact]
    public void WebApp_MustNotReferenceSocialPublishingInfrastructure()
    {
        foreach (var assemblyName in SocialPublishingComplianceAssemblyLoader.RequiredAssemblyNames)
        {
            SocialPublishingComplianceAssemblyLoader.Load(assemblyName);
        }

        var webAppCsprojPath = SocialPublishingComplianceAssemblyLoader.ResolveWebAppProjectPath();
        webAppCsprojPath.Should().NotBeNull("WebApp.csproj deve ser localizado a partir da raiz da solution.");

        var doc = XDocument.Load(webAppCsprojPath!);
        var projectReferences = doc.Descendants()
            .Where(e => e.Name.LocalName == "ProjectReference")
            .Select(e => e.Attribute("Include")?.Value ?? string.Empty)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .ToList();

        projectReferences.Should().NotContain(
            path => path.Contains("SocialPublishing.Infrastructure", StringComparison.OrdinalIgnoreCase),
            "WebApp não pode referenciar SocialPublishing.Infrastructure.");
    }

    /// <summary>
    /// Roadmap SocialPublishing Subfase 1.1: AddSocialPublishingModule registra MediatR via AddMessaging (ValidationBehavior).
    /// </summary>
    [Fact]
    public void AddSocialPublishingModule_RegistersMessagingPipeline()
    {
        var infrastructureAssembly = SocialPublishingComplianceAssemblyLoader.Load("SocialPublishing.Infrastructure");
        var extensionType = infrastructureAssembly.GetExportedTypes()
            .FirstOrDefault(t => t is { IsClass: true, IsSealed: true, IsAbstract: true }
                                 && t.Name == "SocialPublishingModuleServiceExtensions");
        extensionType.Should().NotBeNull("SocialPublishing.Infrastructure deve expor SocialPublishingModuleServiceExtensions.");

        var method = extensionType!.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(m => m.Name == "AddSocialPublishingModule");
        method.Should().NotBeNull("AddSocialPublishingModule deve existir.");

        var parameters = method!.GetParameters();
        parameters.Should().HaveCount(2);
        parameters[0].ParameterType.Should().Be(typeof(IServiceCollection));
        parameters[1].ParameterType.Should().Be(typeof(IConfiguration));
        method.ReturnType.Should().Be(typeof(IServiceCollection));

        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();
        method.Invoke(null, [services, configuration]);

        services.Should().Contain(sd => sd.ServiceType == typeof(IMediator));
        services.Should().Contain(sd =>
            sd.ServiceType.IsGenericType &&
            sd.ServiceType.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>) &&
            sd.ImplementationType != null &&
            sd.ImplementationType.IsGenericType &&
            sd.ImplementationType.GetGenericTypeDefinition() == typeof(ValidationBehavior<,>));
    }
}

/// <summary>
/// Carrega assemblies SocialPublishing por nome para testes de conformidade sem referência de projeto obrigatória na compilação inicial.
/// </summary>
internal static class SocialPublishingComplianceAssemblyLoader
{
    /// <summary>
    /// Nomes dos assemblies do bounded context SocialPublishing exigidos pelos testes de conformidade.
    /// </summary>
    public static readonly string[] RequiredAssemblyNames =
    [
        "SocialPublishing.Domain",
        "SocialPublishing.Contracts",
        "SocialPublishing.Application",
        "SocialPublishing.Infrastructure"
    ];

    /// <summary>
    /// Tenta carregar um assembly pelo nome simples; retorna null se ainda não foi compilado na solution.
    /// </summary>
    /// <param name="assemblyName">Nome simples do assembly (ex.: SocialPublishing.Domain).</param>
    /// <returns>Assembly carregado ou null.</returns>
    public static Assembly? TryLoad(string assemblyName)
    {
        try
        {
            return Assembly.Load(new AssemblyName(assemblyName));
        }
        catch (FileNotFoundException)
        {
            return null;
        }
        catch (FileLoadException)
        {
            return null;
        }
    }

    /// <summary>
    /// Carrega um assembly pelo nome simples ou falha o teste com mensagem explícita.
    /// </summary>
    /// <param name="assemblyName">Nome simples do assembly.</param>
    /// <returns>Assembly carregado.</returns>
    public static Assembly Load(string assemblyName)
    {
        var assembly = TryLoad(assemblyName);
        assembly.Should().NotBeNull($"O assembly {assemblyName} deve existir e ser carregável.");
        return assembly!;
    }

    /// <summary>
    /// Localiza WebApp.csproj subindo a partir do diretório base do teste até encontrar AdMetricsPro.sln.
    /// </summary>
    /// <returns>Caminho absoluto para WebApp.csproj ou null se a solution não for encontrada.</returns>
    public static string? ResolveWebAppProjectPath()
    {
        return ResolveProjectPath("src", "Frontend", "WebApp", "WebApp.csproj");
    }

    /// <summary>
    /// Lê os nomes de assembly inferidos das entradas ProjectReference de um projeto SocialPublishing.
    /// </summary>
    /// <param name="projectAssemblyName">Nome do projeto (ex.: SocialPublishing.Domain).</param>
    /// <returns>Nomes simples dos assemblies referenciados via ProjectReference.</returns>
    public static IReadOnlyList<string> GetProjectReferenceAssemblyNames(string projectAssemblyName)
    {
        var csprojPath = ResolveProjectPath(
            "src",
            "Backend",
            "Modules",
            "SocialPublishing",
            projectAssemblyName,
            $"{projectAssemblyName}.csproj");
        csprojPath.Should().NotBeNull($"{projectAssemblyName}.csproj deve existir na solution.");

        var doc = XDocument.Load(csprojPath!);
        return doc.Descendants()
            .Where(e => e.Name.LocalName == "ProjectReference")
            .Select(e => e.Attribute("Include")?.Value ?? string.Empty)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(path => Path.GetFileNameWithoutExtension(path.Replace('\\', '/')))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();
    }

    private static string? ResolveProjectPath(params string[] relativeSegments)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var slnPath = Path.Combine(directory.FullName, "AdMetricsPro.sln");
            if (File.Exists(slnPath))
            {
                var candidate = Path.Combine([directory.FullName, .. relativeSegments]);
                return File.Exists(candidate) ? candidate : null;
            }

            directory = directory.Parent;
        }

        return null;
    }
}
