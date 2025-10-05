using Microsoft.Extensions.Logging;
using SimpelBogfoering.Models;
using System.Text;
using System.Globalization;
using FluentValidation;

namespace SimpelBogfoering.Services;

/// <summary>
/// Service til at indlæse og håndtere regnskabsdata
/// Indlæser regnskab.json ved startup og validerer dataene
/// </summary>
public class RegnskabService
{
    private readonly ILogger<RegnskabService> _logger;
    private readonly CommandArgs _commandArgs;
    private readonly IValidator<Regnskab> _validator;
    private Regnskab? _regnskab;

    public RegnskabService(
        ILogger<RegnskabService> logger,
        CommandArgs commandArgs,
        IValidator<Regnskab> validator)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _commandArgs = commandArgs ?? throw new ArgumentNullException(nameof(commandArgs));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    /// <summary>
    /// Det indlæste regnskabsobjekt (readonly efter indlæsning)
    /// </summary>
    public Regnskab Regnskab => _regnskab ?? throw new InvalidOperationException("Regnskab er ikke indlæst endnu. Kald LoadRegnskabAsync først.");

    /// <summary>
    /// Indlæser regnskabsdata fra CSV fil og validerer det
    /// </summary>
    public async Task LoadRegnskabAsync()
    {
        try
        {
            var filePath = Path.Combine(_commandArgs.Input, _commandArgs.RegnskabFileName);

            _logger.LogInformation("Indlæser regnskabsdata fra: {FilePath}", filePath);

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Regnskabsfil ikke fundet: {filePath}");
            }

            // Læs og parse CSV fil
            var regnskab = await ParseRegnskabFromCsvAsync(filePath).ConfigureAwait(false);

            // Validér dataene
            _logger.LogDebug("Validerer regnskabsdata...");
            var validationResult = await _validator.ValidateAsync(regnskab).ConfigureAwait(false);

            if (!validationResult.IsValid)
            {
                var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                throw new ValidationException($"Regnskabsdata er ikke gyldige: {errors}");
            }

            _regnskab = regnskab;

            _logger.LogInformation("Regnskabsdata indlæst og valideret: {RegnskabsNavn}, periode {PeriodeFra:yyyy-MM-dd} til {PeriodeTil:yyyy-MM-dd}",
                regnskab.RegnskabsNavn, regnskab.PeriodeFra, regnskab.PeriodeTil);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fejl ved indlæsning af regnskabsdata");
            throw;
        }
    }

    private async Task<Regnskab> ParseRegnskabFromCsvAsync(string filePath)
    {
        // Læs CSV fil med UTF-8 encoding
        var lines = await File.ReadAllLinesAsync(filePath, Encoding.UTF8).ConfigureAwait(false);

        if (lines.Length < 2)
        {
            throw new InvalidOperationException("Regnskab CSV-fil skal indeholde header og mindst én datarad");
        }

        var header = lines[0].Split(';');
        var dataRow = lines[1].Split(';');

        // Valider header format
        ValidateRegnskabCsvHeader(header, dataRow);

        // Parse data
        var periodeFra = ParseDate(dataRow[1], "periodeFra");
        var periodeTil = ParseDate(dataRow[2], "periodeTil");
        var kontoTilgodehavendeMoms = ParseInt(dataRow[3], "kontoTilgodehavendeMoms");
        var kontoSkyldigMoms = ParseInt(dataRow[4], "kontoSkyldigMoms");

        return new Regnskab
        {
            RegnskabsNavn = dataRow[0].Trim(),
            PeriodeFra = periodeFra,
            PeriodeTil = periodeTil,
            KontoTilgodehavendeMoms = kontoTilgodehavendeMoms,
            KontoSkyldigMoms = kontoSkyldigMoms
        };
    }

    private static void ValidateRegnskabCsvHeader(string[] header, string[] dataRow)
    {
        if (header.Length != 5 || dataRow.Length != 5 ||
            !string.Equals(header[0], "regnskabsNavn", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(header[1], "periodeFra", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(header[2], "periodeTil", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(header[3], "kontoTilgodehavendeMoms", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(header[4], "kontoSkyldigMoms", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Regnskab CSV-fil skal have header: regnskabsNavn;periodeFra;periodeTil;kontoTilgodehavendeMoms;kontoSkyldigMoms");
        }
    }

    private static DateTime ParseDate(string value, string fieldName)
    {
        if (!DateTime.TryParse(value, CultureInfo.InvariantCulture, out var result))
        {
            throw new InvalidOperationException($"Ugyldig {fieldName} dato: {value}");
        }
        return result;
    }

    private static int ParseInt(string value, string fieldName)
    {
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
        {
            throw new InvalidOperationException($"Ugyldigt {fieldName}: {value}");
        }
        return result;
    }

    /// <summary>
    /// Tjekker om regnskabsdata er indlæst
    /// </summary>
    public bool IsLoaded => _regnskab != null;

    /// <summary>
    /// Henter regnskabsinformation som string (til logging/debug)
    /// </summary>
    public string GetRegnskabInfo()
    {
        if (!IsLoaded)
            return "Regnskab ikke indlæst";

        return $"{_regnskab!.RegnskabsNavn} ({_regnskab.PeriodeFra:yyyy-MM-dd} - {_regnskab.PeriodeTil:yyyy-MM-dd})";
    }
}