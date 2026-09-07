using BuildingBlocks.Infrastructure.Configuration;
using WebApp.Components;
using WebApp.Extensions;
using WebApp.Services;
using WebApp.State;

// Carrega variáveis do arquivo .env no ambiente de processo e no pipeline de configuração
DotEnvLoader.Load();

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddDotEnvFile();

// Registros de componentes interativos do Blazor Server
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Provedores de estado de sessão do circuito Blazor
builder.Services.AddScoped<ITenantStateProvider, TenantStateProvider>();
builder.Services.AddScoped<ITenantSessionStateProvider, TenantSessionStateProvider>();
builder.Services.AddScoped<IImpersonationStateProvider, ImpersonationStateProvider>();

// Registro dos clientes HTTP fortemente tipados consumindo exclusivamente a Web API
var apiBaseUrl = builder.Configuration["Api:BaseUrl"] ?? "https://localhost:7001";
var apiUri = new Uri(apiBaseUrl);

builder.Services.AddHttpClient<ITenantAuthClientService, TenantAuthClientService>(client => client.BaseAddress = apiUri)
    .ConfigureDevelopmentCertificateBypass(builder.Environment.IsDevelopment());
builder.Services.AddHttpClient<ITenantDirectoryService, TenantDirectoryService>(client => client.BaseAddress = apiUri)
    .ConfigureDevelopmentCertificateBypass(builder.Environment.IsDevelopment());
builder.Services.AddHttpClient<ITenantOnboardingClientService, TenantOnboardingClientService>(client => client.BaseAddress = apiUri)
    .ConfigureDevelopmentCertificateBypass(builder.Environment.IsDevelopment());
builder.Services.AddHttpClient<IPlanManagementService, PlanManagementService>(client => client.BaseAddress = apiUri)
    .ConfigureDevelopmentCertificateBypass(builder.Environment.IsDevelopment());
builder.Services.AddHttpClient<IApiHealthClientService, ApiHealthClientService>(client => client.BaseAddress = apiUri)
    .ConfigureDevelopmentCertificateBypass(builder.Environment.IsDevelopment());
builder.Services.AddHttpClient<IFeatureFlagClientService, FeatureFlagClientService>(client => client.BaseAddress = apiUri)
    .ConfigureDevelopmentCertificateBypass(builder.Environment.IsDevelopment());
builder.Services.AddHttpClient<IImpersonationClientService, ImpersonationClientService>(client => client.BaseAddress = apiUri)
    .ConfigureDevelopmentCertificateBypass(builder.Environment.IsDevelopment());
builder.Services.AddHttpClient<ITenantFtuxClientService, TenantFtuxClientService>(client => client.BaseAddress = apiUri)
    .ConfigureDevelopmentCertificateBypass(builder.Environment.IsDevelopment());
builder.Services.AddHttpClient<IWorkspaceClientService, WorkspaceClientService>(client => client.BaseAddress = apiUri)
    .ConfigureDevelopmentCertificateBypass(builder.Environment.IsDevelopment());
builder.Services.AddHttpClient<ITenantTeamClientService, TenantTeamClientService>(client => client.BaseAddress = apiUri)
    .ConfigureDevelopmentCertificateBypass(builder.Environment.IsDevelopment());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
