using ConecsIA.Modules.Estructura.Application;
using ConecsIA.Modules.Estructura.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ConecsIA.Modules.Estructura.Api;

/// <summary>HU-015 Zonas y puntos de acceso (ADR-019). Permisos y alcance en <see cref="ServicioZonas"/>.</summary>
[ApiController]
[Authorize]
[Route("api/v1/zonas")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class ZonasController(ServicioZonas servicio) : ControllerBase
{
    /// <summary>Con <c>operacion</c>, solo los puntos activos que la admiten y que el usuario puede operar (HU-018).</summary>
    [HttpGet("~/api/v1/sedes/{sedeId:guid}/zonas")]
    [ProducesResponseType<IReadOnlyList<ZonaDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<IReadOnlyList<ZonaDto>> Listar(Guid sedeId, [FromQuery] ModoPuntoAcceso? operacion, CancellationToken cancelacion) =>
        servicio.ListarAsync(sedeId, operacion, cancelacion);

    [HttpPost("~/api/v1/sedes/{sedeId:guid}/zonas")]
    [ProducesResponseType<ZonaDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Crear(Guid sedeId, SolicitudCrearZona solicitud, CancellationToken cancelacion)
    {
        var zona = await servicio.CrearAsync(sedeId, solicitud, cancelacion);
        return CreatedAtAction(nameof(Obtener), new { id = zona.Id }, zona);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<ZonaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<ZonaDto> Obtener(Guid id, CancellationToken cancelacion) => servicio.ObtenerAsync(id, cancelacion);

    [HttpPut("{id:guid}")]
    [ProducesResponseType<ZonaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public Task<ZonaDto> Modificar(Guid id, SolicitudModificarZona solicitud, CancellationToken cancelacion) =>
        servicio.ModificarAsync(id, solicitud, cancelacion);

    [HttpPost("{id:guid}/inactivar")]
    [ProducesResponseType<ZonaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public Task<ZonaDto> Inactivar(Guid id, CancellationToken cancelacion) => servicio.InactivarAsync(id, cancelacion);
}
