namespace SimpelBogfoering.Models;

/// <summary>
/// Model for en bogføringspostering
/// </summary>
public class Postering
{
    /// <summary>
    /// Dato for posteringen
    /// </summary>
    public DateTime Dato { get; set; }

    /// <summary>
    /// Kontonummer posteringen skal bogføres på
    /// </summary>
    public int Konto { get; set; }

    /// <summary>
    /// Tekst/beskrivelse af posteringen
    /// </summary>
    public string Tekst { get; set; } = string.Empty;

    /// <summary>
    /// Beløb (positiv = debet, negativ = kredit)
    /// </summary>
    public decimal Beløb { get; set; }
}