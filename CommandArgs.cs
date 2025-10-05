using CommandLine;

namespace SimpelBogfoering;

/// <summary>
/// Kommandolinje argumenter for applikationen
/// Defineret med CommandLineParser attributter
/// </summary>
public class CommandArgs
{
    /// <summary>
    /// Sti til input-mappen med datafiler (regnskab.json, kontoplan.csv, etc.)
    /// Eksempel: --input "C:\data" eller -i "data"
    /// </summary>
    [Option('i', "input", Required = true, HelpText = "Path to the input directory containing data files (regnskab.json, kontoplan.csv, etc.).")]
    public string Input { get; set; } = string.Empty;

    /// <summary>
    /// Navn på regnskabsfilen (CSV format)
    /// Standard: regnskab.csv
    /// Eksempel: --regnskab "min-regnskab.csv"
    /// </summary>
    [Option("regnskab", Required = false, Default = "regnskab.csv", HelpText = "Name of the regnskab file (CSV format). Default: regnskab.csv")]
    public string RegnskabFileName { get; set; } = "regnskab.csv";

    /// <summary>
    /// Navn på kontoplan filen (CSV format)
    /// Standard: kontoplan.csv
    /// Eksempel: --kontoplan "min-kontoplan.csv"
    /// </summary>
    [Option("kontoplan", Required = false, Default = "kontoplan.csv", HelpText = "Name of the kontoplan file (CSV format). Default: kontoplan.csv")]
    public string KontoplanFileName { get; set; } = "kontoplan.csv";

    /// <summary>
    /// Aktiverer detaljeret logging og output
    /// Eksempel: --verbose eller -v
    /// </summary>
    [Option('v', "verbose", Required = false, HelpText = "Enable verbose output and detailed logging.")]
    public bool Verbose { get; set; } = false;
}