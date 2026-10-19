using System.Globalization;
using System.Security.Claims;
using ConecsIA.Modules.Identidad.Application;
using ConecsIA.SharedKernel.Seguridad;
using ConecsIA.SharedKernel.Tiempo;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace ConecsIA.Api.Seguridad;

/// <summary>
/// Sesión con cookie de ASP.NET Core Identity (ADR-006, opción A): <c>HttpOnly</c>, <c>Secure</c> y
/// <c>SameSite=Strict</c>; 30 min de inactividad deslizante y 8 h como máximo absoluto. En cada
/// petición se compara el sello de seguridad: inactivar al usuario o cambiar su contraseña invalida
/// la sesión en la siguiente petición (PRD §5.2.8).
/// </summary>
public static class AutenticacionCookie
{
    public const string Esquema = "Sesion";
    public const string NombreCookie = "conecsia.sesion";
    public const string ClaimSello = "conecsia:sello";
    public const string ClaimEmitida = "conecsia:emitida";

    public static void Configurar(CookieAuthenticationOptions opciones, IHostEnvironment entorno)
    {
        opciones.Cookie.Name = NombreCookie;
        opciones.Cookie.HttpOnly = true;
        opciones.Cookie.SecurePolicy = PoliticaSecure(entorno);
        opciones.Cookie.SameSite = SameSiteMode.Strict;
        opciones.Cookie.IsEssential = true;
        opciones.ExpireTimeSpan = PoliticaAutenticacion.CierrePorInactividad;
        opciones.SlidingExpiration = true;

        // Una API no redirige a pantallas de inicio de sesión: responde 401/403.
        opciones.Events.OnRedirectToLogin = c => Responder(c.Response, StatusCodes.Status401Unauthorized);
        opciones.Events.OnRedirectToAccessDenied = c => Responder(c.Response, StatusCodes.Status403Forbidden);
        opciones.Events.OnValidatePrincipal = ValidarAsync;
    }

    /// <summary>
    /// <c>Secure</c> siempre, salvo en Development: el entorno local corre por HTTP detrás de nginx y
    /// el antiforgery rechaza una cookie Secure sobre HTTP. En los demás entornos el TLS termina en el
    /// proxy, que debe reenviar el esquema original (X-Forwarded-Proto; ver D-21).
    /// </summary>
    public static CookieSecurePolicy PoliticaSecure(IHostEnvironment entorno) =>
        entorno.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;

    public static ClaimsPrincipal CrearPrincipal(Guid usuarioId, Guid sesionId, string sello, DateTimeOffset emitida) =>
        new(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, usuarioId.ToString()),
                new Claim(ISesionUsuario.ClaimSesion, sesionId.ToString()),
                new Claim(ClaimSello, sello),
                new Claim(ClaimEmitida, emitida.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)),
            ],
            Esquema));

    private static Task Responder(HttpResponse respuesta, int estado)
    {
        respuesta.StatusCode = estado;
        return Task.CompletedTask;
    }

    private static async Task ValidarAsync(CookieValidatePrincipalContext contexto)
    {
        var servicios = contexto.HttpContext.RequestServices;
        var ahora = servicios.GetRequiredService<IClock>().UtcNow;
        var principal = contexto.Principal!;

        var vigente =
            Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var usuarioId)
            && long.TryParse(principal.FindFirstValue(ClaimEmitida), CultureInfo.InvariantCulture, out var emitida)
            && DateTimeOffset.FromUnixTimeSeconds(emitida) + PoliticaAutenticacion.DuracionMaximaSesion > ahora
            && Guid.TryParse(principal.FindFirstValue(ISesionUsuario.ClaimSesion), out var sesionId)
            && await VigenteEnServidorAsync(
                servicios, usuarioId, sesionId, principal.FindFirstValue(ClaimSello), ahora, contexto.HttpContext.RequestAborted);

        if (!vigente)
        {
            contexto.RejectPrincipal();
            await contexto.HttpContext.SignOutAsync(Esquema);
        }
    }

    /// <summary>Usuario activo, sello sin cambios y sesión registrada, propia, no revocada ni vencida.</summary>
    private static async Task<bool> VigenteEnServidorAsync(
        IServiceProvider servicios, Guid usuarioId, Guid sesionId, string? sello, DateTimeOffset ahora, CancellationToken cancelacion)
    {
        var repositorio = servicios.GetRequiredService<IRepositorioIdentidad>();
        var usuario = await repositorio.ObtenerUsuarioAsync(usuarioId, cancelacion);
        if (usuario is not { Activo: true } || !string.Equals(usuario.SelloSeguridad, sello, StringComparison.Ordinal))
        {
            return false;
        }

        var sesion = await repositorio.ObtenerSesionAsync(sesionId, cancelacion);
        return sesion is not null && sesion.UsuarioId == usuarioId && sesion.EstaVigente(ahora);
    }
}

/// <summary>Implementación de <see cref="ISesionUsuario"/> sobre la cookie de sesión.</summary>
internal sealed class SesionCookie(IHttpContextAccessor accesor, IClock reloj) : ISesionUsuario
{
    public Task IniciarAsync(Guid usuarioId, Guid sesionId, string selloSeguridad) =>
        Http.SignInAsync(
            AutenticacionCookie.Esquema,
            AutenticacionCookie.CrearPrincipal(usuarioId, sesionId, selloSeguridad, reloj.UtcNow),
            new AuthenticationProperties { IsPersistent = false, AllowRefresh = true });

    public Task CerrarAsync() => Http.SignOutAsync(AutenticacionCookie.Esquema);

    private HttpContext Http => accesor.HttpContext ?? throw new InvalidOperationException("Sin petición HTTP en curso.");
}
