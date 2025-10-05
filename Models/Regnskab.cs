namespace SimpelBogfoering.Models;

/// <summary>
/// Model for regnskabsdata (regnskab.json)
/// Indeholder hovedoplysninger om regnskabsperioden
/// </summary>
public class Regnskab
{
    /// <summary>
    /// Firmaets/regnskabets navn
    /// </summary>
    public string RegnskabsNavn { get; set; } = string.Empty;

    /// <summary>
    /// Startdato for regnskabsperioden
    /// </summary>
    public DateTime PeriodeFra { get; set; }

    /// <summary>
    /// Slutdato for regnskabsperioden
    /// </summary>
    public DateTime PeriodeTil { get; set; }

    /// <summary>
    /// Konto for tilgodehavende moms (kontonummer som int)
    /// </summary>
    public int KontoTilgodehavendeMoms { get; set; }

    /// <summary>
    /// Konto for skyldig moms (kontonummer som int)
    /// </summary>
    public int KontoSkyldigMoms { get; set; }
}