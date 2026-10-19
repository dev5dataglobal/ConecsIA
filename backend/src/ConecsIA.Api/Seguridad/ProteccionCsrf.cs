using ConecsIA.Api.Errores;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConecsIA.Api.Seguridad;

/// <summary>
/// Anti-CSRF para la sesión con cookie (ADR-006, opción A): toda petición que modifica estado y viaja
/// con la cookie de sesión debe traer el token de <c>GET /api/v1/auth/csrf</c> en la cabecera
/// <see cref="Cabecera"/>. El token se liga al usuario: la SPA lo pide de nuevo tras iniciar sesión.
/// Las peticiones sin cookie de sesión no la necesitan (no hay credencial ambiental que abusar).
/// </summary>
public sealed class MiddlewareCsrf(RequestDelegate siguiente)
{
    public const string Cabecera = "X-CSRF-TOKEN";
    public const string NombreCookie = "conecsia.csrf";
    public const string CodigoCsrfInvalido = "CSRF_INVALIDO";

    public static void Configurar(AntiforgeryOptions opciones, IHostEnvironment entorno)
    {
        opciones.HeaderName = Cabecera;
        opciones.Cookie.Name = NombreCookie;
        opciones.Cookie.HttpOnly = true;
        opciones.Cookie.SecurePolicy = AutenticacionCookie.PoliticaSecure(entorno);
        opciones.Cookie.SameSite = SameSiteMode.Strict;
    }

    public async Task InvokeAsync(HttpContext http, IAntiforgery antiforgery, IProblemDetailsService problemas)
    {
        if (!HttpMethods.IsGet(http.Request.Method)
            && !HttpMethods.IsHead(http.Request.Method)
            && !HttpMethods.IsOptions(http.Request.Method)
            && http.Request.Cookies.ContainsKey(AutenticacionCookie.NombreCookie)
            && !await antiforgery.IsRequestValidAsync(http))
        {
            http.Response.StatusCode = StatusCodes.Status403Forbidden;
            var problema = new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Solicitud no válida",
                Detail = "Falta el token anti-CSRF o no es válido. Pide uno nuevo y reintenta.",
            };
            problema.Extensions[DetallesProblema.ClaveCodigo] = CodigoCsrfInvalido;
            await problemas.WriteAsync(new ProblemDetailsContext { HttpContext = http, ProblemDetails = problema });
            return;
        }

        await siguiente(http);
    }
}

/// <summary>Entrega el token anti-CSRF ligado a la sesión actual (o anónima, antes de iniciarla).</summary>
[ApiController]
[AllowAnonymous]
[Route("api/v1/auth/csrf")]
public sealed class CsrfController(IAntiforgery antiforgery) : ControllerBase
{
    public sealed record TokenCsrf(string Token, string Cabecera);

    [HttpGet]
    [ProducesResponseType<TokenCsrf>(StatusCodes.Status200OK)]
    public TokenCsrf Obtener() =>
        new(antiforgery.GetAndStoreTokens(HttpContext).RequestToken!, MiddlewareCsrf.Cabecera);
}
