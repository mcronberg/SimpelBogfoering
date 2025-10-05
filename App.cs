using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SimpelBogfoering.Services;

namespace SimpelBogfoering;

/// <summary>
/// Hovedapplikationsklasse der indeholder den primære forretningslogik
/// Demonstrerer dependency injection pattern og separation of concerns
/// </summary>
public class App
{
    private readonly ILogger<App> _logger;
    private readonly AppSettings _appSettings;
    private readonly RegnskabService _regnskabService;
    private readonly KontoplanService _kontoplanService;
    private readonly PosteringService _posteringService;
    private readonly CommandArgs _commandArgs;

    public App(ILogger<App> logger, IOptions<AppSettings> appSettings, RegnskabService regnskabService, KontoplanService kontoplanService, PosteringService posteringService, CommandArgs commandArgs)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _appSettings = appSettings.Value ?? throw new ArgumentNullException(nameof(appSettings));
        _regnskabService = regnskabService ?? throw new ArgumentNullException(nameof(regnskabService));
        _kontoplanService = kontoplanService ?? throw new ArgumentNullException(nameof(kontoplanService));
        _posteringService = posteringService ?? throw new ArgumentNullException(nameof(posteringService));
        _commandArgs = commandArgs ?? throw new ArgumentNullException(nameof(commandArgs));
    }

    /// <summary>
    /// Hovedindgangspunkt for applikationslogikken
    /// Håndterer kommandolinje argumenter og kører de relevante operationer
    /// </summary>
    /// <param name="args">Parsede kommandolinje argumenter</param>
    /// <returns>Exit code for applikationen (0 = success, 1 = error)</returns>
    public Task<int> RunAsync(CommandArgs args)
    {
        try
        {
            _logger.LogInformation("Starting {ApplicationTitle}", _appSettings.ApplicationTitle);

            if (args.Verbose)
            {
                _logger.LogInformation("Verbose mode enabled");
                _logger.LogDebug("Application settings loaded: {ApplicationTitle}", _appSettings.ApplicationTitle);
            }

            // Vis oversigt over indlæste data
            ShowDataOverview();

            // Forbered output mappe
            PrepareOutputDirectory();

            // Vis detaljerede informationer i verbose mode
            if (args.Verbose)
            {
                ShowDetailedInformation();
            }

            _logger.LogInformation("{ApplicationTitle} finished successfully", _appSettings.ApplicationTitle);
            return Task.FromResult(0); // Success exit code
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "A critical error occurred while running {ApplicationTitle}", _appSettings.ApplicationTitle);
            return Task.FromResult(1); // Error exit code
        }
    }

    /// <summary>
    /// Viser oversigt over indlæste data
    /// </summary>
    private void ShowDataOverview()
    {
        _logger.LogInformation("Regnskabsdata indlæst: {RegnskabInfo}", _regnskabService.GetRegnskabInfo());
        _logger.LogInformation("Kontoplan indlæst: {AntalKonti} konti", _kontoplanService.GetAllKonti().Count);
        _logger.LogInformation("Posteringer indlæst: {AntalPosteringer} posteringer", _posteringService.Posteringer.Count);
    }

    /// <summary>
    /// Viser detaljerede informationer i verbose mode
    /// </summary>
    private void ShowDetailedInformation()
    {
        ShowRegnskabDetails();
        ShowKontoplanDetails();
        ShowPosteringerDetails();
    }

    /// <summary>
    /// Viser regnskabsdetaljer
    /// </summary>
    private void ShowRegnskabDetails()
    {
        var regnskab = _regnskabService.Regnskab;
        _logger.LogDebug("Regnskabsdetaljer: Navn={RegnskabsNavn}, Periode={PeriodeFra:yyyy-MM-dd} til {PeriodeTil:yyyy-MM-dd}",
            regnskab.RegnskabsNavn, regnskab.PeriodeFra, regnskab.PeriodeTil);
        _logger.LogDebug("Momskonti: Tilgodehavende={KontoTilgodehavende}, Skyldig={KontoSkyldig}",
            regnskab.KontoTilgodehavendeMoms, regnskab.KontoSkyldigMoms);
        _logger.LogDebug("Momsprocent: {MomsProcent:P2} ({MomsProcentDecimal:F2})",
            regnskab.MomsProcent, regnskab.MomsProcent);
    }

    /// <summary>
    /// Viser kontoplandetaljer
    /// </summary>
    private void ShowKontoplanDetails()
    {
        _logger.LogDebug("Kontoplan oversigt:");
        foreach (var konto in _kontoplanService.GetAllKonti())
        {
            _logger.LogDebug("  {Konto}", konto);
        }
    }

    /// <summary>
    /// Viser posteringsdetaljer
    /// </summary>
    private void ShowPosteringerDetails()
    {
        if (!_posteringService.Posteringer.Any())
        {
            _logger.LogDebug("Ingen posteringer fundet");
            return;
        }

        _logger.LogDebug("Posteringer oversigt:");

        // Gruppér posteringer pr. CSV fil
        var posteringerPrFil = _posteringService.Posteringer
            .GroupBy(p => p.CsvFil, StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase);

        foreach (var filGruppe in posteringerPrFil)
        {
            var filBalance = filGruppe.Sum(p => p.Beløb);
            _logger.LogDebug("  Fil: {FilNavn} - {AntalPosteringer} posteringer, balance: {Balance:F2} kr",
                filGruppe.Key, filGruppe.Count(), filBalance);

            foreach (var postering in filGruppe.OrderBy(p => p.Dato).ThenBy(p => p.Bilagsnummer))
            {
                _logger.LogDebug("    {Postering}", postering);
            }
        }

        // Vis konti med posteringer
        var kontiMedPosteringer = _posteringService.Posteringer
            .GroupBy(p => p.Konto)
            .OrderBy(g => g.Key);

        _logger.LogDebug("Konti med posteringer:");
        foreach (var kontoGruppe in kontiMedPosteringer)
        {
            var konto = _kontoplanService.GetKonto(kontoGruppe.Key);
            var saldo = _posteringService.GetSaldoForKonto(kontoGruppe.Key);
            _logger.LogDebug("  Konto {KontoNr} ({KontoNavn}): {AntalPosteringer} posteringer, saldo: {Saldo:F2} kr",
                kontoGruppe.Key, konto?.Navn ?? "Ukendt", kontoGruppe.Count(), saldo);
        }
    }

    /// <summary>
    /// Forbereder output mappen ved at slette den hvis den findes og oprette en ny
    /// </summary>
    private void PrepareOutputDirectory()
    {
        var outputPath = Path.Combine(_commandArgs.Input, "out");

        try
        {
            // Slet output mappe hvis den findes
            if (Directory.Exists(outputPath))
            {
                _logger.LogDebug("Sletter eksisterende output mappe: {OutputPath}", outputPath);
                Directory.Delete(outputPath, recursive: true);
            }

            // Opret ny output mappe
            Directory.CreateDirectory(outputPath);
            _logger.LogInformation("Output mappe forberedt: {OutputPath}", outputPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Kunne ikke forberyde output mappe: {OutputPath}", outputPath);
            throw new InvalidOperationException($"Fejl ved forberedelse af output mappe: {outputPath}", ex);
        }
    }
}