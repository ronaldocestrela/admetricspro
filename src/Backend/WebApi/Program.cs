using WebApi.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Configuração de serviços
builder.Services.Configure<RouteOptions>(options =>
{
    options.LowercaseUrls = true;
});
builder.Services.AddControllers();
builder.Services.AddOpenApiDocumentation();

var app = builder.Build();

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
