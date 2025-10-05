using FluentValidation;
using Microsoft.Extensions.Logging;
using SimpelBogfoering.Models;
using System.Globalization;
using System.Text;

namespace SimpelBogfoering.Services;

/// <summary>
/// Service til indlæsning og håndtering af kontoplan fra CSV-fil
/// </summary>
public class KontoplanService
{
    private readonly ILogger<KontoplanService> _logger;
    private readonly CommandArgs _commandArgs;
    private readonly IValidator<Konto> _kontoValidator;
    private List<Konto> _kontoplan = new();

    public KontoplanService(
        ILogger<KontoplanService> logger,
        CommandArgs commandArgs,
        IValidator<Konto> kontoValidator)
    {
        _logger = logger;
        _commandArgs = commandArgs;
        _kontoValidator = kontoValidator;
    }

    /// <summary>
    /// Indlæser kontoplanen fra CSV-fil og validerer alle konti
    /// </summary>
    public async Task LoadKontoplanAsync()
    {
        var filePath = Path.Combine(_commandArgs.Input, _commandArgs.KontoplanFileName);

        _logger.LogInformation("Indlæser kontoplan fra: {FilePath}", filePath);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Kontoplan-fil ikke fundet: {filePath}");
        }

        try
        {
            var lines = await File.ReadAllLinesAsync(filePath, Encoding.UTF8).ConfigureAwait(false);
            if (lines.Length < 2) // Header + mindst én konto
            {
                throw new InvalidOperationException("Kontoplan-fil skal indeholde header og mindst én konto");
            }

            var kontoplan = await ParseKontoplanFromLines(lines).ConfigureAwait(false);
            await ValidateKontoplan(kontoplan).ConfigureAwait(false);

            // Check for dubletter
            var duplicates = kontoplan.GroupBy(k => k.Nr).Where(g => g.Count() > 1).Select(g => g.Key);
            if (duplicates.Any())
            {
                throw new InvalidOperationException($"Dublerede kontonumre fundet: {string.Join(", ", duplicates)}");
            }

            _kontoplan = kontoplan.OrderBy(k => k.Nr).ToList();

            _logger.LogInformation("Kontoplan indlæst og valideret: {Count} konti", _kontoplan.Count);
        }
        catch (Exception ex) when (ex is not ValidationException and not InvalidOperationException and not FileNotFoundException)
        {
            _logger.LogError(ex, "Fejl ved indlæsning af kontoplan");
            throw new InvalidOperationException("Kunne ikke indlæse kontoplan", ex);
        }
    }

    private Task<List<Konto>> ParseKontoplanFromLines(string[] lines)
    {
        var kontoplan = new List<Konto>();
        var header = lines[0].Split(';');

        // Valider header format
        if (header.Length != 4 ||
            !string.Equals(header[0], "nr", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(header[1], "navn", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(header[2], "type", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(header[3], "moms", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Kontoplan-fil skal have header: nr;navn;type;moms");
        }

        // Parse konti
        for (int i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrEmpty(line))
                continue;

            var parts = line.Split(';');
            if (parts.Length != 4)
            {
                _logger.LogWarning("Ugyldig linje {LineNumber} i kontoplan: {Line}", i + 1, line);
                continue;
            }

            if (!int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int nr))
            {
                _logger.LogWarning("Ugyldigt kontonummer på linje {LineNumber}: {Number}", i + 1, parts[0]);
                continue;
            }

            var konto = new Konto
            {
                Nr = nr,
                Navn = parts[1].Trim(),
                Type = parts[2].Trim(),
                Moms = parts[3].Trim()
            };

            kontoplan.Add(konto);
        }

        return Task.FromResult(kontoplan);
    }

    private async Task ValidateKontoplan(List<Konto> kontoplan)
    {
        _logger.LogDebug("Validerer {Count} konti...", kontoplan.Count);

        // Valider alle konti
        var validationErrors = new List<string>();
        foreach (var konto in kontoplan)
        {
            var validationResult = await _kontoValidator.ValidateAsync(konto).ConfigureAwait(false);
            if (!validationResult.IsValid)
            {
                var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                validationErrors.Add($"Konto {konto.Nr}: {errors}");
            }
        }

        if (validationErrors.Count > 0)
        {
            var allErrors = string.Join("; ", validationErrors);
            throw new ValidationException($"Kontoplan indeholder ugyldige data: {allErrors}");
        }
    }

    /// <summary>
    /// Får alle konti i kontoplanen
    /// </summary>
    public IReadOnlyList<Konto> GetAllKonti()
    {
        return _kontoplan.AsReadOnly();
    }

    /// <summary>
    /// Finder en konto efter nummer
    /// </summary>
    public Konto? GetKonto(int nr)
    {
        return _kontoplan.FirstOrDefault(k => k.Nr == nr);
    }

    /// <summary>
    /// Finder konti efter type
    /// </summary>
    public IReadOnlyList<Konto> GetKontiByType(string type)
    {
        return _kontoplan.Where(k => string.Equals(k.Type, type, StringComparison.OrdinalIgnoreCase)).ToList().AsReadOnly();
    }

    /// <summary>
    /// Finder konti efter momstype
    /// </summary>
    public IReadOnlyList<Konto> GetKontiByMoms(string moms)
    {
        return _kontoplan.Where(k => string.Equals(k.Moms, moms, StringComparison.OrdinalIgnoreCase)).ToList().AsReadOnly();
    }
}