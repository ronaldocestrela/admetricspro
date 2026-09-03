using BuildingBlocks.Infrastructure.Configuration;
using Master.Application.DependencyInjection;
using Master.Infrastructure.Extensions;
using WebApp.Components;
using WebApp.Services;
using WebApp.State;

// Carrega variáveis do arquivo .env no ambiente de processo e no pipeline de configuração
DotEnvLoader.Load();

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddDotEnvFile();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var masterConnectionString = builder.Configuration.GetConnectionString("MasterDb")
    ?? "Server=localhost;Database=MasterCatalog;Trusted_Connection=True;TrustServerCertificate=True;";

builder.Services.AddMasterCatalog(masterConnectionString);
builder.Services.AddMasterApplication();

builder.Services.AddScoped<ITenantStateProvider, TenantStateProvider>();
builder.Services.AddScoped<ITenantDirectoryService, TenantDirectoryService>();
builder.Services.AddScoped<IPlanManagementService, PlanManagementService>();
builder.Services.AddScoped<IApiHealthClientService, ApiHealthClientService>();
builder.Services.AddScoped<IFeatureFlagClientService, FeatureFlagClientService>();
builder.Services.AddScoped<IImpersonationStateProvider, ImpersonationStateProvider>();
builder.Services.AddHttpClient<IImpersonationClientService, ImpersonationClientService>(client =>
{
    var baseUri = builder.Configuration["Api:BaseUrl"] ?? "https://localhost:7001";
    client.BaseAddress = new Uri(baseUri);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
