using FluentValidation;
using SimpelBogfoering.Models;
using SimpelBogfoering.Services;

namespace SimpelBogfoering.Validators;

/// <summary>
/// Validator for Postering model med forretningsregler
/// </summary>
public class PosteringValidator : AbstractValidator<Postering>
{
    private readonly RegnskabService _regnskabService;
    private readonly KontoplanService _kontoplanService;

    public PosteringValidator(RegnskabService regnskabService, KontoplanService kontoplanService)
    {
        _regnskabService = regnskabService;
        _kontoplanService = kontoplanService;

        // Dato skal være inden for regnskabsperioden
        RuleFor(p => p.Dato)
            .NotEmpty()
            .WithMessage("Posteringsdato skal være angivet")
            .Must(DatoErIndenForRegnskabsperiode)
            .WithMessage("Posteringsdato skal være inden for regnskabsperioden");

        // Bilagsnummer skal være mellem 1 og 1.000.000
        RuleFor(p => p.Bilagsnummer)
            .GreaterThan(0)
            .WithMessage("Bilagsnummer skal være større end 0")
            .LessThanOrEqualTo(1_000_000)
            .WithMessage("Bilagsnummer må ikke overstige 1.000.000");

        // Konto skal være mellem 1 og 1.000.000 og skal findes i kontoplanen
        RuleFor(p => p.Konto)
            .GreaterThan(0)
            .WithMessage("Kontonummer skal være større end 0")
            .LessThanOrEqualTo(1_000_000)
            .WithMessage("Kontonummer må ikke overstige 1.000.000")
            .Must(KontoFindesIKontoplan)
            .WithMessage("Kontoen findes ikke i kontoplanen");

        // Tekst skal være mindst 3 tegn
        RuleFor(p => p.Tekst)
            .NotEmpty()
            .WithMessage("Posteringstekst skal være angivet")
            .MinimumLength(3)
            .WithMessage("Posteringstekst skal være mindst 3 tegn")
            .MaximumLength(200)
            .WithMessage("Posteringstekst må maksimalt være 200 tegn");

        // Beløb må ikke være nul
        RuleFor(p => p.Beløb)
            .NotEqual(0)
            .WithMessage("Beløb må ikke være nul");

        // CSV fil skal være angivet
        RuleFor(p => p.CsvFil)
            .NotEmpty()
            .WithMessage("CSV filnavn skal være angivet");
    }

    private bool DatoErIndenForRegnskabsperiode(DateTime dato)
    {
        var regnskab = _regnskabService.Regnskab;
        return dato >= regnskab.PeriodeFra && dato <= regnskab.PeriodeTil;
    }

    private bool KontoFindesIKontoplan(int kontonummer)
    {
        return _kontoplanService.GetKonto(kontonummer) != null;
    }
}