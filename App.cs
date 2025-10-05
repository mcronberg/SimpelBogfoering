using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SimpelBogfoering.Services;
using System.Globalization;
using System.Text;

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

            // Generer rapporter
            GenerateReports();

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

    /// <summary>
    /// Genererer rapporter og gemmer dem i output mappen
    /// </summary>
    private void GenerateReports()
    {
        _logger.LogInformation("Genererer rapporter");

        try
        {
            GenerateKontoplanReport();
            GenerateBalanceReport();
            GenerateKontokortReports();
            GeneratePosteringslisteReport();
            GenerateCSVReports();
            _logger.LogInformation("Rapporter genereret succesfuldt");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fejl ved generering af rapporter");
            throw;
        }
    }

    /// <summary>
    /// Genererer kontoplan rapport og gemmer som kontoplan.txt
    /// </summary>
    private void GenerateKontoplanReport()
    {
        var outputPath = Path.Combine(_commandArgs.Input, "out", "kontoplan.txt");
        var regnskab = _regnskabService.Regnskab;
        var alleKonti = _kontoplanService.GetAlleKonti().OrderBy(k => k.Nr).ToList();

        var content = new StringBuilder();

        // Header
        content.AppendLine($"Regnskab: {regnskab.RegnskabsNavn}");
        content.AppendLine("".PadRight(regnskab.RegnskabsNavn.Length + 10, '-'));
        content.AppendLine();

        // Drift konti
        var driftKonti = alleKonti.Where(k => string.Equals(k.Type, "drift", StringComparison.OrdinalIgnoreCase)).ToList();
        if (driftKonti.Any())
        {
            content.AppendLine("Drift");
            content.AppendLine();

            foreach (var konto in driftKonti)
            {
                var momsInfo = string.Equals(konto.Moms, "INGEN", StringComparison.OrdinalIgnoreCase)
                    ? ""
                    : $" ({konto.Moms})";
                content.AppendLine(CultureInfo.InvariantCulture, $"{konto.Nr,6}: {konto.Navn}{momsInfo}");
            }
            content.AppendLine();
        }

        // Status/Balance konti
        var statusKonti = alleKonti.Where(k => string.Equals(k.Type, "status", StringComparison.OrdinalIgnoreCase)).ToList();
        if (statusKonti.Any())
        {
            content.AppendLine("Status/Balance");
            content.AppendLine();

            foreach (var konto in statusKonti)
            {
                content.AppendLine(CultureInfo.InvariantCulture, $"{konto.Nr,6}: {konto.Navn}");
            }
            content.AppendLine();
        }

        // Sum konti (hvis der er nogen)
        var sumKonti = alleKonti.Where(k => k.Type.StartsWith("sum:", StringComparison.OrdinalIgnoreCase)).ToList();
        if (sumKonti.Any())
        {
            content.AppendLine("Sum konti");
            content.AppendLine();

            foreach (var konto in sumKonti)
            {
                content.AppendLine(CultureInfo.InvariantCulture, $"{konto.Nr,6}: {konto.Navn} ({konto.Type})");
            }
        }

        // Gem fil
        File.WriteAllText(outputPath, content.ToString(), System.Text.Encoding.UTF8);
        _logger.LogInformation("Kontoplan rapport gemt: {FilePath}", outputPath);
    }

    /// <summary>
    /// Genererer balance rapport med saldi og gemmer som balance.txt
    /// </summary>
    private void GenerateBalanceReport()
    {
        var outputPath = Path.Combine(_commandArgs.Input, "out", "balance.txt");
        var regnskab = _regnskabService.Regnskab;
        var alleKonti = _kontoplanService.GetAlleKonti().OrderBy(k => k.Nr).ToList();

        var content = new StringBuilder();

        // Header
        content.AppendLine($"Balance: {regnskab.RegnskabsNavn}");
        content.AppendLine("".PadRight(regnskab.RegnskabsNavn.Length + 9, '='));
        content.AppendLine(CultureInfo.InvariantCulture, $"Periode: {regnskab.PeriodeFra:dd-MM-yyyy} til {regnskab.PeriodeTil:dd-MM-yyyy}");
        content.AppendLine();

        // Drift konti
        var driftKonti = alleKonti.Where(k => string.Equals(k.Type, "drift", StringComparison.OrdinalIgnoreCase)).ToList();
        if (driftKonti.Any())
        {
            content.AppendLine("DRIFT / RESULTAT");
            content.AppendLine("".PadRight(50, '-'));
            content.AppendLine();

            foreach (var konto in driftKonti)
            {
                var saldo = _posteringService.GetSaldoForKonto(konto.Nr);
                var saldoText = $"{saldo:N2}";
                var kontoText = $"{konto.Nr}: {konto.Navn}";

                // Justér så der er god plads mellem konto navn og saldo (total 70 tegn)
                var spacing = Math.Max(1, 70 - kontoText.Length - saldoText.Length);
                content.AppendLine(CultureInfo.InvariantCulture, $"{kontoText}{new string(' ', spacing)}{saldoText}");
            }
            content.AppendLine();
        }

        // Status/Balance konti (ignore sum konti som ønsket)
        var statusKonti = alleKonti.Where(k => string.Equals(k.Type, "status", StringComparison.OrdinalIgnoreCase)).ToList();
        if (statusKonti.Any())
        {
            content.AppendLine("STATUS / BALANCE");
            content.AppendLine("".PadRight(50, '-'));
            content.AppendLine();

            foreach (var konto in statusKonti)
            {
                var saldo = _posteringService.GetSaldoForKonto(konto.Nr);
                var saldoText = $"{saldo:N2}";
                var kontoText = $"{konto.Nr}: {konto.Navn}";

                // Justér så der er god plads mellem konto navn og saldo (total 70 tegn)
                var spacing = Math.Max(1, 70 - kontoText.Length - saldoText.Length);
                content.AppendLine(CultureInfo.InvariantCulture, $"{kontoText}{new string(' ', spacing)}{saldoText}");
            }
            content.AppendLine();
        }

        // Gem fil
        File.WriteAllText(outputPath, content.ToString(), System.Text.Encoding.UTF8);
        _logger.LogInformation("Balance rapport gemt: {FilePath}", outputPath);
    }

    /// <summary>
    /// Genererer kontokort rapporter for alle konti med posteringer
    /// </summary>
    private void GenerateKontokortReports()
    {
        var outputPath = Path.Combine(_commandArgs.Input, "out", "kontokort.txt");
        var regnskab = _regnskabService.Regnskab;
        var alleKonti = _kontoplanService.GetAlleKonti();
        var allePosteringer = _posteringService.GetAllePosteringer();

        // Find alle konti som har posteringer
        var kontiMedPosteringer = alleKonti
            .Where(k => allePosteringer.Any(p => p.Konto == k.Nr))
            .OrderBy(k => k.Nr)
            .ToList();

        var content = new StringBuilder();

        // Hovedheader
        content.AppendLine(CultureInfo.InvariantCulture, $"Kontokort: {regnskab.RegnskabsNavn}");
        content.AppendLine("".PadRight($"Kontokort: {regnskab.RegnskabsNavn}".Length, '='));
        content.AppendLine(CultureInfo.InvariantCulture, $"Periode: {regnskab.PeriodeFra:dd-MM-yyyy} til {regnskab.PeriodeTil:dd-MM-yyyy}");
        content.AppendLine();

        foreach (var konto in kontiMedPosteringer)
        {
            GenerateKontokortForKonto(konto, allePosteringer, content);
            content.AppendLine(); // Ekstra linje mellem konti
        }

        // Gem fil
        File.WriteAllText(outputPath, content.ToString(), System.Text.Encoding.UTF8);
        _logger.LogInformation("Kontokort rapport gemt: {FilePath} for {AntalKonti} konti", outputPath, kontiMedPosteringer.Count);
    }

    /// <summary>
    /// Genererer kontokort for en specifik konto til StringBuilder
    /// </summary>
    private void GenerateKontokortForKonto(Models.Konto konto, IReadOnlyList<Models.Postering> allePosteringer, StringBuilder content)
    {
        // Find alle posteringer for denne konto, sorteret efter dato og bilagsnummer
        var kontoPosteringer = allePosteringer
            .Where(p => p.Konto == konto.Nr)
            .OrderBy(p => p.Dato)
            .ThenBy(p => p.Bilagsnummer)
            .ToList();

        // Konto header
        content.AppendLine(CultureInfo.InvariantCulture, $"Konto {konto.Nr}: {konto.Navn}");
        content.AppendLine("".PadRight($"Konto {konto.Nr}: {konto.Navn}".Length, '-'));
        content.AppendLine(CultureInfo.InvariantCulture, $"Type: {konto.Type.ToUpper(CultureInfo.InvariantCulture)} | Moms: {konto.Moms}");
        content.AppendLine();

        // Tabel header
        content.AppendLine("Dato       Bilag  Tekst                                          Beløb        Saldo");
        content.AppendLine("".PadRight(80, '-'));

        // Posteringer med løbende saldo
        decimal løbendeSaldo = 0;
        foreach (var postering in kontoPosteringer)
        {
            løbendeSaldo += postering.Beløb;

            var datoText = postering.Dato.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);
            var bilagText = postering.Bilagsnummer.ToString(CultureInfo.InvariantCulture).PadLeft(5);
            var tekstText = postering.Tekst.Length > 38 ? postering.Tekst.Substring(0, 35) + "..." : postering.Tekst;
            tekstText = tekstText.PadRight(38);
            var beløbText = $"{postering.Beløb:N2}".PadLeft(12);
            var saldoText = $"{løbendeSaldo:N2}".PadLeft(12);

            content.AppendLine(CultureInfo.InvariantCulture, $"{datoText} {bilagText}  {tekstText} {beløbText} {saldoText}");
        }

        // Footer med totaler
        content.AppendLine("".PadRight(80, '-'));
        var antalPosteringer = kontoPosteringer.Count;
        var slutsaldo = _posteringService.GetSaldoForKonto(konto.Nr);
        content.AppendLine(CultureInfo.InvariantCulture, $"Antal posteringer: {antalPosteringer}");
        content.AppendLine(CultureInfo.InvariantCulture, $"Slutsaldo: {slutsaldo:N2}");
    }

    /// <summary>
    /// Genererer posteringsliste rapport sorteret efter dato og bilag
    /// </summary>
    private void GeneratePosteringslisteReport()
    {
        var outputPath = Path.Combine(_commandArgs.Input, "out", "posteringsliste.txt");
        var regnskab = _regnskabService.Regnskab;
        var allePosteringer = _posteringService.GetAllePosteringer()
            .OrderBy(p => p.Dato)
            .ThenBy(p => p.Bilagsnummer)
            .ToList();

        var content = new StringBuilder();

        // Header
        content.AppendLine($"Posteringsliste: {regnskab.RegnskabsNavn}");
        content.AppendLine("".PadRight($"Posteringsliste: {regnskab.RegnskabsNavn}".Length, '='));
        content.AppendLine(CultureInfo.InvariantCulture, $"Periode: {regnskab.PeriodeFra:dd-MM-yyyy} til {regnskab.PeriodeTil:dd-MM-yyyy}");
        content.AppendLine();

        // Tabel header
        content.AppendLine("Dato       Bilag  Konto  Tekst                                    Beløb        Fil");
        content.AppendLine("".PadRight(90, '-'));

        // Alle posteringer
        foreach (var postering in allePosteringer)
        {
            var konto = _kontoplanService.GetKonto(postering.Konto);
            var kontoNavn = konto?.Navn ?? "Ukendt";

            var datoText = postering.Dato.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);
            var bilagText = postering.Bilagsnummer.ToString(CultureInfo.InvariantCulture).PadLeft(5);
            var kontoText = $"{postering.Konto}".PadLeft(5);
            var tekstText = postering.Tekst.Length > 36 ? postering.Tekst.Substring(0, 33) + "..." : postering.Tekst;
            tekstText = tekstText.PadRight(36);
            var beløbText = $"{postering.Beløb:N2}".PadLeft(12);
            var filText = postering.CsvFil ?? "Ukendt";

            content.AppendLine(CultureInfo.InvariantCulture, $"{datoText} {bilagText}  {kontoText}  {tekstText} {beløbText}  {filText}");
        }

        // Footer
        content.AppendLine("".PadRight(90, '-'));
        content.AppendLine(CultureInfo.InvariantCulture, $"Total antal posteringer: {allePosteringer.Count}");

        // Gem fil
        File.WriteAllText(outputPath, content.ToString(), System.Text.Encoding.UTF8);
        _logger.LogInformation("Posteringsliste rapport gemt: {FilePath}", outputPath);
    }

    /// <summary>
    /// Genererer CSV rapporter for balance og posteringsliste
    /// </summary>
    private void GenerateCSVReports()
    {
        GenerateBalanceCSV();
        GeneratePosteringslisteCSV();
    }

    /// <summary>
    /// Genererer balance som CSV fil
    /// </summary>
    private void GenerateBalanceCSV()
    {
        var outputPath = Path.Combine(_commandArgs.Input, "out", "balance.csv");
        var alleKonti = _kontoplanService.GetAlleKonti().OrderBy(k => k.Nr).ToList();

        var content = new StringBuilder();

        // CSV header
        content.AppendLine("Kontonummer;Kontonavn;Kontotype;Moms;Saldo");

        // Alle konti med saldi (kun drift og status, ikke sum)
        var relevantKonti = alleKonti.Where(k =>
            string.Equals(k.Type, "drift", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(k.Type, "status", StringComparison.OrdinalIgnoreCase)).ToList();

        foreach (var konto in relevantKonti)
        {
            var saldo = _posteringService.GetSaldoForKonto(konto.Nr);
            content.AppendLine(CultureInfo.InvariantCulture, $"{konto.Nr};{konto.Navn};{konto.Type};{konto.Moms};{saldo:F2}");
        }

        // Gem fil
        File.WriteAllText(outputPath, content.ToString(), System.Text.Encoding.UTF8);
        _logger.LogInformation("Balance CSV gemt: {FilePath}", outputPath);
    }

    /// <summary>
    /// Genererer posteringsliste som CSV fil
    /// </summary>
    private void GeneratePosteringslisteCSV()
    {
        var outputPath = Path.Combine(_commandArgs.Input, "out", "posteringsliste.csv");
        var allePosteringer = _posteringService.GetAllePosteringer()
            .OrderBy(p => p.Dato)
            .ThenBy(p => p.Bilagsnummer)
            .ToList();

        var content = new StringBuilder();

        // CSV header
        content.AppendLine("Dato;Bilagsnummer;Konto;Kontonavn;Tekst;Beløb;CsvFil");

        // Alle posteringer
        foreach (var postering in allePosteringer)
        {
            var konto = _kontoplanService.GetKonto(postering.Konto);
            var kontoNavn = konto?.Navn ?? "Ukendt";

            content.AppendLine(CultureInfo.InvariantCulture,
                $"{postering.Dato:dd-MM-yyyy};{postering.Bilagsnummer};{postering.Konto};{kontoNavn};{postering.Tekst};{postering.Beløb:F2};{postering.CsvFil ?? "Ukendt"}");
        }

        // Gem fil
        File.WriteAllText(outputPath, content.ToString(), System.Text.Encoding.UTF8);
        _logger.LogInformation("Posteringsliste CSV gemt: {FilePath}", outputPath);
    }
}