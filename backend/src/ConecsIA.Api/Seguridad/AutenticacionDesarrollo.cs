using System.Security.Claims;
using System.Text.Encodings.Web;
using ConecsIA.SharedKernel.Seguridad;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace ConecsIA.Api.Seguridad;

/// <summary>
/// Esquema de autenticación SOLO para desarrollo y pruebas: la identidad llega en la cabecera
/// <c>X-Usuario-Desarrollo</c>. Sustituye a HU-006 mientras D-03 (proveedor de identidad) está abierta.
/// Deshabilitado, no autentica a nadie; habilitarlo en producción detiene el arranque.
/// </summary>
public sealed class AutenticacionDesarrollo(
    IOptionsMonitor<AuthenticationSchemeOptions> opciones,
    ILoggerFactory logger,
    UrlEncoder codificador,
    IConfiguration configuracion) : AuthenticationHandler<AuthenticationSchemeOptions>(opciones, logger, codificador)
{
    public const string Esquema = "Desarrollo";
    public const string ClaveHabilitada = "Autenticacion:Desarrollo:Habilitada";

    public static bool EstaHabilitada(IConfiguration configuracion) => configuracion.GetValue<bool>(ClaveHabilitada);

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!EstaHabilitada(configuracion)
            || !Request.Headers.TryGetValue(CabecerasSeguridad.UsuarioDesarrollo, out var valor))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        if (!Guid.TryParse(valor.ToString(), out var usuarioId))
        {
            return Task.FromResult(AuthenticateResult.Fail("Identificador de usuario inválido."));
        }

        // Solo afirma quién dice ser; si existe y está activo lo verifica el middleware de contexto.
        var identidad = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, usuarioId.ToString())], Esquema);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identidad), Esquema)));
    }

    public static void ValidarEntorno(IConfiguration configuracion, IHostEnvironment entorno)
    {
        if (EstaHabilitada(configuracion) && !(entorno.IsDevelopment() || entorno.IsEnvironment("Testing")))
        {
            throw new InvalidOperationException(
                $"La autenticación de desarrollo no está permitida en {entorno.EnvironmentName} (D-03, HU-006).");
        }
    }
}
