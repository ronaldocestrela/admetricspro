using BuildingBlocks.Application.Messaging;
using FluentAssertions;
using Xunit;

namespace UnitTests.Backend.Messaging;

/// <summary>
/// Garante o catálogo de erros do módulo de mensageria.
/// </summary>
public sealed class MessagingErrorsCatalogTests
{
    /// <summary>
    /// Códigos da Fase 3 devem estar definidos.
    /// </summary>
    [Fact]
    public void Catalogo_DeveConterCodigosOperacionais()
    {
        MessagingErrors.Disabled.Code.Should().Be("Messaging.Disabled");
        MessagingErrors.LeaseNotAcquired.Code.Should().Be("Messaging.LeaseNotAcquired");
        MessagingErrors.ReprocessPending.Code.Should().Be("Messaging.ReprocessPending");
        MessagingErrors.PublishFailed.Code.Should().Be("Messaging.PublishFailed");
        MessagingErrors.PayloadInvalid.Code.Should().Be("Messaging.PayloadInvalid");
        MessagingErrors.TenantRequired.Code.Should().Be("Messaging.TenantRequired");
    }
}