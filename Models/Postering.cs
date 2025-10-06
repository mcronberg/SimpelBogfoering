namespace SimpelBogfoering.Models;

/// <summary>
/// Model for en bogføringspostering
/// </summary>
public class Postering
{
    /// <summary>
    /// Dato for posteringen (skal være inden for regnskabsåret)
    /// </summary>
    public DateTime Dato { get; set; }

    /// <summary>
    /// Bilagsnummer (1-1.000.000)
    /// </summary>
    public int Bilagsnummer { get; set; }

    /// <summary>
    /// Kontonummer posteringen skal bogføres på (1-1.000.000)
    /// </summary>
    public int Konto { get; set; }

    /// <summary>
    /// Tekst/beskrivelse af posteringen (mindst 3 tegn)
    /// </summary>
    public string Tekst { get; set; } = string.Empty;

    /// <summary>
    /// Beløb (positiv = debet, negativ = kredit)
    /// </summary>
    public decimal Beløb { get; set; }

    /// <summary>
    /// Navn på CSV-filen som posteringen kommer fra
    /// </summary>
    public string CsvFil { get; set; } = string.Empty;

    /// <summary>
    /// Modkonto - hvis udfyldt oprettes automatisk modpostering
    /// </summary>
    public int? Modkonto { get; set; }

    public override string ToString()
    {
        return $"{Dato:yyyy-MM-dd} Bilag:{Bilagsnummer} Konto:{Konto} {Beløb:F2} - {Tekst} ({CsvFil})";
    }
}