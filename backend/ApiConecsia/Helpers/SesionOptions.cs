namespace ApiConecsia.Helpers;

/// <summary>
/// Sección «Sesion». La vigencia máxima sigue la política aprobada en ADR-006 (D-03c: 8 h); «Recordarme» solo decide
/// si la cookie sobrevive al cierre del navegador dentro de esa vigencia. Ampliarla requiere resolver D-29.
/// </summary>
public sealed class SesionOptions
{
    public const string Cookie = "conecsia_sesion";
    public const string RutaCookie = "/api/auth";

    /// <summary>Vigencia absoluta de una sesión desde el inicio de sesión (con o sin «Recordarme»).</summary>
    public int HorasMaximasSesion { get; set; } = 8;

    /// <summary>Vigencia del enlace de restablecimiento de contraseña.</summary>
    public int MinutosRecuperacion { get; set; } = 30;

    /// <summary>URL pública de la SPA para construir el enlace de recuperación (p. ej. https://app.conecsia.co).</summary>
    public string UrlFrontend { get; set; } = string.Empty;
}
