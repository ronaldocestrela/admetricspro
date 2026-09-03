using System.Reflection;
using System.Xml.Linq;
using FluentAssertions;
using WebApp.Services;

namespace UnitTests.Frontend.Compliance;

/// <summary>
/// Suíte de testes de conformidade para documentação XML no frontend Blazor Server (WebApp).
/// Audita estritamente os tipos de serviços, modelos e provedores de estado (excluindo código gerado do Razor).
/// </summary>
public sealed class FrontendDocumentationComplianceTests
{
    /// <summary>
    /// Valida que todas as classes, interfaces e records de Services, Models e State do WebApp
    /// contêm tags XML &lt;summary&gt; ou &lt;inheritdoc/&gt; documentadas.
    /// </summary>
    [Fact]
    public void WebApp_PublicServicesModelsAndState_MustHaveXmlDocumentationSummary()
    {
        // Arrange
        var webAppAssembly = typeof(TenantDirectoryService).Assembly;
        var xmlDoc = LoadXmlDocumentation(webAppAssembly);
        xmlDoc.Should().NotBeNull("O arquivo XML de documentação do WebApp deve existir.");

        var documentedMembers = xmlDoc!.Descendants("member")
            .Where(m => !string.IsNullOrWhiteSpace(m.Element("summary")?.Value) || m.Element("inheritdoc") != null)
            .Select(m => m.Attribute("name")?.Value)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .ToHashSet();

        var relevantNamespaces = new[] { "WebApp.Services", "WebApp.Models", "WebApp.State" };

        var publicTypes = webAppAssembly.GetExportedTypes()
            .Where(t => !t.IsCompilerGenerated() && !t.Name.StartsWith('<'))
            .Where(t => relevantNamespaces.Any(ns => t.Namespace != null && t.Namespace.StartsWith(ns)))
            .ToList();

        var missingSummaries = new List<string>();

        // Act & Assert
        foreach (var type in publicTypes)
        {
            var typeDocId = $"T:{(type.FullName ?? type.Name).Replace('+', '.')}";
            if (!documentedMembers.Contains(typeDocId))
            {
                missingSummaries.Add($"[Tipo Sem <summary>]: {type.FullName}");
            }

            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(m => !m.IsSpecialName && !m.IsCompilerGenerated() && !m.Name.StartsWith('<'))
                .Where(m => m.Name != "PrintMembers" && m.Name != "Deconstruct")
                .Where(m => m.GetBaseDefinition().DeclaringType != typeof(object));

            foreach (var method in methods)
            {
                var methodDocId = $"M:{(type.FullName ?? type.Name).Replace('+', '.')}.{method.Name}";
                if (!documentedMembers.Any(d => d is not null && d.StartsWith(methodDocId)))
                {
                    missingSummaries.Add($"[Método Sem <summary>]: {type.FullName}.{method.Name}");
                }
            }

            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(p => !p.IsSpecialName && p.Name != "EqualityContract");

            foreach (var prop in properties)
            {
                var propDocId = $"P:{(type.FullName ?? type.Name).Replace('+', '.')}.{prop.Name}";
                if (!documentedMembers.Contains(propDocId))
                {
                    missingSummaries.Add($"[Propriedade Sem <summary>]: {type.FullName}.{prop.Name}");
                }
            }
        }

        missingSummaries.Should().BeEmpty(
            $"O WebApp contém {missingSummaries.Count} elementos públicos sem documentação XML:{Environment.NewLine}{string.Join(Environment.NewLine, missingSummaries.Take(25))}");
    }

    private static XDocument? LoadXmlDocumentation(Assembly assembly)
    {
        var codeBase = assembly.Location;
        var xmlPath = Path.ChangeExtension(codeBase, ".xml");

        if (File.Exists(xmlPath))
            return XDocument.Load(xmlPath);

        var altPath = Path.Combine(AppContext.BaseDirectory, $"{assembly.GetName().Name}.xml");
        if (File.Exists(altPath))
            return XDocument.Load(altPath);

        return null;
    }
}

/// <summary>
/// Métodos de extensão auxiliares para reflexão no frontend.
/// </summary>
internal static class FrontendTypeInspectionExtensions
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
