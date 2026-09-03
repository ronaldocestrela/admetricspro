using BuildingBlocks.Infrastructure.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace UnitTests.Backend.Configuration;

/// <summary>
/// Testes unitários para o carregador de arquivos .env (<see cref="DotEnvLoader"/>) e suas extensões de configuração.
/// </summary>
public sealed class DotEnvLoaderTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly List<string> _environmentVariablesToClean = [];

    /// <summary>
    /// Inicializa uma nova suite de testes criando um diretório temporário isolado.
    /// </summary>
    public DotEnvLoaderTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "DotEnvLoaderTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    /// <summary>
    /// Limpa o diretório temporário e restaura variáveis de ambiente alteradas.
    /// </summary>
    public void Dispose()
    {
        foreach (var variable in _environmentVariablesToClean)
        {
            Environment.SetEnvironmentVariable(variable, null);
        }

        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    private void TrackEnvVar(string name)
    {
        _environmentVariablesToClean.Add(name);
    }

    /// <summary>
    /// Garante que o método Load popule variáveis no processo para pares chave=valor válidos.
    /// </summary>
    [Fact]
    public void Load_ShouldPopulateEnvironmentVariables_WhenValidKeyValuePairsProvided()
    {
        // Arrange
        var envFilePath = Path.Combine(_tempDirectory, ".env");
        var key1 = "DOTENV_TEST_KEY1_" + Guid.NewGuid().ToString("N");
        var key2 = "Section__SubKey_" + Guid.NewGuid().ToString("N");
        TrackEnvVar(key1);
        TrackEnvVar(key2);

        var content = $"""
            {key1}=SuperSecretValue123
            {key2}=NestedConfigurationValue
            """;
        File.WriteAllText(envFilePath, content);

        // Act
        var loadedCount = DotEnvLoader.Load(envFilePath, overrideExisting: true);

        // Assert
        loadedCount.Should().Be(2);
        Environment.GetEnvironmentVariable(key1).Should().Be("SuperSecretValue123");
        Environment.GetEnvironmentVariable(key2).Should().Be("NestedConfigurationValue");
    }

    /// <summary>
    /// Garante que linhas em branco e comentários iniciados por '#' sejam ignorados no carregamento.
    /// </summary>
    [Fact]
    public void Load_ShouldIgnoreCommentsAndEmptyLines()
    {
        // Arrange
        var envFilePath = Path.Combine(_tempDirectory, ".env");
        var validKey = "DOTENV_TEST_COMMENT_" + Guid.NewGuid().ToString("N");
        TrackEnvVar(validKey);

        var content = $"""
            # Comentário no início
               # Comentário com espaços

            {validKey}=ActiveValue # Este comentário inline ou final deve ser tratado com cuidado se implementado, mas na linha inteira deve ignorar
            
            """;
        File.WriteAllText(envFilePath, content);

        // Act
        var loadedCount = DotEnvLoader.Load(envFilePath, overrideExisting: true);

        // Assert
        loadedCount.Should().Be(1);
        Environment.GetEnvironmentVariable(validKey).Should().Be("ActiveValue # Este comentário inline ou final deve ser tratado com cuidado se implementado, mas na linha inteira deve ignorar");
    }

    /// <summary>
    /// Garante que valores envolvidos em aspas simples ou duplas tenham as aspas removidas corretamente.
    /// </summary>
    [Fact]
    public void Load_ShouldStripQuotes_WhenValueEnclosedInSingleOrDoubleQuotes()
    {
        // Arrange
        var envFilePath = Path.Combine(_tempDirectory, ".env");
        var keyDouble = "DOTENV_TEST_DOUBLE_QUOTE_" + Guid.NewGuid().ToString("N");
        var keySingle = "DOTENV_TEST_SINGLE_QUOTE_" + Guid.NewGuid().ToString("N");
        TrackEnvVar(keyDouble);
        TrackEnvVar(keySingle);

        var content = $"""
            {keyDouble}="Server=localhost,1433;Database=MasterCatalog;User Id=sa;Password=Secret!;"
            {keySingle}='Valor com espaços e caracteres especiais @#$'
            """;
        File.WriteAllText(envFilePath, content);

        // Act
        var loadedCount = DotEnvLoader.Load(envFilePath, overrideExisting: true);

        // Assert
        loadedCount.Should().Be(2);
        Environment.GetEnvironmentVariable(keyDouble).Should().Be("Server=localhost,1433;Database=MasterCatalog;User Id=sa;Password=Secret!;");
        Environment.GetEnvironmentVariable(keySingle).Should().Be("Valor com espaços e caracteres especiais @#$");
    }

    /// <summary>
    /// Garante que variáveis já existentes no processo não sejam sobrescritas quando overrideExisting for falso.
    /// </summary>
    [Fact]
    public void Load_ShouldNotOverrideExistingEnvironmentVariable_WhenOverrideExistingIsFalse()
    {
        // Arrange
        var envFilePath = Path.Combine(_tempDirectory, ".env");
        var key = "DOTENV_TEST_NO_OVERRIDE_" + Guid.NewGuid().ToString("N");
        TrackEnvVar(key);

        Environment.SetEnvironmentVariable(key, "OriginalProcessValue");

        var content = $"{key}=NewValueFromFile";
        File.WriteAllText(envFilePath, content);

        // Act
        var loadedCount = DotEnvLoader.Load(envFilePath, overrideExisting: false);

        // Assert
        loadedCount.Should().Be(0);
        Environment.GetEnvironmentVariable(key).Should().Be("OriginalProcessValue");
    }

    /// <summary>
    /// Garante que variáveis já existentes sejam atualizadas quando overrideExisting for verdadeiro.
    /// </summary>
    [Fact]
    public void Load_ShouldOverrideExistingEnvironmentVariable_WhenOverrideExistingIsTrue()
    {
        // Arrange
        var envFilePath = Path.Combine(_tempDirectory, ".env");
        var key = "DOTENV_TEST_WITH_OVERRIDE_" + Guid.NewGuid().ToString("N");
        TrackEnvVar(key);

        Environment.SetEnvironmentVariable(key, "OriginalProcessValue");

        var content = $"{key}=UpdatedValueFromFile";
        File.WriteAllText(envFilePath, content);

        // Act
        var loadedCount = DotEnvLoader.Load(envFilePath, overrideExisting: true);

        // Assert
        loadedCount.Should().Be(1);
        Environment.GetEnvironmentVariable(key).Should().Be("UpdatedValueFromFile");
    }

    /// <summary>
    /// Garante que a ausência do arquivo retorne zero e não dispare exceção quando optional for verdadeiro.
    /// </summary>
    [Fact]
    public void Load_ShouldReturnZeroAndNotThrow_WhenFileDoesNotExistAndOptionalIsTrue()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_tempDirectory, "non_existent.env");

        // Act
        var loadedCount = DotEnvLoader.Load(nonExistentPath, optional: true);

        // Assert
        loadedCount.Should().Be(0);
    }

    /// <summary>
    /// Garante que seja lançada FileNotFoundException quando o arquivo não existir e optional for falso.
    /// </summary>
    [Fact]
    public void Load_ShouldThrowFileNotFoundException_WhenFileDoesNotExistAndOptionalIsFalse()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_tempDirectory, "missing.env");

        // Act & Assert
        var action = () => DotEnvLoader.Load(nonExistentPath, optional: false);
        action.Should().Throw<FileNotFoundException>();
    }

    /// <summary>
    /// Garante que o método FindFile navegue recursivamente subindo diretórios para localizar o arquivo .env.
    /// </summary>
    [Fact]
    public void FindFile_ShouldTraverseUpwards_ToFindEnvInParentDirectory()
    {
        // Arrange: cria uma árvore /root/.env e /root/sub/nested/
        var parentEnv = Path.Combine(_tempDirectory, ".env");
        File.WriteAllText(parentEnv, "FOUND_IN_PARENT=true");

        var nestedDir = Path.Combine(_tempDirectory, "sub", "nested");
        Directory.CreateDirectory(nestedDir);

        // Act
        var discoveredPath = DotEnvLoader.FindFile(".env", startDirectory: nestedDir);

        // Assert
        discoveredPath.Should().NotBeNull();
        discoveredPath.Should().Be(parentEnv);
    }

    /// <summary>
    /// Garante que a extensão AddDotEnvFile popule corretamente as chaves no IConfiguration com suporte a hierarquia .NET.
    /// </summary>
    [Fact]
    public void AddDotEnvFile_ShouldPopulateIConfiguration_Correctly()
    {
        // Arrange
        var envFilePath = Path.Combine(_tempDirectory, ".env");
        var key = "ConnectionStrings__MasterDb";
        var expectedConnection = "Server=dbhost;Database=MasterCatalog;User Id=sa;Password=Secret!;";

        File.WriteAllText(envFilePath, $"{key}=\"{expectedConnection}\"");

        // Act
        var config = new ConfigurationBuilder()
            .AddDotEnvFile(envFilePath, optional: false)
            .Build();

        // Assert
        config.GetConnectionString("MasterDb").Should().Be(expectedConnection);
    }
}
