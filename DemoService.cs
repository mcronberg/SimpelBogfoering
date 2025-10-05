using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SimpelBogfoering;

/// <summary>
/// Demo service der viser brug af dependency injection
/// Demonstrerer hvordan man injicerer og bruger ILogger og konfiguration
/// </summary>
public class DemoService
{
    private readonly ILogger<DemoService> _logger;
    private readonly AppSettings _appSettings;

    public DemoService(ILogger<DemoService> logger, IOptions<AppSettings> appSettings)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _appSettings = appSettings.Value ?? throw new ArgumentNullException(nameof(appSettings));
    }

    /// <summary>
    /// Demonstrerer en asynkron operation med logging
    /// Viser hvordan man bruger injicerede dependencies
    /// </summary>
    /// <param name="isVerbose">Om detaljeret output skal vises</param>
    public async Task<string> PerformDemoOperationAsync(bool isVerbose)
    {
        _logger.LogInformation("Starting demo operation for application: {ApplicationTitle}", _appSettings.ApplicationTitle);

        if (isVerbose)
        {
            _logger.LogDebug("Verbose mode activated - showing detailed information");
            _logger.LogInformation("Current configuration: ApplicationTitle = {ApplicationTitle}", _appSettings.ApplicationTitle);
        }

        // Simuler noget asynkront arbejde
        _logger.LogDebug("Performing simulated async work...");
        await Task.Delay(1000).ConfigureAwait(false); // Simulerer 1 sekunds arbejde

        var result = $"Demo operation completed successfully for {_appSettings.ApplicationTitle}";
        _logger.LogInformation("Demo operation finished: {Result}", result);

        return result;
    }

    /// <summary>
    /// Demonstrerer error handling og logging
    /// </summary>
    public void DemonstrateErrorHandling()
    {
        try
        {
            _logger.LogDebug("Demonstrating error handling...");

            // Simuler en potentiel fejl
            var random = new Random();
            if (random.Next(1, 10) > 7) // 30% chance of "error"
            {
                throw new InvalidOperationException("This is a simulated error for demonstration purposes");
            }

            _logger.LogInformation("Error handling demonstration completed without errors");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during error handling demonstration");
            // I en rigtig applikation ville man håndtere fejlen passende
            // Her logger vi bare og fortsætter
        }
    }
}