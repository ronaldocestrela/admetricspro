using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Messaging.DependencyInjection;
using BuildingBlocks.Infrastructure.Configuration;
using BuildingBlocks.Infrastructure.DependencyInjection;
using BuildingBlocks.Infrastructure.Emails;
using BuildingBlocks.Infrastructure.Messaging;
using BuildingBlocks.Infrastructure.MultiTenancy;
using BuildingBlocks.Infrastructure.Persistence;
using BuildingBlocks.Infrastructure.Security;
using Master.Application.DependencyInjection;
using Master.Infrastructure.Extensions;
using Tenants.Application.DependencyInjection;
using Tenants.Infrastructure.DependencyInjection;
using Integrations.Application.DependencyInjection;
using Integrations.Infrastructure.DependencyInjection;
using Analytics.Application.DependencyInjection;
using Analytics.Infrastructure.DependencyInjection;
using Automations.Application.DependencyInjection;
using Automations.Infrastructure.DependencyInjection;
using SocialPublishing.Infrastructure.DependencyInjection;
using WebApi.Extensions;

// Carrega variáveis do arquivo .env no ambiente de processo e no pipeline de configuração
DotEnvLoader.Load();

var builder = WebApplication.CreateBuilder(args);
// O callback contém code/state na query; não registrar URLs de entrada em logs padrão.
builder.Logging.AddFilter("Microsoft.AspNetCore.Hosting.Diagnostics", LogLevel.Warning);
builder.Configuration.AddDotEnvFile();

// Configuração de serviços
builder.Services.Configure<RouteOptions>(options =>
{
    options.LowercaseUrls = true;
});
builder.Services.AddControllers(options => options.Filters.Add<WebApi.Security.IntegrationAccessFilter>());
builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
{
    var originalFactory = options.InvalidModelStateResponseFactory;
    options.InvalidModelStateResponseFactory = context => WebApi.Security.IntegrationAccessFilter.Protects(context.HttpContext.Request.Path)
        ? new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(BuildingBlocks.Domain.Primitives.Result.Failure(
            BuildingBlocks.Domain.Primitives.Error.Validation("Request.Invalid", "Os campos da requisição de integração são inválidos.")))
        : originalFactory(context);
});
builder.Services.AddOpenApiDocumentation();

var messagingHostRole = MessagingHostRoleExtensions.GetRequiredMessagingHostRole(builder.Configuration);
var messagingRegistrationMode = MessagingHostRoleExtensions.ResolveMessagingRegistrationMode(builder.Configuration);
var messagingEnabled = builder.Configuration.GetValue<bool>("Messaging:Enabled");

builder.Services.AddHealthChecks()
    .AddRabbitMqHealthCheck(builder.Configuration);

var masterConnectionString = builder.Configuration.GetConnectionString("MasterDb")
    ?? "Server=localhost;Database=MasterCatalog;Trusted_Connection=True;TrustServerCertificate=True;";

builder.Services.AddMasterCatalog(masterConnectionString);
builder.Services.AddMasterApplication();
builder.Services.AddSecurityServices();
builder.Services.AddMultiTenancy();
builder.Services.AddTenantPersistence<TenantDbContext>();
builder.Services.AddTenantMessagingStores();
builder.Services.AddSingleton<IMessagingFeatureGate, ConfigurationMessagingFeatureGate>();
builder.Services.Configure<WebApi.Configuration.MessagingOptions>(
    builder.Configuration.GetSection(WebApi.Configuration.MessagingOptions.SectionName));
builder.Services.Configure<WebApi.Configuration.CatalogEmailConsumerOptions>(
    builder.Configuration.GetSection(WebApi.Configuration.CatalogEmailConsumerOptions.SectionName));
builder.Services.Configure<WebApi.Configuration.MediaSyncConsumerOptions>(
    builder.Configuration.GetSection(WebApi.Configuration.MediaSyncConsumerOptions.SectionName));
builder.Services.Configure<WebApi.Configuration.IngestionReactionsConsumerOptions>(
    builder.Configuration.GetSection(WebApi.Configuration.IngestionReactionsConsumerOptions.SectionName));
builder.Services.Configure<WebApi.Configuration.OrganicPublishConsumerOptions>(
    builder.Configuration.GetSection(WebApi.Configuration.OrganicPublishConsumerOptions.SectionName));
builder.Services.AddScoped<WebApi.Background.IConnectedAdAccountMetaSyncExecutor, WebApi.Background.ConnectedAdAccountMetaSyncExecutor>();
if (MessagingHostRolePolicy.ShouldRegisterMassTransitConsumers(messagingHostRole))
{
    builder.Services.AddAdMetricsMessaging(
        builder.Configuration,
        messagingRegistrationMode,
        AdMetricsMessagingConsumerRegistration.AddWebApiIntegrationConsumers,
        AdMetricsMessagingConsumerRegistration.ConfigureWebApiReceiveEndpoints);
}
else
{
    builder.Services.AddAdMetricsMessaging(builder.Configuration, messagingRegistrationMode);
}

builder.Services.AddOptions<WebApi.Background.OutboxDispatchOptions>()
    .BindConfiguration(WebApi.Background.OutboxDispatchOptions.SectionName)
    .Validate(o => o.IntervalSeconds >= 5, "Intervalo de despacho de outbox deve ser >= 5 segundos.");
if (MessagingHostRolePolicy.ShouldRegisterSchedulerWorkers(messagingHostRole))
{
    builder.Services.AddMessagingSchedulerLeadership(builder.Configuration);
}

if (messagingEnabled && MessagingHostRolePolicy.ShouldRegisterSchedulerWorkers(messagingHostRole))
{
    builder.Services.AddHostedService<WebApi.Background.OutboxDispatchWorker>();
}

builder.Services.AddTenantsApplication();
builder.Services.AddTenantsInfrastructure(builder.Configuration);
builder.Services.AddIntegrationsApplication();
builder.Services.AddIntegrationsInfrastructure(builder.Configuration);
builder.Services.AddAnalyticsApplication();
builder.Services.AddAnalyticsInfrastructure(builder.Configuration);
builder.Services.AddAutomationsApplication();
builder.Services.AddAutomationsInfrastructure(builder.Configuration);
builder.Services.AddSocialPublishingModule(builder.Configuration);
builder.Services.AddSocialPublishingTenantPersistenceComposition(
    MessagingHostRolePolicy.ShouldRegisterSocialTenantMigrationWorker(messagingHostRole));
builder.Services.AddEmailInfrastructure(builder.Configuration);
builder.Services.AddOptions<WebApi.Background.MetaSynchronizationOptions>()
    .BindConfiguration("Integrations:MetaSynchronization")
    .Validate(o => o.IntervalMinutes >= 5 && o.LookbackDays is >= 1 and <= 90, "Intervalo Meta deve ser >= 5 minutos e lookback entre 1 e 90 dias.");
if (MessagingHostRolePolicy.ShouldRegisterSchedulerWorkers(messagingHostRole))
{
    builder.Services.AddHostedService<WebApi.Background.MetaSynchronizationWorker>();
}
builder.Services.AddOptions<SocialPublishing.Application.Options.SocialPublishingDispatchOptions>()
    .BindConfiguration(SocialPublishing.Application.Options.SocialPublishingDispatchOptions.SectionName)
    .Validate(
        o => o.IntervalSeconds >= 5 && o.InProgressTimeoutSeconds >= 60,
        "Intervalo de disparo orgânico deve ser >= 5 segundos e timeout InProgress >= 60 segundos.");
if (MessagingHostRolePolicy.ShouldRegisterSchedulerWorkers(messagingHostRole))
{
    builder.Services.AddHostedService<WebApi.Background.SocialPublishingDispatchWorker>();
}
builder.Services.Configure<Master.Infrastructure.Services.ImpersonationJwtOptions>(options =>
{
    builder.Configuration.GetSection(Master.Infrastructure.Services.ImpersonationJwtOptions.SectionName).Bind(options);
});
if (MessagingHostRolePolicy.ShouldRegisterSchedulerWorkers(messagingHostRole))
{
    builder.Services.AddDunningBackgroundService(options =>
    {
        builder.Configuration.GetSection(Master.Infrastructure.Services.DunningOptions.SectionName).Bind(options);
    });
    builder.Services.AddTrialNoticeBackgroundService(options =>
    {
        builder.Configuration.GetSection(Master.Infrastructure.Services.TrialNoticeOptions.SectionName).Bind(options);
    });
}

var app = builder.Build();

// Executa migrações automáticas do catálogo MasterDb no startup quando habilitado por configuração e fora de testes
if (!app.Environment.IsEnvironment("Testing") &&
    app.Configuration.GetValue<bool>("DatabaseMigrations:ApplyMasterMigrationsOnStartup", false) &&
    MessagingHostRolePolicy.ShouldRunMasterMigrationsOnStartup(messagingHostRole))
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

app.UseAuthentication();
app.UseTenantResolution();
app.UseRouting();
app.UseAuthorization();

app.MapHealthChecks("/health");
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
