using System.Text.RegularExpressions;

namespace ConecsIA.Api.Observabilidad;

/// <summary>
/// Enmascara datos sensibles en texto de logs (ADR-018): contraseñas, tokens, cabeceras de
/// autorización, correos, teléfonos y números de documento. Es una red de seguridad: el código
/// no debe registrar estos datos en primer lugar.
/// </summary>
public static partial class RedactorDatosSensibles
{
    public const string Mascara = "***";

    public static string Redactar(string? texto)
    {
        if (string.IsNullOrEmpty(texto)) return texto ?? string.Empty;

        // Bearer primero: si no, "Authorization: Bearer x" enmascararía solo la palabra Bearer.
        var resultado = Bearer().Replace(texto, $"Bearer {Mascara}");
        resultado = ClaveValorSensible().Replace(resultado, m => $"{m.Groups["clave"].Value}{m.Groups["sep"].Value}{Mascara}");
        resultado = Correo().Replace(resultado, Mascara);
        // También enmascara IPv4: un documento con puntos (1.023.456.789) tiene la misma forma.
        resultado = SecuenciaNumerica().Replace(resultado, Mascara);
        return resultado;
    }

    // password=..., "token": "...", qr_token: ..., documento=..., telefono=...
    [GeneratedRegex(
        """(?<clave>["']?(?:password|contrase[ñn]a|pwd|secret|token|qr[_-]?token|api[_-]?key|authorization|documento|numero[_-]?documento|telefono|tel[eé]fono|cookie)["']?)(?<sep>\s*[:=]\s*)(?:"[^"]*"|'[^']*'|[^\s,;&}]+)""",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ClaveValorSensible();

    [GeneratedRegex(@"Bearer\s+[A-Za-z0-9\-._~+/]+=*", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex Bearer();

    [GeneratedRegex(@"[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}", RegexOptions.CultureInvariant)]
    private static partial Regex Correo();

    // Documentos y teléfonos: 6 o más dígitos seguidos (permite separadores de miles y espacios).
    [GeneratedRegex(@"(?<![\w-])\+?\d(?:[\d .-]{4,}\d)(?![\w-])", RegexOptions.CultureInvariant)]
    private static partial Regex SecuenciaNumerica();
}
