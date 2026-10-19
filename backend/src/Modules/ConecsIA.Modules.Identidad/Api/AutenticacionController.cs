using System.Security.Claims;
using ConecsIA.Modules.Identidad.Application;
using ConecsIA.SharedKernel.Seguridad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ConecsIA.Modules.Identidad.Api;

/// <summary>HU-006: inicio y cierre de sesión con la cookie de Identity (ADR-006, opción A).</summary>
[ApiController]
[Authorize]
[Route("api/v1/auth")]
public sealed class AutenticacionController(ServicioIngreso servicio, IContextoSolicitud contexto) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status423Locked)]
    public async Task<IActionResult> Ingresar(SolicitudIngreso solicitud, CancellationToken cancelacion)
    {
        await servicio.IngresarAsync(solicitud, cancelacion);
        return NoContent();
    }

    /// <summary>Anónimo a propósito: cerrar sesión no exige membresía activa.</summary>
    [AllowAnonymous]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Cerrar(CancellationToken cancelacion)
    {
        await servicio.CerrarAsync(Claim(ClaimTypes.NameIdentifier), Claim(ISesionUsuario.ClaimSesion), cancelacion);
        return NoContent();
    }

    /// <summary>Sesiones vigentes del usuario autenticado (D-03c: simultáneas y revocables).</summary>
    [HttpGet("sesiones")]
    [ProducesResponseType<IReadOnlyList<SesionDto>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<SesionDto>> Sesiones(CancellationToken cancelacion) =>
        servicio.ListarSesionesAsync(contexto.UsuarioId!.Value, Claim(ISesionUsuario.ClaimSesion), cancelacion);

    [HttpDelete("sesiones/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Revocar(Guid id, CancellationToken cancelacion)
    {
        await servicio.RevocarSesionAsync(contexto.UsuarioId!.Value, id, cancelacion);
        return NoContent();
    }

    private Guid? Claim(string tipo) => Guid.TryParse(User.FindFirstValue(tipo), out var valor) ? valor : null;
}
