using System.Text.Json;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using Xunit;

namespace UnitTests.Backend.Messaging;

/// <summary>
/// Testes de validação e serialização de <see cref="IntegrationEventEnvelope"/>.
/// </summary>
public sealed class IntegrationEventEnvelopeTests
{
    private static readonly Guid EventId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6");
    private static readonly Guid CorrelationId = Guid.Parse("7c9e6679-7425-40de-944b-e07fc1f90ae7");
    private static readonly Guid TenantId = Guid.Parse("2f8b3c1a-9d4e-4b7c-a1e2-3f4d5e6a7b8c");
    private static readonly DateTimeOffset OccurredOn = new(2026, 11, 25, 18, 30, 0, TimeSpan.Zero);

  private static JsonElement SamplePayload =>
        JsonDocument.Parse("""{"connectedAdAccountId":"act_123","platform":"Meta"}""").RootElement.Clone();

    /// <summary>
    /// EventId vazio deve retornar PayloadInvalid.
    /// </summary>
    [Fact]
    public void Create_ComEventIdVazio_DeveRetornarPayloadInvalid()
    {
        var result = IntegrationEventEnvelope.Create(
            Guid.Empty,
            "integrations.campaign-metrics-synced.v1",
            CorrelationId,
            OccurredOn,
            IntegrationEventScope.Operational,
            TenantId,
            SamplePayload);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(MessagingErrors.PayloadInvalid.Code);
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    /// <summary>
    /// Tipo em branco deve retornar PayloadInvalid.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ComTipoEmBranco_DeveRetornarPayloadInvalid(string messageType)
    {
        var result = IntegrationEventEnvelope.Create(
            EventId,
            messageType,
            CorrelationId,
            OccurredOn,
            IntegrationEventScope.Operational,
            TenantId,
            SamplePayload);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(MessagingErrors.PayloadInvalid.Code);
    }

    /// <summary>
    /// Evento operacional sem tenant deve retornar TenantRequired.
    /// </summary>
    [Fact]
    public void Create_OperacionalSemTenant_DeveRetornarTenantRequired()
    {
        var result = IntegrationEventEnvelope.Create(
            EventId,
            "integrations.campaign-metrics-synced.v1",
            CorrelationId,
            OccurredOn,
            IntegrationEventScope.Operational,
            tenantId: null,
            SamplePayload);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(MessagingErrors.TenantRequired.Code);
    }

    /// <summary>
    /// Evento de catálogo com tenant nulo é aceito.
    /// </summary>
    [Fact]
    public void Create_CatalogoSemTenant_DeveSerAceito()
    {
        var result = IntegrationEventEnvelope.Create(
            EventId,
            "master.tenant-provisioned.v1",
            CorrelationId,
            OccurredOn,
            IntegrationEventScope.Catalog,
            tenantId: null,
            SamplePayload);

        result.IsSuccess.Should().BeTrue();
        result.Value.TenantId.Should().BeNull();
        result.Value.Scope.Should().Be(IntegrationEventScope.Catalog);
    }

    /// <summary>
    /// Catálogo com tenant informado também é aceito.
    /// </summary>
    [Fact]
    public void Create_CatalogoComTenant_DeveSerAceito()
    {
        var result = IntegrationEventEnvelope.Create(
            EventId,
            "master.welcome-email.v1",
            CorrelationId,
            OccurredOn,
            IntegrationEventScope.Catalog,
            TenantId,
            SamplePayload);

        result.IsSuccess.Should().BeTrue();
        result.Value.TenantId.Should().Be(TenantId);
    }

    /// <summary>
    /// CorrelationId vazio deve retornar PayloadInvalid.
    /// </summary>
    [Fact]
    public void Create_ComCorrelationIdVazio_DeveRetornarPayloadInvalid()
    {
        var result = IntegrationEventEnvelope.Create(
            EventId,
            "integrations.campaign-metrics-synced.v1",
            Guid.Empty,
            OccurredOn,
            IntegrationEventScope.Operational,
            TenantId,
            SamplePayload);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(MessagingErrors.PayloadInvalid.Code);
    }

    /// <summary>
    /// Payload que não é objeto JSON deve retornar PayloadInvalid.
    /// </summary>
    [Fact]
    public void Create_ComPayloadNaoObjeto_DeveRetornarPayloadInvalid()
    {
        var primitive = JsonDocument.Parse("\"not-an-object\"").RootElement.Clone();

        var result = IntegrationEventEnvelope.Create(
            EventId,
            "integrations.campaign-metrics-synced.v1",
            CorrelationId,
            OccurredOn,
            IntegrationEventScope.Operational,
            TenantId,
            primitive);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(MessagingErrors.PayloadInvalid.Code);
    }

    /// <summary>
    /// Serialização usa nomes explícitos em camelCase e round-trip independe da ordem das propriedades.
    /// </summary>
    [Fact]
    public void JsonRoundTrip_UsaNomesExplicitosEOrdemIndependente()
    {
        var envelope = IntegrationEventEnvelope.Create(
            EventId,
            "integrations.campaign-metrics-synced.v1",
            CorrelationId,
            OccurredOn,
            IntegrationEventScope.Operational,
            TenantId,
            SamplePayload).Value;

        var json = IntegrationEventEnvelopeSerializer.Serialize(envelope);

        json.Should().Contain("\"eventId\"");
        json.Should().Contain("\"messageType\"");
        json.Should().Contain("\"correlationId\"");
        json.Should().Contain("\"occurredOnUtc\"");
        json.Should().Contain("\"tenantId\"");
        json.Should().Contain("\"payload\"");
        json.Should().NotContain("\"Payload\":\"{");
        json.Should().Contain("\"connectedAdAccountId\"");

        var shuffled = $$"""
            {
              "payload": { "platform": "Meta", "connectedAdAccountId": "act_123" },
              "tenantId": "{{TenantId}}",
              "occurredOnUtc": "2026-11-25T18:30:00+00:00",
              "messageType": "integrations.campaign-metrics-synced.v1",
              "correlationId": "{{CorrelationId}}",
              "eventId": "{{EventId}}"
            }
            """;

        var roundTrip = IntegrationEventEnvelopeSerializer.Deserialize(shuffled);
        roundTrip.IsSuccess.Should().BeTrue();
        roundTrip.Value.EventId.Should().Be(EventId);
        roundTrip.Value.MessageType.Should().Be("integrations.campaign-metrics-synced.v1");
        roundTrip.Value.Payload.GetProperty("platform").GetString().Should().Be("Meta");
    }
}