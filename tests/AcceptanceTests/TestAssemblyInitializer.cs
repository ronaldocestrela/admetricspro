using System.Runtime.CompilerServices;

namespace AcceptanceTests;

/// <summary>
/// Inicializador do assembly de testes de aceitação para assegurar isolamento hermético e determinismo.
/// </summary>
internal static class TestAssemblyInitializer
{
    /// <summary>
    /// Configura variáveis de ambiente antes da inicialização de qualquer teste ou host web.
    /// </summary>
    [ModuleInitializer]
    internal static void Initialize()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable("DatabaseMigrations__ApplyMasterMigrationsOnStartup", "false");
    }
}
