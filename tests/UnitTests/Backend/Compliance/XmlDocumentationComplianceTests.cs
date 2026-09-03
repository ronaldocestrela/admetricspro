using System.Reflection;
using System.Xml.Linq;
using FluentAssertions;

namespace UnitTests.Backend.Compliance;

/// <summary>
/// Suíte de testes de conformidade para a Subfase 6.2.1 do AGENTS.md.
/// Executa auditoria estrita da presença de documentação XML (&lt;summary&gt; ou &lt;inheritdoc/&gt;)
/// em todas as classes, interfaces, records, structs, métodos e propriedades públicas da solução.
/// </summary>
public sealed class XmlDocumentationComplianceTests
{
    private static readonly Assembly[] BackendAssemblies =
    [
        typeof(BuildingBlocks.Domain.Primitives.Result).Assembly,
        typeof(BuildingBlocks.Application.Behaviors.ValidationBehavior<,>).Assembly,
        typeof(BuildingBlocks.Infrastructure.Persistence.TenantDbContext).Assembly,
        typeof(Master.Domain.Tenants.Tenant).Assembly,
        typeof(Master.Application.Tenants.Commands.CreateTenant.CreateTenantCommand).Assembly,
        typeof(Master.Infrastructure.Persistence.MasterDbContext).Assembly,
        typeof(Program).Assembly
    ];

    /// <summary>
    /// Valida que todo tipo público e seus membros públicos declarados nos assemblies do backend
    /// possuem documentação XML válida (&lt;summary&gt; ou &lt;inheritdoc/&gt;) no arquivo de documentação gerado.
    /// </summary>
    [Theory]
    [InlineData("BuildingBlocks.Domain")]
    [InlineData("BuildingBlocks.Application")]
    [InlineData("BuildingBlocks.Infrastructure")]
    [InlineData("Master.Domain")]
    [InlineData("Master.Application")]
    [InlineData("Master.Infrastructure")]
    [InlineData("WebApi")]
    public void Assembly_PublicTypesAndMembers_MustHaveXmlDocumentationSummary(string assemblyName)
    {
        // Arrange
        var assembly = BackendAssemblies.FirstOrDefault(a => a.GetName().Name == assemblyName);
        assembly.Should().NotBeNull($"O assembly {assemblyName} deve estar presente na lista de assemblies a auditar.");

        var xmlDoc = LoadXmlDocumentation(assembly!);
        xmlDoc.Should().NotBeNull($"O arquivo de documentação XML do assembly {assemblyName} deve existir e ser legível.");

        var documentedMembers = xmlDoc!.Descendants("member")
            .Where(m => (!string.IsNullOrWhiteSpace(m.Element("summary")?.Value)) || m.Element("inheritdoc") != null)
            .Select(m => m.Attribute("name")?.Value)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .ToHashSet();

        var publicTypes = assembly!.GetExportedTypes()
            .Where(t => !t.IsCompilerGenerated() && !t.Name.StartsWith('<'))
            .ToList();

        var missingSummaries = new List<string>();

        // Act & Assert
        foreach (var type in publicTypes)
        {
            var typeDocId = $"T:{GetXmlMemberName(type)}";
            if (!documentedMembers.Contains(typeDocId))
            {
                missingSummaries.Add($"[Tipo Sem <summary>]: {type.FullName}");
            }

            // Inspecionar métodos públicos declarados
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(m => !m.IsSpecialName && !m.IsCompilerGenerated() && !m.Name.StartsWith('<'))
                .Where(m => m.Name != "PrintMembers" && m.Name != "Deconstruct")
                .Where(m => m.GetBaseDefinition().DeclaringType != typeof(object));

            foreach (var method in methods)
            {
                var methodDocId = $"M:{GetXmlMemberName(type)}.{method.Name}";
                // Prefix match para lidar com sobrecargas de métodos e assinaturas de parâmetros
                if (!documentedMembers.Any(d => d is not null && d.StartsWith(methodDocId)))
                {
                    missingSummaries.Add($"[Método Sem <summary>]: {type.FullName}.{method.Name}");
                }
            }

            // Inspecionar propriedades públicas declaradas
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(p => !p.IsSpecialName && p.Name != "EqualityContract");

            foreach (var prop in properties)
            {
                var propDocId = $"P:{GetXmlMemberName(type)}.{prop.Name}";
                if (!documentedMembers.Contains(propDocId))
                {
                    missingSummaries.Add($"[Propriedade Sem <summary>]: {type.FullName}.{prop.Name}");
                }
            }
        }

        missingSummaries.Should().BeEmpty(
            $"O assembly {assemblyName} contém {missingSummaries.Count} elementos públicos sem a documentação XML exigida pelo AGENTS.md:{Environment.NewLine}{string.Join(Environment.NewLine, missingSummaries.Take(25))}");
    }

    private static XDocument? LoadXmlDocumentation(Assembly assembly)
    {
        var codeBase = assembly.Location;
        var xmlPath = Path.ChangeExtension(codeBase, ".xml");

        if (File.Exists(xmlPath))
            return XDocument.Load(xmlPath);

        // Tentar no AppContext.BaseDirectory
        var altPath = Path.Combine(AppContext.BaseDirectory, $"{assembly.GetName().Name}.xml");
        if (File.Exists(altPath))
            return XDocument.Load(altPath);

        return null;
    }

    private static string GetXmlMemberName(Type type)
    {
        var name = type.FullName ?? type.Name;
        return name.Replace('+', '.');
    }
}

/// <summary>
/// Métodos de extensão auxiliares para inspeção de atributos e tipos de compilador.
/// </summary>
internal static class TypeInspectionExtensions
{
    public static bool IsCompilerGenerated(this Type type)
    {
        return type.GetCustomAttributes(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), false).Length > 0;
    }

    public static bool IsCompilerGenerated(this MethodInfo method)
    {
        return method.GetCustomAttributes(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), false).Length > 0;
    }
}
