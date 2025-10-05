using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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

    public App(ILogger<App> logger, IOptions<AppSettings> appSettings, DemoService demoService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _appSettings = appSettings.Value ?? throw new ArgumentNullException(nameof(appSettings));
        _demoService = demoService ?? throw new ArgumentNullException(nameof(demoService));
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
}