using System.Text.RegularExpressions;

namespace ApiConecsia.Services;

/// <summary>
/// Normalización de identificadores de dispositivos. Provisional hasta resolver D-17: recortar, pasar a mayúsculas y
/// reducir espacios internos a uno; el identificador es único por organización.
/// </summary>
public static class ReglasDispositivo
{
    public static string NormalizarIdentificador(string? codigo) =>
        Regex.Replace((codigo ?? "").Trim(), @"\s+", " ").ToUpperInvariant();

    /// <summary>Serial para búsquedas: sin espacios ni guiones, en mayúsculas.</summary>
    public static string NormalizarSerial(string serial) => Regex.Replace(serial, @"[\s\-]", "").ToUpperInvariant();
}
