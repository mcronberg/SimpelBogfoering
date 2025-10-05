namespace SimpelBogfoering.Models;

/// <summary>
/// Repræsenterer en konto i kontoplanen
/// </summary>
public class Konto
{
    /// <summary>
    /// Kontonummer (1-1.000.000)
    /// </summary>
    public int Nr { get; set; }

    /// <summary>
    /// Navnet på kontoen
    /// </summary>
    public string Navn { get; set; } = string.Empty;

    /// <summary>
    /// Kontotype: drift, status eller sum:fra-til
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Momstype: INDG (indgående), UDG (udgående) eller INGEN
    /// </summary>
    public string Moms { get; set; } = string.Empty;

    public override string ToString()
    {
        return $"{Nr}: {Navn} ({Type}, {Moms})";
    }
}