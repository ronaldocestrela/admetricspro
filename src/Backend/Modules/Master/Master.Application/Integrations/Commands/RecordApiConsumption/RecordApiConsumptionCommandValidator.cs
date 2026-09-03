using FluentValidation;

namespace Master.Application.Integrations.Commands.RecordApiConsumption;

/// <summary>
/// Validator for <see cref="RecordApiConsumptionCommand"/>.
/// </summary>
public sealed class RecordApiConsumptionCommandValidator : AbstractValidator<RecordApiConsumptionCommand>
{
    /// <summary>
    /// Initializes validation rules for recording API consumption.
    /// </summary>
    public RecordApiConsumptionCommandValidator()
    {
        RuleFor(x => x.Platform)
            .IsInEnum()
            .WithMessage("Plataforma de anúncios inválida.");

        RuleFor(x => x.Units)
            .GreaterThan(0)
            .WithMessage("O consumo registrado deve ser maior que zero.");
    }
}
