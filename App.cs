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
    private readonly DemoService _demoService;
    private readonly RegnskabService _regnskabService;
    private readonly KontoplanService _kontoplanService;
    private readonly CommandArgs _commandArgs;

    public App(ILogger<App> logger, IOptions<AppSettings> appSettings, DemoService demoService, RegnskabService regnskabService, KontoplanService kontoplanService, CommandArgs commandArgs)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _appSettings = appSettings.Value ?? throw new ArgumentNullException(nameof(appSettings));
        _demoService = demoService ?? throw new ArgumentNullException(nameof(demoService));
        _regnskabService = regnskabService ?? throw new ArgumentNullException(nameof(regnskabService));
        _kontoplanService = kontoplanService ?? throw new ArgumentNullException(nameof(kontoplanService));
        _commandArgs = commandArgs ?? throw new ArgumentNullException(nameof(commandArgs));
    }

    /// <summary>
    /// Hovedindgangspunkt for applikationslogikken
    /// Håndterer kommandolinje argumenter og kører de relevante operationer
    /// </summary>
    /// <param name="args">Parsede kommandolinje argumenter</param>
    /// <returns>Exit code for applikationen (0 = success, 1 = error)</returns>
    public async Task<int> RunAsync(CommandArgs args)
    {
        try
        {
            _logger.LogInformation("Starting {ApplicationTitle}", _appSettings.ApplicationTitle);

            if (args.Verbose)
            {
                _logger.LogInformation("Verbose mode enabled");
                _logger.LogDebug("Application settings loaded: {ApplicationTitle}", _appSettings.ApplicationTitle);
            }

            // Vis regnskabs- og kontoplanoplysninger
            _logger.LogInformation("Regnskabsdata indlæst: {RegnskabInfo}", _regnskabService.GetRegnskabInfo());
            _logger.LogInformation("Kontoplan indlæst: {AntalKonti} konti", _kontoplanService.GetAllKonti().Count);

            // Forbered output mappe
            PrepareOutputDirectory();

            if (args.Verbose)
            {
                var regnskab = _regnskabService.Regnskab;
                _logger.LogDebug("Regnskabsdetaljer: Navn={RegnskabsNavn}, Periode={PeriodeFra:yyyy-MM-dd} til {PeriodeTil:yyyy-MM-dd}",
                    regnskab.RegnskabsNavn, regnskab.PeriodeFra, regnskab.PeriodeTil);
                _logger.LogDebug("Momskonti: Tilgodehavende={KontoTilgodehavende}, Skyldig={KontoSkyldig}",
                    regnskab.KontoTilgodehavendeMoms, regnskab.KontoSkyldigMoms);

                _logger.LogDebug("Kontoplan oversigt:");
                foreach (var konto in _kontoplanService.GetAllKonti())
                {
                    _logger.LogDebug("  {Konto}", konto);
                }
            }

            // Kør demo operationer
            _logger.LogDebug("Executing demo service operations...");

            var result = await _demoService.PerformDemoOperationAsync(args.Verbose).ConfigureAwait(false);
            _logger.LogInformation("Demo service result: {Result}", result);

            // Demonstrer error handling
            _demoService.DemonstrateErrorHandling();

            if (args.Verbose)
            {
                _logger.LogInformation("All operations completed successfully");
            }

            _logger.LogInformation("{ApplicationTitle} finished successfully", _appSettings.ApplicationTitle);
            return 0; // Success exit code
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "A critical error occurred while running {ApplicationTitle}", _appSettings.ApplicationTitle);
            return 1; // Error exit code
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