using Microsoft.Data.SqlClient;

namespace IntegrationTests.SocialPublishing;

/// <summary>
/// Utilitários para bancos efêmeros nos testes de integração do SocialPublishing.
/// </summary>
internal static class SocialPublishingTestDatabase
{
    /// <summary>
    /// Monta uma connection string apontando para o catálogo informado.
    /// </summary>
    /// <param name="rootConnectionString">Connection string base do container SQL.</param>
    /// <param name="databaseName">Nome do banco de dados.</param>
    /// <returns>Connection string com catálogo alterado.</returns>
    public static string WithDatabase(string rootConnectionString, string databaseName)
    {
        var builder = new SqlConnectionStringBuilder(rootConnectionString)
        {
            InitialCatalog = databaseName
        };

        return builder.ConnectionString;
    }

    /// <summary>
    /// Garante que o banco de dados existe no servidor SQL.
    /// </summary>
    /// <param name="connectionString">Connection string alvo.</param>
    /// <returns>Tarefa de conclusão.</returns>
    public static async Task EnsureDatabaseCreatedAsync(string connectionString)
    {
        await using var connection = new SqlConnection(new SqlConnectionStringBuilder(connectionString)
        {
            InitialCatalog = "master"
        }.ConnectionString);

        await connection.OpenAsync();
        var databaseName = new SqlConnectionStringBuilder(connectionString).InitialCatalog;
        await using var command = connection.CreateCommand();
        command.CommandText = $"IF DB_ID('{databaseName}') IS NULL CREATE DATABASE [{databaseName}]";
        await command.ExecuteNonQueryAsync();
    }
}
