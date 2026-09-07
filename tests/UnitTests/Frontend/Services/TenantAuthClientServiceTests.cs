using System.Net;
using System.Text.Json;
using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using Tenants.Application.Auth.DTOs;
using WebApp.Models;
using WebApp.Services;
using Xunit;

namespace UnitTests.Frontend.Services;

/// <summary>
/// Testes unitários para o cliente HTTP tipado de autenticação de inquilinos (<see cref="TenantAuthClientService"/>).
/// Simula respostas da Web API em cenários de sucesso, erro de validação e credenciais inválidas.
/// </summary>
public sealed class TenantAuthClientServiceTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Valida que a consulta de branding retorna o ViewModel populado com sucesso quando a API responde 200 OK.
    /// </summary>
    [Fact]
    public async Task GetPublicBrandingAsync_WhenApiReturnsSuccess_ShouldReturnViewModel()
    {
        // Arrange
        var brandingDto = new TenantPublicBrandingDto(
            TenantId: Guid.NewGuid(),
            CompanyName: "Agência Vanguarda",
            Subdomain: "vanguarda",
            CustomDomain: null,
            PrimaryColor: "#1E40AF",
            SecondaryColor: "#F59E0B",
            LogoUrl: "https://cdn.internal/logo.png",
            IsActive: true);

        var apiEnvelope = Result<TenantPublicBrandingDto>.Success(brandingDto);

        var handler = new MockHttpMessageHandler((request, cancellationToken) =>
        {
            request.RequestUri!.PathAndQuery.Should().Contain("/api/v1/tenants/auth/branding?subdomain=vanguarda");
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(apiEnvelope, JsonOptions), System.Text.Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost:7001") };
        var sut = new TenantAuthClientService(httpClient);

        // Act
        var result = await sut.GetPublicBrandingAsync("vanguarda");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.CompanyName.Should().Be("Agência Vanguarda");
        result.Value.Subdomain.Should().Be("vanguarda");
        result.Value.PrimaryColor.Should().Be("#1E40AF");
    }

    /// <summary>
    /// Valida que o login bem-sucedido retorna o AuthenticatedTenantUserDto com o token JWT.
    /// </summary>
    [Fact]
    public async Task LoginAsync_WhenCredentialsAreValid_ShouldReturnAuthenticatedUserDto()
    {
        // Arrange
        var userDto = new AuthenticatedTenantUserDto(
            AccessToken: "jwt_token_valid",
            TokenType: "Bearer",
            ExpiresIn: 28800,
            UserId: Guid.NewGuid(),
            Email: "gestor@vanguarda.com.br",
            FullName: "Gestor Carlos",
            Role: "Owner",
            TenantId: Guid.NewGuid(),
            Subdomain: "vanguarda",
            Branding: new TenantBrandingDto("Agência Vanguarda", "#1E40AF", "#F59E0B", null, null, null));

        var apiEnvelope = Result<AuthenticatedTenantUserDto>.Success(userDto);

        var handler = new MockHttpMessageHandler((request, cancellationToken) =>
        {
            request.RequestUri!.PathAndQuery.Should().Be("/api/v1/tenants/auth/login");
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(apiEnvelope, JsonOptions), System.Text.Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost:7001") };
        var sut = new TenantAuthClientService(httpClient);

        var model = new TenantLoginModel
        {
            Email = "gestor@vanguarda.com.br",
            Password = "SenhaCorreta123!",
            Subdomain = "vanguarda"
        };

        // Act
        var result = await sut.LoginAsync(model);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("jwt_token_valid");
        result.Value.Email.Should().Be("gestor@vanguarda.com.br");
        result.Value.Role.Should().Be("Owner");
    }

    /// <summary>
    /// Valida que erro 401 retornado pela API é desserializado com o código de erro Auth.InvalidCredentials.
    /// </summary>
    [Fact]
    public async Task LoginAsync_WhenCredentialsAreInvalid_ShouldReturnFailureResult()
    {
        // Arrange
        var apiEnvelope = Result<AuthenticatedTenantUserDto>.Failure(
            Error.Unauthorized("Auth.InvalidCredentials", "E-mail ou senha incorretos."));

        var handler = new MockHttpMessageHandler((request, cancellationToken) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent(JsonSerializer.Serialize(apiEnvelope, JsonOptions), System.Text.Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost:7001") };
        var sut = new TenantAuthClientService(httpClient);

        var model = new TenantLoginModel
        {
            Email = "gestor@vanguarda.com.br",
            Password = "SenhaErrada",
            Subdomain = "vanguarda"
        };

        // Act
        var result = await sut.LoginAsync(model);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.InvalidCredentials");
        result.Error.Description.Should().Be("E-mail ou senha incorretos.");
    }

    private sealed class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

        public MockHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return _handler(request, cancellationToken);
        }
    }
}
