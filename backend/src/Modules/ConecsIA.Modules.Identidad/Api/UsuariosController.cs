using ConecsIA.Modules.Identidad.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;

namespace ConecsIA.Modules.Identidad.Api;

/// <summary>HU-016 y HU-018: usuarios de la organización. Permisos y alcance en <see cref="ServicioUsuarios"/>.</summary>
[ApiController]
[Authorize]
[Route("api/v1/usuarios")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class UsuariosController(ServicioUsuarios servicio, IWebHostEnvironment entorno) : ControllerBase
{
    /// <summary>
    /// Respuesta de la invitación. El enlace solo se devuelve en Development/Testing, mientras el envío
    /// por correo (loop_integraciones, D-12) no exista; en otros entornos se entrega por ese canal.
    /// </summary>
    public sealed record InvitacionDto(UsuarioOrganizacionDto Usuario, string? EnlaceAceptacion);

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<UsuarioOrganizacionDto>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<UsuarioOrganizacionDto>> Listar(CancellationToken cancelacion) => servicio.ListarAsync(cancelacion);

    [HttpGet("{id:guid}")]
    [ProducesResponseType<UsuarioOrganizacionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<UsuarioOrganizacionDto> Obtener(Guid id, CancellationToken cancelacion) => servicio.ObtenerAsync(id, cancelacion);

    [HttpPost("invitaciones")]
    [ProducesResponseType<InvitacionDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Invitar(SolicitudInvitacion solicitud, CancellationToken cancelacion)
    {
        var creada = await servicio.InvitarAsync(solicitud, cancelacion);
        var exponer = entorno.IsDevelopment() || entorno.IsEnvironment("Testing");
        var dto = new InvitacionDto(creada.Usuario, exponer ? $"/invitaciones/{creada.Token}" : null);
        return CreatedAtAction(nameof(Obtener), new { id = creada.Usuario.UsuarioId }, dto);
    }

    [HttpPut("{id:guid}/roles")]
    [ProducesResponseType<UsuarioOrganizacionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<UsuarioOrganizacionDto> AsignarRoles(Guid id, SolicitudRoles solicitud, CancellationToken cancelacion) =>
        servicio.AsignarRolesAsync(id, solicitud, cancelacion);

    [HttpPut("{id:guid}/alcance")]
    [ProducesResponseType<UsuarioOrganizacionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<UsuarioOrganizacionDto> AsignarAlcance(Guid id, SolicitudAlcance solicitud, CancellationToken cancelacion) =>
        servicio.AsignarAlcanceAsync(id, solicitud, cancelacion);

    [HttpPost("{id:guid}/inactivar")]
    [ProducesResponseType<UsuarioOrganizacionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<UsuarioOrganizacionDto> Inactivar(Guid id, CancellationToken cancelacion) => servicio.InactivarAsync(id, cancelacion);

    [HttpPost("{id:guid}/reactivar")]
    [ProducesResponseType<UsuarioOrganizacionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<UsuarioOrganizacionDto> Reactivar(Guid id, CancellationToken cancelacion) => servicio.ReactivarAsync(id, cancelacion);
}

/// <summary>
/// HU-016: aceptación anónima de la invitación; el token es la prueba de posesión del correo. Va en
/// el cuerpo, no en la ruta, para que no quede en trazas ni en Problem Details (ADR-018).
/// </summary>
[ApiController]
[Route("api/v1/invitaciones")]
public sealed class InvitacionesController(ServicioAceptacionInvitaciones servicio) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("aceptar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status410Gone)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Aceptar(SolicitudAceptarInvitacion solicitud, CancellationToken cancelacion)
    {
        await servicio.AceptarAsync(solicitud, cancelacion);
        return NoContent();
    }
}
