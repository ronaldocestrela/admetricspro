using Master.Application.DependencyInjection;
using Master.Infrastructure.Extensions;
using WebApi.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Configuração de serviços
builder.Services.Configure<RouteOptions>(options =>
{
    options.LowercaseUrls = true;
});
builder.Services.AddControllers();
builder.Services.AddOpenApiDocumentation();

var masterConnectionString = builder.Configuration.GetConnectionString("MasterDb")
    ?? "Server=localhost;Database=MasterCatalog;Trusted_Connection=True;TrustServerCertificate=True;";

builder.Services.AddMasterCatalog(masterConnectionString);
builder.Services.AddMasterApplication();
builder.Services.AddDunningBackgroundService(options =>
{
    builder.Configuration.GetSection(Master.Infrastructure.Services.DunningOptions.SectionName).Bind(options);
});

var app = builder.Build();

// Executa migrações automáticas do catálogo MasterDb no startup quando habilitado por configuração
if (app.Configuration.GetValue<bool>("DatabaseMigrations:ApplyMasterMigrationsOnStartup", false))
{
    var migrationResult = await app.ApplyMasterDatabaseMigrationsAsync();
    if (migrationResult.IsFailure)
    {
        app.Logger.LogError("Falha ao aplicar migrações do MasterDb: {ErrorCode} - {ErrorMessage}",
            migrationResult.Error.Code,
            migrationResult.Error.Description);
        throw new InvalidOperationException($"Falha ao aplicar migrações do MasterDb: {migrationResult.Error.Description}");
    }
}

// Pipeline de middlewares HTTP
app.UseOpenApiAndScalar();

app.UseRouting();

app.MapControllers();

app.Run();

/// <summary>
/// Ponto de entrada parcial para permitir que suites de testes de aceitação façam referência a Program.
/// </summary>
public partial class Program
{
    /// <summary>
    /// Construtor protegido para conformidade com documentação XML.
    /// </summary>
    protected Program() { }
}
