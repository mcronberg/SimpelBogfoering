using FluentValidation;
using SimpelBogfoering.Models;

namespace SimpelBogfoering.Validators;

/// <summary>
/// Validator for Postering model
/// Sikrer at posteringsdata er gyldige
/// </summary>
public class PosteringValidator : AbstractValidator<Postering>
{
    public PosteringValidator()
    {
        // Dato skal være gyldig og ikke i fremtiden
        RuleFor(p => p.Dato)
            .NotEmpty()
            .WithMessage("Posteringsdato skal være angivet")
            .Must(BeValidDate)
            .WithMessage("Posteringsdato skal være en gyldig dato")
            .LessThanOrEqualTo(DateTime.Today)
            .WithMessage("Posteringsdato kan ikke være i fremtiden");

        // Kontonummer skal være positivt
        RuleFor(p => p.Konto)
            .GreaterThan(0)
            .WithMessage("Kontonummer skal være større end 0")
            .LessThanOrEqualTo(9999)
            .WithMessage("Kontonummer må ikke overstige 9999");

        // Tekst skal være udfyldt
        RuleFor(p => p.Tekst)
            .NotEmpty()
            .WithMessage("Posteringstekst må ikke være tom")
            .MinimumLength(3)
            .WithMessage("Posteringstekst skal være mindst 3 tegn")
            .MaximumLength(100)
            .WithMessage("Posteringstekst må maksimalt være 100 tegn");

        // Beløb skal være forskelligt fra 0
        RuleFor(p => p.Beløb)
            .NotEqual(0)
            .WithMessage("Posteringsbeløb kan ikke være 0")
            .Must(BeReasonableAmount)
            .WithMessage("Posteringsbeløb skal være mellem -1.000.000 og 1.000.000");

        // Dato må ikke være ældre end 10 år
        RuleFor(p => p.Dato)
            .GreaterThan(DateTime.Today.AddYears(-10))
            .WithMessage("Posteringsdato må ikke være ældre end 10 år");
    }

    /// <summary>
    /// Validerer om en dato er gyldig og ikke default værdi
    /// </summary>
    private static bool BeValidDate(DateTime date)
    {
        return date != default && date > new DateTime(1900, 1, 1);
    }

    /// <summary>
    /// Validerer om beløbet er rimeligt (ikke ekstreme værdier)
    /// </summary>
    private static bool BeReasonableAmount(decimal amount)
    {
        return Math.Abs(amount) <= 1_000_000m; // Max 1 million kr
    }
}