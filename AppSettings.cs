using System.ComponentModel.DataAnnotations;

namespace SimpelBogfoering;

/// <summary>
/// Klasse for applikationens konfiguration
/// Bindes til "AppSettings" sektionen i appsettings.json
/// Kan overskrives via miljøvariabler med prefix "AppSettings__"
/// </summary>
public class AppSettings
{
    /// <summary>
    /// Applikationens titel - bruges til logging og brugergrænsefladen
    /// Kan sættes via miljøvariabel: AppSettings__ApplicationTitle
    /// </summary>
    [Required]
    public string ApplicationTitle { get; set; } = string.Empty;
}