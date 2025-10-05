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

        // Momsprocent skal være mellem 0 og 0.5 (0% til 50%)
        RuleFor(r => r.MomsProcent)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Momsprocent skal være 0 eller større")
            .LessThan(0.5m)
            .WithMessage("Momsprocent skal være mindre end 0,5 (50%)");

        // Conditionale moms validering baseret på momsprocent
        RuleFor(r => r.KontoTilgodehavendeMoms)
            .Must((regnskab, konto) => ValidateBasicMomsKonto(regnskab, konto))
            .WithMessage("Hvis momsprocent er 0 skal tilgodehavende moms konto være 0, ellers skal den være >0 og ≤1.000.000");

        RuleFor(r => r.KontoSkyldigMoms)
            .Must((regnskab, konto) => ValidateBasicMomsKonto(regnskab, konto))
            .WithMessage("Hvis momsprocent er 0 skal skyldig moms konto være 0, ellers skal den være >0 og ≤1.000.000");
    }

    /// <summary>
    /// Validerer basic moms konto regler baseret på momsprocent (uden kontoplan check)
    /// </summary>
    private static bool ValidateBasicMomsKonto(Regnskab regnskab, int konto)
    {
        if (regnskab.MomsProcent == 0)
        {
            // Hvis momsprocent er 0, skal moms konti være 0
            return konto == 0;
        }
        else
        {
            // Hvis momsprocent > 0, skal moms konti være >0 og ≤1.000.000
            // Kontoplan check skal ske senere efter kontoplan er indlæst
            return konto > 0 && konto <= 1_000_000;
        }
    }

    /// <summary>
    /// Validerer om en dato er gyldig og ikke default værdi
    /// </summary>
    private static bool BeValidDate(DateTime date)
    {
        return date != default && date > new DateTime(1900, 1, 1) && date < DateTime.Now.AddYears(10);
    }
}