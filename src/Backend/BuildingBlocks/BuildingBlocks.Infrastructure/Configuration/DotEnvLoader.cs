namespace BuildingBlocks.Infrastructure.Configuration;

/// <summary>
/// Utilitário de alta performance e desacoplado para localização, parsing e carregamento de arquivos .env
/// em variáveis de processo do sistema operacional e coleções de configuração.
/// </summary>
public static class DotEnvLoader
{
    private const string DefaultFileName = ".env";

    /// <summary>
    /// Localiza e carrega variáveis de um arquivo .env no ambiente do processo (<see cref="Environment.SetEnvironmentVariable(string, string?)"/>).
    /// </summary>
    /// <param name="filePath">Caminho específico do arquivo .env. Se nulo, busca recursivamente a partir do diretório de execução.</param>
    /// <param name="overrideExisting">Indica se variáveis já existentes no processo devem ser sobrescritas. Padrão: false.</param>
    /// <param name="optional">Indica se a inexistência do arquivo deve ser ignorada silenciosamente. Padrão: true.</param>
    /// <returns>O número de variáveis carregadas com sucesso.</returns>
    /// <exception cref="FileNotFoundException">Lançada caso o arquivo não seja localizado e <paramref name="optional"/> seja false.</exception>
    public static int Load(string? filePath = null, bool overrideExisting = false, bool optional = true)
    {
        var resolvedPath = filePath;
        if (string.IsNullOrWhiteSpace(resolvedPath))
        {
            resolvedPath = FindFile(DefaultFileName);
        }

        if (string.IsNullOrWhiteSpace(resolvedPath) || !File.Exists(resolvedPath))
        {
            if (!optional)
            {
                throw new FileNotFoundException($"Arquivo .env não localizado em '{filePath ?? DefaultFileName}'.", filePath ?? DefaultFileName);
            }

            return 0;
        }

        var entries = ReadFile(resolvedPath);
        var loadedCount = 0;

        foreach (var (key, value) in entries)
        {
            var existingValue = Environment.GetEnvironmentVariable(key);
            if (!overrideExisting && !string.IsNullOrEmpty(existingValue))
            {
                continue;
            }

            Environment.SetEnvironmentVariable(key, value);
            loadedCount++;
        }

        return loadedCount;
    }

    /// <summary>
    /// Busca recursivamente por um arquivo subindo a árvore de diretórios até atingir a raiz do sistema.
    /// </summary>
    /// <param name="fileName">Nome do arquivo a ser localizado (ex.: ".env").</param>
    /// <param name="startDirectory">Diretório inicial de busca. Se nulo, utiliza <see cref="Directory.GetCurrentDirectory()"/>.</param>
    /// <returns>O caminho absoluto para o arquivo caso localizado; caso contrário, nulo.</returns>
    public static string? FindFile(string fileName = DefaultFileName, string? startDirectory = null)
    {
        var currentDir = new DirectoryInfo(startDirectory ?? Directory.GetCurrentDirectory());

        while (currentDir != null && currentDir.Exists)
        {
            var targetPath = Path.Combine(currentDir.FullName, fileName);
            if (File.Exists(targetPath))
            {
                return targetPath;
            }

            currentDir = currentDir.Parent;
        }

        return null;
    }

    /// <summary>
    /// Lê e efetua o parsing das linhas de um arquivo no formato chave=valor.
    /// </summary>
    /// <param name="filePath">Caminho absoluto ou relativo do arquivo a ser lido.</param>
    /// <returns>Dicionário com as chaves e valores extraídos.</returns>
    public static IDictionary<string, string> ReadFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        var content = File.ReadAllText(filePath);
        return Parse(content);
    }

    /// <summary>
    /// Efetua o parsing de uma string com declarações de variáveis no padrão .env.
    /// Suporta comentários iniciados por '#', valores envolvidos em aspas simples ou duplas e hierarquias .NET.
    /// </summary>
    /// <param name="content">Conteúdo em formato texto bruto.</param>
    /// <returns>Dicionário com pares chave/valor válidos.</returns>
    public static IDictionary<string, string> Parse(string content)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(content))
        {
            return result;
        }

        using var reader = new StringReader(content);
        string? line;

        while ((line = reader.ReadLine()) != null)
        {
            var trimmed = line.Trim();

            // Ignora linhas vazias ou comentários completos
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#'))
            {
                continue;
            }

            var separatorIndex = trimmed.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var rawKey = trimmed[..separatorIndex].Trim();
            var rawValue = trimmed[(separatorIndex + 1)..].Trim();

            if (string.IsNullOrEmpty(rawKey))
            {
                continue;
            }

            var cleanedValue = StripQuotes(rawValue);
            result[rawKey] = cleanedValue;
        }

        return result;
    }

    private static string StripQuotes(string value)
    {
        if (value.Length >= 2)
        {
            if ((value.StartsWith('"') && value.EndsWith('"')) ||
                (value.StartsWith('\'') && value.EndsWith('\'')))
            {
                return value[1..^1];
            }
        }

        return value;
    }
}
