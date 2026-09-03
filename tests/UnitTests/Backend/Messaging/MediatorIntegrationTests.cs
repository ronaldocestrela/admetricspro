using BuildingBlocks.Application.DependencyInjection;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace UnitTests.Backend.Messaging;

/// <summary>
/// Testes de integração para o pipeline do MediatR, FluentValidation e injeção de dependências.
/// </summary>
public sealed class MediatorIntegrationTests
{
    /// <summary>
    /// Comando de teste com retorno não-genérico.
    /// </summary>
    public sealed record PingCommand(string Message) : ICommand;

    /// <summary>
    /// Validador do comando de teste PingCommand.
    /// </summary>
    public sealed class PingCommandValidator : AbstractValidator<PingCommand>
    {
        /// <summary>
        /// Inicializa as regras de validação.
        /// </summary>
        public PingCommandValidator()
        {
            RuleFor(x => x.Message).NotEmpty().WithMessage("Message must not be empty.");
        }
    }

    /// <summary>
    /// Handler para o comando PingCommand.
    /// </summary>
    public sealed class PingCommandHandler : ICommandHandler<PingCommand>
    {
        /// <summary>
        /// Manipula o comando.
        /// </summary>
        public Task<Result> Handle(PingCommand request, CancellationToken cancellationToken)
        {
            return Task.FromResult(Result.Success());
        }
    }

    /// <summary>
    /// Comando de teste tipado com retorno numérico.
    /// </summary>
    public sealed record CalculateScoreCommand(int Factor) : ICommand<int>;

    /// <summary>
    /// Validador do comando CalculateScoreCommand.
    /// </summary>
    public sealed class CalculateScoreCommandValidator : AbstractValidator<CalculateScoreCommand>
    {
        /// <summary>
        /// Inicializa as regras de validação.
        /// </summary>
        public CalculateScoreCommandValidator()
        {
            RuleFor(x => x.Factor).GreaterThan(0).WithMessage("Factor must be strictly positive.");
        }
    }

    /// <summary>
    /// Handler para o comando CalculateScoreCommand.
    /// </summary>
    public sealed class CalculateScoreCommandHandler : ICommandHandler<CalculateScoreCommand, int>
    {
        /// <summary>
        /// Manipula o comando calculando o score.
        /// </summary>
        public Task<Result<int>> Handle(CalculateScoreCommand request, CancellationToken cancellationToken)
        {
            return Task.FromResult(Result<int>.Success(request.Factor * 10));
        }
    }

    /// <summary>
    /// Evento de domínio de teste.
    /// </summary>
    public sealed record TestTenantCreatedEvent(Guid TenantId, string Name) : IDomainEvent;

    /// <summary>
    /// Handler do evento de domínio de teste.
    /// </summary>
    public sealed class TestTenantCreatedEventHandler : IDomainEventHandler<TestTenantCreatedEvent>
    {
        /// <summary>
        /// Obtém o ID do último tenant recebido.
        /// </summary>
        public static Guid LastReceivedTenantId { get; set; }

        /// <summary>
        /// Manipula a notificação do evento de domínio.
        /// </summary>
        public Task Handle(DomainEventNotification<TestTenantCreatedEvent> notification, CancellationToken cancellationToken)
        {
            LastReceivedTenantId = notification.DomainEvent.TenantId;
            return Task.CompletedTask;
        }
    }

    private static IServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddMessaging(typeof(MediatorIntegrationTests).Assembly);
        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Verifica que o comando válido transita pelo MediatR e pelo handler com sucesso.
    /// </summary>
    [Fact]
    public async Task Send_ValidCommand_ShouldPassThroughPipelineAndSucceed()
    {
        // Arrange
        var provider = BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act
        var result = await mediator.Send(new PingCommand("Hello MediatR"));

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    /// <summary>
    /// Verifica que o comando inválido é barrado pelo ValidationBehavior antes do handler.
    /// </summary>
    [Fact]
    public async Task Send_InvalidCommand_ShouldBeInterceptedByValidationBehavior()
    {
        // Arrange
        var provider = BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act
        var result = await mediator.Send(new PingCommand(""));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Should().BeOfType<ValidationError>();

        var valError = (ValidationError)result.Error;
        valError.Errors.Should().ContainKey("Message");
    }

    /// <summary>
    /// Verifica que o comando tipado válido retorna o payload calculado.
    /// </summary>
    [Fact]
    public async Task Send_ValidGenericCommand_ShouldReturnTypedPayload()
    {
        // Arrange
        var provider = BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act
        var result = await mediator.Send(new CalculateScoreCommand(5));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(50);
    }

    /// <summary>
    /// Verifica que a publicação de evento de domínio é entregue ao handler desacoplado.
    /// </summary>
    [Fact]
    public async Task Publish_DomainEventNotification_ShouldDeliverToHandler()
    {
        // Arrange
        var provider = BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var tenantId = Guid.NewGuid();
        var domainEvent = new TestTenantCreatedEvent(tenantId, "Tenant Alfa");

        // Act
        await mediator.Publish(new DomainEventNotification<TestTenantCreatedEvent>(domainEvent));

        // Assert
        TestTenantCreatedEventHandler.LastReceivedTenantId.Should().Be(tenantId);
    }
}
