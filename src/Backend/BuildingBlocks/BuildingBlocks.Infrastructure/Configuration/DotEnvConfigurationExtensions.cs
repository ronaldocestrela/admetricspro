using Microsoft.Extensions.Configuration;

namespace BuildingBlocks.Infrastructure.Configuration;

/// <summary>
/// Métodos de extensão para integração do carregamento de arquivos .env no pipeline de <see cref="IConfigurationBuilder"/>.
/// </summary>
public static class DotEnvConfigurationExtensions
{
    private const string DefaultFileName = ".env";

    /// <summary>
    /// Adiciona um arquivo .env como fonte de configuração em memória no <see cref="IConfigurationBuilder"/>,
    /// normalizando automaticamente chaves hierárquicas no formato .NET ('__' para ':').
    /// </summary>
    /// <param name="builder">O construtor de configuração.</param>
    /// <param name="filePath">Caminho específico para o arquivo .env. Se omitido, busca recursivamente a partir da raiz.</param>
    /// <param name="optional">Indica se a ausência do arquivo deve ser ignorada silenciosamente. Padrão: true.</param>
    /// <param name="overrideExisting">Indica se deve sobrescrever chaves já carregadas no builder. Padrão: false.</param>
    /// <returns>A mesma instância do construtor para chamadas encadeadas.</returns>
    /// <exception cref="FileNotFoundException">Lançada caso o arquivo não seja localizado e <paramref name="optional"/> seja false.</exception>
    public static IConfigurationBuilder AddDotEnvFile(
        this IConfigurationBuilder builder,
        string? filePath = null,
        bool optional = true,
        bool overrideExisting = false)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var resolvedPath = filePath;
        if (string.IsNullOrWhiteSpace(resolvedPath))
        {
            resolvedPath = DotEnvLoader.FindFile(DefaultFileName);
        }

        if (string.IsNullOrWhiteSpace(resolvedPath) || !File.Exists(resolvedPath))
        {
            if (!optional)
            {
                throw new FileNotFoundException($"Arquivo .env não localizado em '{filePath ?? DefaultFileName}'.", filePath ?? DefaultFileName);
            }

            return builder;
        }

        var entries = DotEnvLoader.ReadFile(resolvedPath);
        var configurationDictionary = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        foreach (var (key, value) in entries)
        {
            configurationDictionary[key] = value;

            // Suporta nativamente a convenção de dois pontos do .NET Configuration
            if (key.Contains("__"))
            {
                var normalizedKey = key.Replace("__", ConfigurationPath.KeyDelimiter);
                configurationDictionary[normalizedKey] = value;
            }
        }

        builder.AddInMemoryCollection(configurationDictionary);
        return builder;
    }
}
