using CommandLine;

namespace SimpelBogfoering;

/// <summary>
/// Kommandolinje argumenter for applikationen
/// Defineret med CommandLineParser attributter
/// </summary>
public class CommandArgs
{
    /// <summary>
    /// Aktiverer detaljeret logging og output
    /// Eksempel: --verbose eller -v
    /// </summary>
    [Option('v', "verbose", Required = false, HelpText = "Enable verbose output and detailed logging.")]
    public bool Verbose { get; set; } = false;
}