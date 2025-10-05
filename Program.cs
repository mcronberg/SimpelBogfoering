using CommandLine;
using DotNetEnv;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using SimpelBogfoering;
using System.Linq;

// Indlæs .env fil først - dette gør det muligt at overskrive appsettings.json værdier
Env.Load();

// Byg konfiguration med prioriteret rækkefølge: miljøvariabler > appsettings.json
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables() // Miljøvariabler har højeste prioritet
    .Build();

// Konfigurer Serilog baseret på appsettings.json konfiguration
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .CreateLogger();

try
{
    Log.Information("Starting application startup");

    // Parse kommandolinje argumenter
    var parseResult = Parser.Default.ParseArguments<CommandArgs>(args);

    // Håndter parsing resultatet synkront først
    CommandArgs? commandArgs = null;
    var helpOrVersionRequested = false;
    var parsingFailed = false;

    parseResult.MapResult(
        (CommandArgs args) =>
        {
            commandArgs = args;
            return 0;
        },
        errors =>
        {
            // Håndter forskellige typer af kommandolinje "fejl"
            var errorList = errors.ToList();

            // Help og version requests er ikke rigtige fejl
            if (errorList.Any(e => e.Tag == CommandLine.ErrorType.HelpRequestedError ||
                                  e.Tag == CommandLine.ErrorType.VersionRequestedError))
            {
                helpOrVersionRequested = true;
                return 0;
            }

            // Rigtige kommandolinje parsing fejl
            Log.Error("Invalid command line arguments provided");
            foreach (var error in errorList)
            {
                Log.Error("Command line error: {ErrorType} - {ErrorMessage}", error.Tag, error);
            }
            parsingFailed = true;
            return 1;
        });

    // Bestem exit code baseret på parsing resultat
    int finalExitCode;

    if (helpOrVersionRequested)
    {
        // Help/version blev vist - success
        finalExitCode = 0;
    }
    else if (parsingFailed)
    {
        // Parsing fejlede
        finalExitCode = 1;
    }
    else if (commandArgs != null)
    {
        // Parsing lykkedes, kør applikationen asynkront
        finalExitCode = await RunApplicationAsync(configuration, commandArgs).ConfigureAwait(false);
    }
    else
    {
        // Uventet tilstand
        Log.Error("Unexpected state in command line parsing");
        finalExitCode = 1;
    }

    Log.Information("Application shutting down with exit code: {ExitCode}", finalExitCode);
    return finalExitCode;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly during startup");
    return 1;
}
finally
{
    // Sikrer at alle log beskeder bliver skrevet før applikationen lukkes
    await Log.CloseAndFlushAsync().ConfigureAwait(false);
}

/// <summary>
/// Kør applikationen asynkront med de parsed kommandolinje argumenter
/// </summary>
static async Task<int> RunApplicationAsync(IConfiguration configuration, CommandArgs commandArgs)
{
    try
    {
        // Opret host med dependency injection
        var host = CreateHost(configuration, commandArgs);

        // Kør applikationen med en service scope
        using var scope = host.Services.CreateScope();
        var app = scope.ServiceProvider.GetRequiredService<App>();
        return await app.RunAsync(commandArgs).ConfigureAwait(false);
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "Application terminated unexpectedly during execution");
        return 1;
    }
}

/// <summary>
/// Opret og konfigurer dependency injection containeren
/// Registrerer alle services og konfiguration
/// </summary>
static IHost CreateHost(IConfiguration configuration, CommandArgs commandArgs)
{
    var builder = Host.CreateDefaultBuilder()
        .UseSerilog() // Brug Serilog som logging provider
        .ConfigureServices((context, services) =>
        {
            // Registrer konfiguration
            services.Configure<AppSettings>(configuration.GetSection("AppSettings"));

            // Validér AppSettings konfiguration
            services.AddOptions<AppSettings>()
                .Bind(configuration.GetSection("AppSettings"))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            // Registrer application services
            services.AddScoped<App>();
            services.AddScoped<DemoService>();

            // Juster log niveau baseret på verbose flag
            if (commandArgs.Verbose)
            {
                services.AddLogging(logging =>
                {
                    logging.SetMinimumLevel(LogLevel.Debug);
                });
            }
        });

    return builder.Build();
}
