using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;
using FluentAssertions;

namespace UnitTests.Backend.Messaging;

/// <summary>
/// Testes da política de composição por papel do host (Fase 10).
/// </summary>
public sealed class MessagingHostRolePolicyTests
{
    /// <summary>Verifica matriz de registro de laços.</summary>
    [Theory]
    [InlineData(MessagingHostRole.Api, false)]
    [InlineData(MessagingHostRole.Consumer, false)]
    [InlineData(MessagingHostRole.Scheduler, true)]
    [InlineData(MessagingHostRole.All, true)]
    public void ShouldRegisterSchedulerWorkers_MatchesMatrix(MessagingHostRole role, bool expected) =>
        MessagingHostRolePolicy.ShouldRegisterSchedulerWorkers(role).Should().Be(expected);

    /// <summary>Verifica matriz de consumidores.</summary>
    [Theory]
    [InlineData(MessagingHostRole.Api, false)]
    [InlineData(MessagingHostRole.Scheduler, false)]
    [InlineData(MessagingHostRole.Consumer, true)]
    [InlineData(MessagingHostRole.All, true)]
    public void ShouldRegisterMassTransitConsumers_MatchesMatrix(MessagingHostRole role, bool expected) =>
        MessagingHostRolePolicy.ShouldRegisterMassTransitConsumers(role).Should().Be(expected);

    /// <summary>Verifica matriz do health check AMQP.</summary>
    [Theory]
    [InlineData(MessagingHostRole.Api, true, false)]
    [InlineData(MessagingHostRole.Scheduler, true, true)]
    [InlineData(MessagingHostRole.Consumer, true, true)]
    [InlineData(MessagingHostRole.All, true, true)]
    [InlineData(MessagingHostRole.All, false, false)]
    public void ShouldRegisterRabbitMqHealthCheck_MatchesMatrix(MessagingHostRole role, bool enabled, bool expected) =>
        MessagingHostRolePolicy.ShouldRegisterRabbitMqHealthCheck(role, enabled).Should().Be(expected);

    /// <summary>Rejeita papel inválido.</summary>
    [Fact]
    public void Parse_InvalidRole_ReturnsValidationError()
    {
        var result = MessagingHostRoleParser.Parse("Invalid");
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Messaging.InvalidHostRole");
    }

    /// <summary>Valor vazio padrão All.</summary>
    [Fact]
    public void Parse_Empty_DefaultsToAll()
    {
        MessagingHostRoleParser.Parse(null).Value.Should().Be(MessagingHostRole.All);
        MessagingHostRoleParser.Parse("  ").Value.Should().Be(MessagingHostRole.All);
    }
}