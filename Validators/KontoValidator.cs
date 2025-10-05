using FluentValidation;
using SimpelBogfoering.Models;
using System.Globalization;

namespace SimpelBogfoering.Validators;

/// <summary>
/// Validator for Konto-model med forretningsregler
/// </summary>
public class KontoValidator : AbstractValidator<Konto>
{
    public KontoValidator()
    {
        // Kontonummer skal være mellem 1 og 1.000.000
        RuleFor(k => k.Nr)
            .GreaterThan(0)
            .WithMessage("Kontonummer skal være større end 0")
            .LessThanOrEqualTo(1_000_000)
            .WithMessage("Kontonummer må ikke overstige 1.000.000");

        // Kontonavn er påkrævet
        RuleFor(k => k.Navn)
            .NotEmpty()
            .WithMessage("Kontonavn skal være angivet")
            .MaximumLength(100)
            .WithMessage("Kontonavn må maksimalt være 100 tegn");

        // Type skal være drift, status eller sum:fra-til
        RuleFor(k => k.Type)
            .NotEmpty()
            .WithMessage("Kontotype skal være angivet")
            .Must(BeValidType)
            .WithMessage("Kontotype skal være 'drift', 'status' eller 'sum:fra-til' hvor fra < til");

        // Moms skal være INDG, UDG eller INGEN
        RuleFor(k => k.Moms)
            .NotEmpty()
            .WithMessage("Momstype skal være angivet")
            .Must(moms => string.Equals(moms, "INDG", StringComparison.Ordinal) ||
                         string.Equals(moms, "UDG", StringComparison.Ordinal) ||
                         string.Equals(moms, "INGEN", StringComparison.Ordinal))
            .WithMessage("Momstype skal være 'INDG', 'UDG' eller 'INGEN'");

        // Status konti skal altid have momstype INGEN
        RuleFor(k => k)
            .Must(konto => !string.Equals(konto.Type, "status", StringComparison.Ordinal) ||
                          string.Equals(konto.Moms, "INGEN", StringComparison.Ordinal))
            .WithMessage("Status konti (balancekonti) skal altid have momstype 'INGEN'");
    }

    private static bool BeValidType(string type)
    {
        if (string.IsNullOrEmpty(type))
            return false;

        // Tillad drift og status
        if (string.Equals(type, "drift", StringComparison.Ordinal) ||
            string.Equals(type, "status", StringComparison.Ordinal))
            return true;

        // Valider sum:fra-til format
        if (type.StartsWith("sum:", StringComparison.Ordinal))
        {
            var range = type.Substring(4); // Fjern "sum:" prefix
            var parts = range.Split('-');

            if (parts.Length == 2 &&
                int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int fra) &&
                int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int til))
            {
                return fra > 0 && til > 0 && fra < til && til <= 1_000_000;
            }
        }

        return false;
    }
}