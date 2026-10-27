using ConecsIA.Application.DTOs;
using ApiConecsia.Extensions;
using ApiConecsia.Helpers;
using ApiConecsia.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace ApiConecsia.Controllers;

/// <summary>
/// Inicio de sesión, renovación y recuperación de contraseña. El token de acceso (JWT corto) se entrega en el cuerpo
/// y la SPA lo mantiene solo en memoria; la sesión renovable viaja en una cookie HttpOnly limitada a /api/auth.
/// </summary>
[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    IUserService userService,
    RecuperacionContrasenaService recuperacion,
    IWebHostEnvironment entorno) : ControllerBase
{
    /// <summary>Cabecera obligatoria en renovar/cerrar: fuerza preflight CORS y evita peticiones cruzadas simples.</summary>
    public const string CabeceraSesion = "X-Conecsia-Sesion";

    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponseDto>> Login(
        [FromBody] LoginDto request,
        CancellationToken cancellationToken)
    {
        var response = await userService.AuthenticateAsync(request, cancellationToken);
        if (response.Response is not null)
        {
            EscribirCookie(response.Sesion);
            return Ok(response.Response);
        }

        var detail = response.FailureReason switch
        {
            AuthenticationFailureReason.UserNotRegistered => "El usuario ingresado no está registrado",
            AuthenticationFailureReason.InvalidPassword => "La contraseña ingresada es incorrecta",
            AuthenticationFailureReason.UserLocked => "El usuario está temporalmente bloqueado. Inténtalo más tarde",
            AuthenticationFailureReason.UserInactive => "El usuario está inactivo. Comunícate con el administrador de tu organización",
            _ => "No se pudo iniciar sesión"
        };

        return Unauthorized(new ProblemDetails
        {
            Type = "https://httpstatuses.com/401",
            Title = "No se pudo iniciar sesión",
            Status = StatusCodes.Status401Unauthorized,
            Detail = detail
        });
    }

    /// <summary>
    /// Emite un token de acceso nuevo a partir de la cookie de sesión. Los permisos se recalculan aquí:
    /// los cambios de perfil se reflejan en el token al renovar (el acceso por módulo ya se valida en cada petición).
    /// </summary>
    [AllowAnonymous]
    [HttpPost("renovar")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponseDto>> Renovar(CancellationToken cancellationToken)
    {
        if (!Request.Headers.ContainsKey(CabeceraSesion)) return SesionNoValida();
        var resultado = await userService.RenovarAsync(Request.Cookies[SesionOptions.Cookie], cancellationToken);
        if (resultado.Response is null)
        {
            BorrarCookie();
            return SesionNoValida();
        }
        EscribirCookie(resultado.Sesion);
        return Ok(resultado.Response);
    }

    [AllowAnonymous]
    [HttpPost("cerrar-sesion")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> CerrarSesion(CancellationToken cancellationToken)
    {
        if (!Request.Headers.ContainsKey(CabeceraSesion)) return SesionNoValida();
        await userService.CerrarSesionAsync(Request.Cookies[SesionOptions.Cookie], cancellationToken);
        BorrarCookie();
        return NoContent();
    }

    /// <summary>Responde 202 con el mismo mensaje exista o no la cuenta.</summary>
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitingExtensions.PoliticaRecuperacion)]
    [HttpPost("recuperar-contrasena")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> SolicitarRecuperacion([FromBody] SolicitarRecuperacionDto request, CancellationToken cancellationToken)
    {
        var resultado = await recuperacion.SolicitarAsync(request.Correo, cancellationToken);
        if (resultado == ResultadoRecuperacion.CorreoNoConfigurado)
        {
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "Recuperación no disponible",
                detail: "El envío de correos no está configurado. Comunícate con el administrador de tu organización.");
        }
        return Accepted(new { mensaje = "Si el correo está registrado, recibirás un enlace para restablecer tu contraseña." });
    }

    [AllowAnonymous]
    [EnableRateLimiting(RateLimitingExtensions.PoliticaRecuperacion)]
    [HttpPost("restablecer-contrasena")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Restablecer([FromBody] RestablecerContrasenaDto request, CancellationToken cancellationToken)
    {
        if (request.NuevaContrasena != request.ConfirmacionContrasena)
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Solicitud no válida", detail: "La confirmación no coincide con la nueva contraseña.");

        return await recuperacion.RestablecerAsync(request.Token, request.NuevaContrasena, cancellationToken) switch
        {
            ResultadoRecuperacion.Ok => NoContent(),
            ResultadoRecuperacion.ContrasenaInvalida => Problem(statusCode: StatusCodes.Status400BadRequest, title: "Solicitud no válida", detail: PoliticaContrasena.Mensaje),
            _ => Problem(statusCode: StatusCodes.Status400BadRequest, title: "Enlace no válido",
                detail: "El enlace de recuperación no es válido, ya se usó o venció. Solicita uno nuevo."),
        };
    }

    /// <summary>
    /// Cookie HttpOnly, SameSite=Strict, ruta /api/auth. Secure siempre fuera de Development (y en Development si la
    /// petición llega por HTTPS). Sin «Recordarme» es una cookie de sesión del navegador (sin Expires).
    /// </summary>
    private void EscribirCookie(SesionEmitida? sesion)
    {
        if (sesion is null) return;
        Response.Cookies.Append(SesionOptions.Cookie, sesion.Token, OpcionesCookie(sesion.Recordar ? sesion.ExpiraEn : null));
    }

    private void BorrarCookie() => Response.Cookies.Delete(SesionOptions.Cookie, OpcionesCookie(null));

    private CookieOptions OpcionesCookie(DateTime? expira) => new()
    {
        HttpOnly = true,
        Secure = Request.IsHttps || !entorno.IsDevelopment(),
        SameSite = SameSiteMode.Strict,
        Path = SesionOptions.RutaCookie,
        IsEssential = true,
        Expires = expira is null ? null : new DateTimeOffset(expira.Value, TimeSpan.Zero),
    };

    private UnauthorizedObjectResult SesionNoValida() => Unauthorized(new ProblemDetails
    {
        Type = "https://httpstatuses.com/401",
        Title = "Sesión no válida",
        Status = StatusCodes.Status401Unauthorized,
        Detail = "Tu sesión terminó. Inicia sesión de nuevo."
    });
}
