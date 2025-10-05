using FluentValidation;
using SimpelBogfoering.Models;

namespace SimpelBogfoering.Validators;

/// <summary>
/// Validator for Regnskab model
/// Sikrer at regnskabsdata er gyldige og konsistente
/// </summary>
public class RegnskabValidator : AbstractValidator<Regnskab>
{
    public RegnskabValidator()
    {
        // RegnskabsNavn skal være udfyldt
        RuleFor(r => r.RegnskabsNavn)
            .NotEmpty()
            .WithMessage("Regnskabsnavn må ikke være tomt")
            .MinimumLength(2)
            .WithMessage("Regnskabsnavn skal være mindst 2 tegn")
            .MaximumLength(100)
            .WithMessage("Regnskabsnavn må maksimalt være 100 tegn");

        // PeriodeFra skal være en gyldig dato
        RuleFor(r => r.PeriodeFra)
            .NotEmpty()
            .WithMessage("Periode fra skal være angivet")
            .Must(BeValidDate)
            .WithMessage("Periode fra skal være en gyldig dato");

        // PeriodeTil skal være en gyldig dato
        RuleFor(r => r.PeriodeTil)
            .NotEmpty()
            .WithMessage("Periode til skal være angivet")
            .Must(BeValidDate)
            .WithMessage("Periode til skal være en gyldig dato");

        // PeriodeTil skal være efter PeriodeFra
        RuleFor(r => r.PeriodeTil)
            .GreaterThan(r => r.PeriodeFra)
            .WithMessage("Periode til skal være efter periode fra");

        // Regnskabsperioden må ikke være længere end 2 år
        RuleFor(r => r)
            .Must(r => (r.PeriodeTil - r.PeriodeFra).TotalDays <= 731) // 2 år + 1 dag for skudår
            .WithMessage("Regnskabsperioden må ikke overstige 2 år");

        // Konto for tilgodehavende moms skal være et gyldigt kontonummer
        RuleFor(r => r.KontoTilgodehavendeMoms)
            .GreaterThan(0)
            .WithMessage("Konto for tilgodehavende moms skal være større end 0")
            .LessThanOrEqualTo(1_000_000)
            .WithMessage("Konto for tilgodehavende moms må ikke overstige 1.000.000");

        // Konto for skyldig moms skal være et gyldigt kontonummer
        RuleFor(r => r.KontoSkyldigMoms)
            .GreaterThan(0)
            .WithMessage("Konto for skyldig moms skal være større end 0")
            .LessThanOrEqualTo(1_000_000)
            .WithMessage("Konto for skyldig moms må ikke overstige 1.000.000");
    }

    /// <summary>
    /// Validerer om en dato er gyldig og ikke default værdi
    /// </summary>
    private static bool BeValidDate(DateTime date)
    {
        return date != default && date > new DateTime(1900, 1, 1) && date < DateTime.Now.AddYears(10);
    }
}