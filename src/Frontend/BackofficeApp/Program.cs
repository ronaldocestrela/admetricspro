using System.Security.Claims;
using BuildingBlocks.Infrastructure.Configuration;
using BuildingBlocks.Infrastructure.Security;
using Master.Application.DependencyInjection;
using Master.Application.Users.Services;
using Master.Infrastructure.Extensions;
using Master.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using BackofficeApp.Services;
using BackofficeApp.State;
using BackofficeApp.Components;

// Carrega variáveis do arquivo .env no ambiente de processo e no pipeline de configuração
DotEnvLoader.Load();

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddDotEnvFile();

// Registros do Blazor Server
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();

// Configuração de Autenticação baseada em Cookie seguro para o Backoffice
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "AdMetricsPro_Backoffice_Session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/access-denied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireSuperAdmin", policy =>
        policy.RequireRole(MasterRole.SuperAdmin));

    options.AddPolicy("RequireBackofficeAccess", policy =>
        policy.RequireRole(MasterRole.SuperAdmin, MasterRole.SupportTechnician));
});

// Resolução de persistência no MasterDb
var masterConnectionString = builder.Configuration.GetConnectionString("MasterDb")
    ?? "Server=localhost;Database=MasterCatalog;Trusted_Connection=True;TrustServerCertificate=True;";

builder.Services.AddMasterCatalog(masterConnectionString);
builder.Services.AddMasterApplication();
builder.Services.AddSecurityServices();

// Registros dos serviços administrativos de consumo do frontend
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

// Aplicação de migrações e seed inicial do SuperAdmin quando habilitado
if (app.Configuration.GetValue<bool>("DatabaseMigrations:ApplyMasterMigrationsOnStartup", false))
{
    var migrationResult = await app.ApplyMasterDatabaseMigrationsAsync();
    if (migrationResult.IsFailure)
    {
        app.Logger.LogError("Falha ao aplicar migrações e seed do MasterDb no Backoffice: {Error}", migrationResult.Error.Description);
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();

// Endpoints de Autenticação HTTP para emissão e encerramento de cookies
app.MapPost("/api/auth/login", async (
    HttpContext httpContext,
    IBackofficeAuthService authService) =>
{
    string email = string.Empty;
    string password = string.Empty;
    bool rememberMe = false;
    string returnUrl = "/";

    if (httpContext.Request.HasFormContentType)
    {
        var form = await httpContext.Request.ReadFormAsync();
        email = form["email"].ToString();
        password = form["password"].ToString();
        bool.TryParse(form["rememberMe"], out rememberMe);
        returnUrl = form["returnUrl"].ToString();
        if (string.IsNullOrWhiteSpace(returnUrl)) returnUrl = "/";
    }
    else
    {
        var req = await httpContext.Request.ReadFromJsonAsync<BackofficeLoginRequest>();
        if (req != null)
        {
            email = req.Email;
            password = req.Password;
            rememberMe = req.RememberMe;
        }
    }

    var result = await authService.AuthenticateAsync(
        email,
        password,
        httpContext.Connection.RemoteIpAddress?.ToString());

    if (result.IsFailure)
    {
        if (httpContext.Request.HasFormContentType)
        {
            var encodedError = Uri.EscapeDataString(result.Error.Description);
            var encodedReturn = Uri.EscapeDataString(returnUrl);
            return Results.Redirect($"/login?error={encodedError}&returnUrl={encodedReturn}");
        }
        return Results.BadRequest(new { error = result.Error.Description, code = result.Error.Code });
    }

    var user = result.Value;
    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new(ClaimTypes.Name, user.FullName),
        new(ClaimTypes.Email, user.Email)
    };

    foreach (var role in user.Roles)
    {
        claims.Add(new Claim(ClaimTypes.Role, role));
    }

    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    var principal = new ClaimsPrincipal(identity);

    await httpContext.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        principal,
        new AuthenticationProperties
        {
            IsPersistent = rememberMe,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
        });

    if (httpContext.Request.HasFormContentType)
    {
        return Results.Redirect(string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl);
    }

    return Results.Ok(new { success = true, user, redirectUrl = returnUrl });
}).AllowAnonymous().DisableAntiforgery();

app.MapGet("/api/auth/logout", async (HttpContext httpContext) =>
{
    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
});

app.MapPost("/api/auth/logout", async (HttpContext httpContext) =>
{
    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

/// <summary>
/// Modelo de entrada para requisições HTTP de login no Backoffice.
/// </summary>
/// <param name="Email">E-mail corporativo cadastrado.</param>
/// <param name="Password">Senha do operador.</param>
/// <param name="RememberMe">Indica se a sessão deve persistir além da janela do navegador.</param>
public sealed record BackofficeLoginRequest(string Email, string Password, bool RememberMe = false);

/// <summary>
/// Ponto de entrada parcial para testes de aceitação e integração.
/// </summary>
public partial class Program
{
    /// <summary>
    /// Construtor protegido para conformidade com documentação XML.
    /// </summary>
    protected Program() { }
}
