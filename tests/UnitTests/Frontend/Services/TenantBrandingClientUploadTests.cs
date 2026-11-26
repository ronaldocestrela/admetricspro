using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Tenants.Application.Branding.DTOs;
using WebApp.Services;
using WebApp.State;

namespace UnitTests.Frontend.Services;

/// <summary>
/// Testes do cliente HTTP de upload de branding.
/// </summary>
public sealed class TenantBrandingClientUploadTests
{
    /// <summary>
    /// Upload multipart aponta para a rota versionada do slot.
    /// </summary>
    [Fact]
    public async Task UploadBrandAssetAsync_ShouldPostToBrandingAssetRoute()
    {
        var handler = new RecordingHandler();
        var services = new ServiceCollection();
        var tenantState = Substitute.For<ITenantStateProvider>();
        tenantState.CurrentTenant.Returns(new WebApp.State.TenantState(Guid.NewGuid(), "Tenant", "tenant", null, TenantBranding.Default));
        services.AddSingleton(tenantState);

        var client = new TenantBrandingClientService(
            new HttpClient(handler) { BaseAddress = new Uri("https://localhost:7001") },
            tenantState);

        await using var stream = new MemoryStream([1, 2, 3]);
        var result = await client.UploadBrandAssetAsync("light", "logo.png", "image/png", stream);

        handler.LastRequest.Should().NotBeNull();
        handler.LastRequest!.Method.Should().Be(HttpMethod.Post);
        handler.LastRequest.RequestUri!.AbsolutePath.Should().Be("/api/v1/tenants/branding/assets/light");
        result.IsFailure.Should().BeTrue();
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            var payload = Result<TenantBrandingDetailsDto>.Failure(
                Error.Failure("Test", "Resposta sintética de teste."));
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest)
            {
                Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(payload)),
            });
        }
    }
}