namespace ConecsIA.SharedKernel.Tiempo;

public static class ZonaHorariaIana
{
    /// <summary>
    /// Acepta solo identificadores IANA conocidos por la base de zonas del sistema (ICU/tzdata).
    /// Rechaza identificadores de Windows y desfases fijos. NodaTime (ADR-008) sigue en Propuesta.
    /// </summary>
    public static bool EsValida(string? zona) =>
        !string.IsNullOrWhiteSpace(zona)
        && zona == zona.Trim()
        && TimeZoneInfo.TryFindSystemTimeZoneById(zona, out _)
        && TimeZoneInfo.TryConvertIanaIdToWindowsId(zona, out _);
}
