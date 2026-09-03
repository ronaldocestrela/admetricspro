using BuildingBlocks.Application.Behaviors;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using FluentValidation;
using MediatR;
using Xunit;

namespace UnitTests.Backend.Messaging;

/// <summary>
/// Testes unitários para o <see cref="ValidationBehavior{TRequest, TResponse}"/>.
/// </summary>
public sealed class ValidationBehaviorTests
{
    private sealed record SampleCommand(string Name, decimal Budget) : ICommand;
    private sealed record SampleValueCommand(string Title, int Quantity) : ICommand<int>;
    private sealed record SampleQuery(string Filter) : IQuery<string>;

    /// <summary>
    /// Verifica que quando nenhum validador é registrado para a requisição, o delegate next é invocado diretamente.
    /// </summary>
    [Fact]
    public async Task Handle_WhenNoValidatorsExist_ShouldInvokeNextAndReturnResult()
    {
        // Arrange
        var validators = Enumerable.Empty<IValidator<SampleCommand>>();
        var behavior = new ValidationBehavior<SampleCommand, Result>(validators);
        var command = new SampleCommand("Valid Name", 100m);
        var expectedResult = Result.Success();

        RequestHandlerDelegate<Result> next = (CancellationToken _) => Task.FromResult(expectedResult);

        // Act
        var result = await behavior.Handle(command, next, CancellationToken.None);

        // Assert
        result.Should().BeSameAs(expectedResult);
        result.IsSuccess.Should().BeTrue();
    }

    /// <summary>
    /// Verifica que quando a requisição passa em todas as validações, o delegate next é invocado com sucesso.
    /// </summary>
    [Fact]
    public async Task Handle_WhenValidationPasses_ShouldInvokeNextAndReturnResult()
    {
        // Arrange
        var validator = new InlineValidator<SampleCommand>();
        validator.RuleFor(x => x.Name).NotEmpty();
        validator.RuleFor(x => x.Budget).GreaterThan(0);

        var validators = new[] { validator };
        var behavior = new ValidationBehavior<SampleCommand, Result>(validators);
        var command = new SampleCommand("Campaign Alpha", 250m);
        var expectedResult = Result.Success();

        RequestHandlerDelegate<Result> next = (CancellationToken _) => Task.FromResult(expectedResult);

        // Act
        var result = await behavior.Handle(command, next, CancellationToken.None);

        // Assert
        result.Should().BeSameAs(expectedResult);
        result.IsSuccess.Should().BeTrue();
    }

    /// <summary>
    /// Verifica que falhas de validação em comandos não-genéricos retornam Result.Failure sem invocar next.
    /// </summary>
    [Fact]
    public async Task Handle_WhenValidationFailsForNonGenericResult_ShouldReturnValidationFailureWithoutCallingNext()
    {
        // Arrange
        var validator = new InlineValidator<SampleCommand>();
        validator.RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.");
        validator.RuleFor(x => x.Budget).GreaterThan(0).WithMessage("Budget must be positive.");

        var validators = new[] { validator };
        var behavior = new ValidationBehavior<SampleCommand, Result>(validators);
        var command = new SampleCommand("", -10m);

        var nextInvoked = false;
        RequestHandlerDelegate<Result> next = (CancellationToken _) =>
        {
            nextInvoked = true;
            return Task.FromResult(Result.Success());
        };

        // Act
        var result = await behavior.Handle(command, next, CancellationToken.None);

        // Assert
        nextInvoked.Should().BeFalse("Next delegate must not be invoked when validation fails.");
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be("Validation.General");

        result.Error.Should().BeOfType<ValidationError>();
        var validationError = (ValidationError)result.Error;
        validationError.Errors.Should().ContainKey("Name");
        validationError.Errors.Should().ContainKey("Budget");
    }

    /// <summary>
    /// Verifica que falhas de validação em comandos genéricos Result de TValue retornam envelope de falha sem invocar next.
    /// </summary>
    [Fact]
    public async Task Handle_WhenValidationFailsForGenericResult_ShouldReturnGenericValidationFailureWithoutCallingNext()
    {
        // Arrange
        var validator = new InlineValidator<SampleValueCommand>();
        validator.RuleFor(x => x.Title).NotEmpty().WithMessage("Title is required.");
        validator.RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("Quantity must be positive.");

        var validators = new[] { validator };
        var behavior = new ValidationBehavior<SampleValueCommand, Result<int>>(validators);
        var command = new SampleValueCommand("", 0);

        var nextInvoked = false;
        RequestHandlerDelegate<Result<int>> next = (CancellationToken _) =>
        {
            nextInvoked = true;
            return Task.FromResult(Result<int>.Success(42));
        };

        // Act
        var result = await behavior.Handle(command, next, CancellationToken.None);

        // Assert
        nextInvoked.Should().BeFalse("Next delegate must not be invoked when validation fails.");
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be("Validation.General");

        result.Error.Should().BeOfType<ValidationError>();
        var validationError = (ValidationError)result.Error;
        validationError.Errors.Should().ContainKey("Title");
        validationError.Errors.Should().ContainKey("Quantity");
    }

    /// <summary>
    /// Verifica que falhas de validação em consultas IQuery de TValue retornam envelope genérico de falha.
    /// </summary>
    [Fact]
    public async Task Handle_WhenValidationFailsForGenericQuery_ShouldReturnGenericValidationFailure()
    {
        // Arrange
        var validator = new InlineValidator<SampleQuery>();
        validator.RuleFor(x => x.Filter).MinimumLength(3).WithMessage("Filter must have at least 3 characters.");

        var validators = new[] { validator };
        var behavior = new ValidationBehavior<SampleQuery, Result<string>>(validators);
        var query = new SampleQuery("ab");

        var nextInvoked = false;
        RequestHandlerDelegate<Result<string>> next = (CancellationToken _) =>
        {
            nextInvoked = true;
            return Task.FromResult(Result<string>.Success("All data"));
        };

        // Act
        var result = await behavior.Handle(query, next, CancellationToken.None);

        // Assert
        nextInvoked.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Description.Should().Contain("Filter must have at least 3 characters.");
    }
}
