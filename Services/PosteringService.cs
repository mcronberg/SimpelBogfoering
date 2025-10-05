using FluentValidation;
using Microsoft.Extensions.Logging;
using SimpelBogfoering.Models;
using SimpelBogfoering.Validators;
using System.Globalization;
using System.Text;

namespace SimpelBogfoering.Services;

/// <summary>
/// Service til håndtering af posteringer fra CSV filer
/// Indlæser og validerer posteringer fra filer med format "posteringer*.csv"
/// </summary>
public class PosteringService
{
    private readonly ILogger<PosteringService> _logger;
    private readonly CommandArgs _commandArgs;
    private readonly PosteringValidator _validator;
    private readonly RegnskabService _regnskabService;
    private readonly KontoplanService _kontoplanService;
    private List<Postering> _posteringer = new();

    public PosteringService(
        ILogger<PosteringService> logger,
        CommandArgs commandArgs,
        PosteringValidator validator,
        RegnskabService regnskabService,
        KontoplanService kontoplanService)
    {
        _logger = logger;
        _commandArgs = commandArgs;
        _validator = validator;
        _regnskabService = regnskabService;
        _kontoplanService = kontoplanService;
    }    /// <summary>
         /// Alle indlæste posteringer
         /// </summary>
    public IReadOnlyList<Postering> Posteringer => _posteringer.AsReadOnly();

    /// <summary>
    /// Indlæs alle posteringer fra CSV filer i input directory
    /// Filer skal have format "posteringer*.csv"
    /// </summary>
    public async Task LoadPosteringerAsync()
    {
        _logger.LogInformation("Starter indlæsning af posteringer fra: {InputPath}", _commandArgs.Input);

        try
        {
            var posteringerFiler = Directory.GetFiles(_commandArgs.Input, "posteringer*.csv", SearchOption.TopDirectoryOnly);

            if (posteringerFiler.Length == 0)
            {
                _logger.LogWarning("Ingen posteringer*.csv filer fundet i: {InputPath}", _commandArgs.Input);
                return;
            }

            _logger.LogInformation("Fundet {AntalFiler} posteringer filer", posteringerFiler.Length);

            var allePosteringer = new List<Postering>();

            foreach (var filePath in posteringerFiler.OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
            {
                var posteringerFraFil = await LoadPosteringerFraFilAsync(filePath).ConfigureAwait(false);
                if (posteringerFraFil.Any())
                {
                    allePosteringer.AddRange(posteringerFraFil);

                    // Validér at hver fil balancerer (summen af alle posteringer i filen = 0)
                    var filBalance = posteringerFraFil.Sum(p => p.Beløb);
                    if (filBalance != 0)
                    {
                        var filNavn = Path.GetFileName(filePath);
                        throw new InvalidOperationException(
                            $"Fil {filNavn} balancerer ikke (sum: {filBalance:F2} kr). " +
                            "Summen af alle posteringer i hver fil skal være 0.");
                    }

                    _logger.LogInformation("Fil {FilNavn} indlæst korrekt med {AntalPosteringer} posteringer (balanceret)",
                        Path.GetFileName(filePath), posteringerFraFil.Count);
                }
            }

            _posteringer = allePosteringer;
            _logger.LogInformation("Indlæste i alt {AntalPosteringer} posteringer fra {AntalFiler} filer",
                _posteringer.Count, posteringerFiler.Length);

            // Generer automatiske momsposteringer
            await GenerateAutomaticMomsPosteringerAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fejl under indlæsning af posteringer");
            throw;
        }
    }

    /// <summary>
    /// Indlæs posteringer fra en specifik CSV fil
    /// </summary>
    private async Task<List<Postering>> LoadPosteringerFraFilAsync(string filePath)
    {
        var filNavn = Path.GetFileName(filePath);
        _logger.LogDebug("Indlæser posteringer fra: {FilNavn}", filNavn);

        try
        {
            var lines = await File.ReadAllLinesAsync(filePath, Encoding.UTF8).ConfigureAwait(false);
            if (lines.Length == 0)
            {
                _logger.LogWarning("Fil {FilNavn} er tom", filNavn);
                return new List<Postering>();
            }

            ValidateCSVHeader(lines[0], filNavn);
            return await ParsePosteringerFromLines(lines, filNavn).ConfigureAwait(false);
        }
        catch (Exception ex) when (!(ex is InvalidOperationException))
        {
            _logger.LogError(ex, "Uventet fejl under indlæsning af fil: {FilNavn}", filNavn);
            throw new InvalidOperationException($"Fejl under indlæsning af {filNavn}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Validerer CSV header
    /// </summary>
    private static void ValidateCSVHeader(string headerLine, string filNavn)
    {
        var expectedHeader = "Dato;Bilagsnummer;Konto;Tekst;Beløb";
        if (!string.Equals(headerLine, expectedHeader, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Ugyldig header i {filNavn}. Forventet: '{expectedHeader}', fandt: '{headerLine}'");
        }
    }

    /// <summary>
    /// Parser posteringer fra CSV linjer
    /// </summary>
    private async Task<List<Postering>> ParsePosteringerFromLines(string[] lines, string filNavn)
    {
        var posteringer = new List<Postering>();
        var fejlListe = new List<string>();

        for (int i = 1; i < lines.Length; i++)
        {
            var lineNumber = i + 1;
            var line = lines[i];

            // Skip tomme linjer
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            try
            {
                var postering = ParsePosteringLine(line, filNavn);

                // Validér postering
                var validationResult = await _validator.ValidateAsync(postering).ConfigureAwait(false);
                if (!validationResult.IsValid)
                {
                    var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                    fejlListe.Add($"Linje {lineNumber}: {errors}");
                    continue;
                }

                posteringer.Add(postering);
            }
            catch (Exception ex)
            {
                fejlListe.Add($"Linje {lineNumber}: {ex.Message}");
            }
        }

        // Hvis der er valideringsfejl, kast exception med alle fejl
        if (fejlListe.Any())
        {
            var fejlBesked = $"Valideringsfejl i {filNavn}:\n" + string.Join("\n", fejlListe);
            throw new InvalidOperationException(fejlBesked);
        }

        return posteringer;
    }

    /// <summary>
    /// Parse en CSV linje til Postering object
    /// </summary>
    private static Postering ParsePosteringLine(string line, string filNavn)
    {
        var fields = line.Split(';');
        if (fields.Length != 5)
        {
            throw new FormatException($"Ugyldig CSV format. Forventet 5 felter, fandt {fields.Length}");
        }

        try
        {
            return new Postering
            {
                Dato = ParseDato(fields[0]),
                Bilagsnummer = ParseInt(fields[1], "Bilagsnummer"),
                Konto = ParseInt(fields[2], "Konto"),
                Tekst = fields[3].Trim(),
                Beløb = ParseBeløb(fields[4]),
                CsvFil = filNavn
            };
        }
        catch (Exception ex)
        {
            throw new FormatException($"Fejl i parsing af linje: {ex.Message}");
        }
    }

    /// <summary>
    /// Parse en dato fra CSV felt med dansk format (dd-MM-yyyy)
    /// </summary>
    private static DateTime ParseDato(string datoText)
    {
        if (string.IsNullOrWhiteSpace(datoText))
        {
            throw new FormatException("Dato felt er tomt");
        }

        if (!DateTime.TryParseExact(datoText.Trim(), "dd-MM-yyyy",
            CultureInfo.GetCultureInfo("da-DK"), DateTimeStyles.None, out var dato))
        {
            throw new FormatException($"Ugyldig dato format: '{datoText}'. Forventet format: dd-MM-yyyy");
        }

        return dato;
    }

    /// <summary>
    /// Parse et integer felt fra CSV
    /// </summary>
    private static int ParseInt(string intText, string feltNavn)
    {
        if (string.IsNullOrWhiteSpace(intText))
        {
            throw new FormatException($"{feltNavn} felt er tomt");
        }

        if (!int.TryParse(intText.Trim(), NumberStyles.Integer,
            CultureInfo.GetCultureInfo("da-DK"), out var værdi))
        {
            throw new FormatException($"Ugyldigt {feltNavn}: '{intText}'");
        }

        return værdi;
    }

    /// <summary>
    /// Parse et beløb fra CSV felt med dansk format
    /// </summary>
    private static decimal ParseBeløb(string beløbText)
    {
        if (string.IsNullOrWhiteSpace(beløbText))
        {
            throw new FormatException("Beløb felt er tomt");
        }

        // Understøt både komma og punktum som decimalseparator
        var normalizedText = beløbText.Trim().Replace('.', ',');

        if (!decimal.TryParse(normalizedText, NumberStyles.Number,
            CultureInfo.GetCultureInfo("da-DK"), out var beløb))
        {
            throw new FormatException($"Ugyldigt beløb: '{beløbText}'");
        }

        return beløb;
    }

    /// <summary>
    /// Hent posteringer for en specifik konto
    /// </summary>
    public IEnumerable<Postering> GetPosteringerForKonto(int kontonummer)
    {
        return _posteringer.Where(p => p.Konto == kontonummer);
    }

    /// <summary>
    /// Hent posteringer for en dato periode
    /// </summary>
    public IEnumerable<Postering> GetPosteringerForPeriode(DateTime fra, DateTime til)
    {
        return _posteringer.Where(p => p.Dato >= fra && p.Dato <= til);
    }

    /// <summary>
    /// Beregn saldo for en konto
    /// </summary>
    public decimal GetSaldoForKonto(int kontonummer)
    {
        return _posteringer.Where(p => p.Konto == kontonummer).Sum(p => p.Beløb);
    }

    /// <summary>
    /// Generer automatiske momsposteringer baseret på kontoplanens momstyper
    /// </summary>
    private async Task GenerateAutomaticMomsPosteringerAsync()
    {
        var regnskab = _regnskabService.Regnskab;

        // Hvis momsprocent er 0, generer ingen momsposteringer
        if (regnskab.MomsProcent == 0)
        {
            _logger.LogDebug("Momsprocent er 0 - ingen automatiske momsposteringer genereres");
            return;
        }

        _logger.LogInformation("Genererer automatiske momsposteringer med momsprocent {MomsProcent:P2}", regnskab.MomsProcent);

        var nyeMomsPosteringer = GenerateMomsPosteringerForAllePosteringer(regnskab);
        await ValidateAndAddMomsPosteringerAsync(nyeMomsPosteringer).ConfigureAwait(false);
    }

    private List<Postering> GenerateMomsPosteringerForAllePosteringer(Regnskab regnskab)
    {
        // Kopier liste for at undgå modification during iteration
        var originalPosteringer = _posteringer.ToList();
        var nyeMomsPosteringer = new List<Postering>();

        foreach (var postering in originalPosteringer)
        {
            var momsPosteringer = GenerateMomsPosteringerForPosteringIfApplicable(postering, regnskab);
            nyeMomsPosteringer.AddRange(momsPosteringer);
        }

        return nyeMomsPosteringer;
    }

    private List<Postering> GenerateMomsPosteringerForPosteringIfApplicable(Postering postering, Regnskab regnskab)
    {
        var konto = _kontoplanService.GetKonto(postering.Konto);
        if (string.Equals(konto?.Moms, "INGEN", StringComparison.OrdinalIgnoreCase))
        {
            return new List<Postering>(); // Skip konti uden moms
        }

        // Beregn moms fra brutto beløb (inklusive moms) til netto momsbeløb
        // Formel: momsBeløb = bruttoBeløb * (momsprocent / (1 + momsprocent))
        decimal momsBeløb = Math.Round(Math.Abs(postering.Beløb) * (regnskab.MomsProcent / (1 + regnskab.MomsProcent)), 2);

        if (string.Equals(konto?.Moms, "INDG", StringComparison.OrdinalIgnoreCase)) // Indgående moms (køb/udgifter)
        {
            return CreateMomsPosteringerForIndgående(postering, momsBeløb, regnskab.KontoTilgodehavendeMoms);
        }
        else if (string.Equals(konto?.Moms, "UDG", StringComparison.OrdinalIgnoreCase)) // Udgående moms (salg/indtægter)
        {
            return CreateMomsPosteringerForUdgående(postering, momsBeløb, regnskab.KontoSkyldigMoms);
        }

        return new List<Postering>();
    }

    private async Task ValidateAndAddMomsPosteringerAsync(List<Postering> nyeMomsPosteringer)
    {
        if (nyeMomsPosteringer.Any())
        {
            // Validér alle nye momsposteringer
            foreach (var momsPostering in nyeMomsPosteringer)
            {
                var validationResult = await _validator.ValidateAsync(momsPostering).ConfigureAwait(false);
                if (!validationResult.IsValid)
                {
                    var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                    throw new InvalidOperationException($"Automatisk genereret momspostering er ugyldig: {errors}");
                }
            }

            _posteringer.AddRange(nyeMomsPosteringer);
            _logger.LogInformation("Genererede {AntalMomsPosteringer} automatiske momsposteringer", nyeMomsPosteringer.Count);
        }
        else
        {
            _logger.LogDebug("Ingen konti med moms fundet - ingen momsposteringer genereret");
        }
    }

    /// <summary>
    /// Opretter momsposteringer for indgående moms (INDG)
    /// </summary>
    private static List<Postering> CreateMomsPosteringerForIndgående(Postering originalPostering, decimal momsBeløb, int tilgodehavendeMomsKonto)
    {
        var momsPosteringer = new List<Postering>();

        // Momspostering på samme konto (negativ hvis original er positiv og omvendt)
        var momsPostering1 = new Postering
        {
            Dato = originalPostering.Dato,
            Bilagsnummer = originalPostering.Bilagsnummer,
            Konto = originalPostering.Konto,
            Tekst = $"Moms af {originalPostering.Tekst}",
            Beløb = originalPostering.Beløb >= 0 ? -momsBeløb : momsBeløb,
            CsvFil = "Autogenereret"
        };

        // Modpostering på tilgodehavende moms konto
        var momsPostering2 = new Postering
        {
            Dato = originalPostering.Dato,
            Bilagsnummer = originalPostering.Bilagsnummer,
            Konto = tilgodehavendeMomsKonto,
            Tekst = $"Moms af {originalPostering.Tekst}",
            Beløb = originalPostering.Beløb >= 0 ? momsBeløb : -momsBeløb,
            CsvFil = "Autogenereret"
        };

        momsPosteringer.Add(momsPostering1);
        momsPosteringer.Add(momsPostering2);
        return momsPosteringer;
    }

    /// <summary>
    /// Opretter momsposteringer for udgående moms (UDG)
    /// </summary>
    private static List<Postering> CreateMomsPosteringerForUdgående(Postering originalPostering, decimal momsBeløb, int skyldigMomsKonto)
    {
        var momsPosteringer = new List<Postering>();

        // Momspostering på samme konto (positiv hvis original er negativ og omvendt)
        var momsPostering1 = new Postering
        {
            Dato = originalPostering.Dato,
            Bilagsnummer = originalPostering.Bilagsnummer,
            Konto = originalPostering.Konto,
            Tekst = $"Moms af {originalPostering.Tekst}",
            Beløb = originalPostering.Beløb >= 0 ? momsBeløb : -momsBeløb,
            CsvFil = "Autogenereret"
        };

        // Modpostering på skyldig moms konto
        var momsPostering2 = new Postering
        {
            Dato = originalPostering.Dato,
            Bilagsnummer = originalPostering.Bilagsnummer,
            Konto = skyldigMomsKonto,
            Tekst = $"Moms af {originalPostering.Tekst}",
            Beløb = originalPostering.Beløb >= 0 ? -momsBeløb : momsBeløb,
            CsvFil = "Autogenereret"
        };

        momsPosteringer.Add(momsPostering1);
        momsPosteringer.Add(momsPostering2);
        return momsPosteringer;
    }
}