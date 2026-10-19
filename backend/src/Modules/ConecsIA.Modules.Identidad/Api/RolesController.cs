using ConecsIA.Modules.Identidad.Application;
using ConecsIA.Modules.Identidad.Domain;
using ConecsIA.SharedKernel.Seguridad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ConecsIA.Modules.Identidad.Api;

/// <summary>HU-017 Roles configurables. Permisos verificados en <see cref="ServicioRoles"/>.</summary>
[ApiController]
[Authorize]
[Route("api/v1/roles")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class RolesController(ServicioRoles servicio) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<RolDto>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<RolDto>> Listar([FromQuery] bool? activos, CancellationToken cancelacion) =>
        servicio.ListarAsync(activos, cancelacion);

    [HttpGet("plantillas")]
    [ProducesResponseType<IReadOnlyList<PlantillaRol>>(StatusCodes.Status200OK)]
    public IReadOnlyList<PlantillaRol> Plantillas() => servicio.Plantillas();

    [HttpGet("{id:guid}")]
    [ProducesResponseType<RolDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<RolDto> Obtener(Guid id, CancellationToken cancelacion) => servicio.ObtenerAsync(id, cancelacion);

    [HttpPost]
    [ProducesResponseType<RolDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Crear(SolicitudCrearRol solicitud, CancellationToken cancelacion)
    {
        var rol = await servicio.CrearAsync(solicitud, cancelacion);
        return CreatedAtAction(nameof(Obtener), new { id = rol.Id }, rol);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<RolDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public Task<RolDto> Modificar(Guid id, SolicitudModificarRol solicitud, CancellationToken cancelacion) =>
        servicio.ModificarAsync(id, solicitud, cancelacion);

    [HttpPost("{id:guid}/inactivar")]
    [ProducesResponseType<RolDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public Task<RolDto> Inactivar(Guid id, CancellationToken cancelacion) => servicio.InactivarAsync(id, cancelacion);
}

/// <summary>HU-017: catálogo cerrado de permisos (recurso y acción) para el editor de roles.</summary>
[ApiController]
[Authorize]
[Route("api/v1/permisos")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class PermisosController(ServicioRoles servicio) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<DefinicionPermiso>>(StatusCodes.Status200OK)]
    public IReadOnlyList<DefinicionPermiso> Listar() => servicio.Catalogo();
}
